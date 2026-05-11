using Cirth.Application.Common.Ports;
using Cirth.Application.Features.Documents.UploadDocument;
using Cirth.Infrastructure.Parsers;
using Cirth.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Cirth.Worker;

public sealed class JobPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<JobPollingService> logger,
    IConfiguration configuration) : BackgroundService
{
    private readonly int _intervalSeconds = configuration.GetValue("Worker:JobPollingIntervalSeconds", 5);
    private readonly int _maxConcurrent = configuration.GetValue("Worker:MaxConcurrentJobs", 3);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("JobPollingService started. Polling every {Interval}s", _intervalSeconds);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in job polling loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), ct);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var jobQueue = scope.ServiceProvider.GetRequiredService<IJobQueue>();

        var jobs = await jobQueue.DequeueAsync(_maxConcurrent, ct);
        if (jobs.Count == 0) return;

        logger.LogInformation("Dequeued {Count} job(s)", jobs.Count);

        var tasks = jobs.Select(job => ProcessJobAsync(job, ct));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessJobAsync(JobRecord job, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var jobQueue = scope.ServiceProvider.GetRequiredService<IJobQueue>();
        var logger2 = scope.ServiceProvider.GetRequiredService<ILogger<JobPollingService>>();

        try
        {
            logger2.LogInformation("Processing job {JobId} type={Type}", job.Id.Value, job.Type);

            switch (job.Type)
            {
                case "ProcessDocument":
                    await ProcessDocumentJobAsync(scope.ServiceProvider, job, ct);
                    break;
                case "SuggestTags":
                    await SuggestTagsJobAsync(scope.ServiceProvider, job, ct);
                    break;
                default:
                    logger2.LogWarning("Unknown job type: {Type}", job.Type);
                    break;
            }

            await jobQueue.CompleteAsync(job.Id, ct);
            logger2.LogInformation("Job {JobId} completed", job.Id.Value);
        }
        catch (Exception ex)
        {
            logger2.LogError(ex, "Job {JobId} failed: {Error}", job.Id.Value, ex.Message);
            await jobQueue.FailAsync(job.Id, ex.Message, ct);
        }
    }

    private static async Task ProcessDocumentJobAsync(
        IServiceProvider sp, JobRecord job, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<ProcessDocumentPayload>(job.PayloadJson)!;

        var objectStorage = sp.GetRequiredService<IObjectStorage>();
        var compositeParser = sp.GetRequiredService<CompositeDocumentParser>();
        var chunker = sp.GetRequiredService<IChunker>();
        var embeddingService = sp.GetRequiredService<IEmbeddingService>();
        var vectorStore = sp.GetRequiredService<IVectorStore>();
        var chunkRepo = sp.GetRequiredService<IChunkRepository>();
        var documentRepo = sp.GetRequiredService<IDocumentRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var notificationHub = sp.GetRequiredService<INotificationHub>();

        var tenantId = new TenantId(payload.TenantId);
        var docId = new DocumentId(payload.DocumentId);
        var versionId = new DocumentVersionId(payload.VersionId);

        var document = await documentRepo.GetByIdAsync(docId, ct)
            ?? throw new InvalidOperationException($"Document {payload.DocumentId} not found.");

        // Mark old chunks as historical if re-upload
        await chunkRepo.MarkVersionHistoricalAsync(versionId, ct);

        // Download from MinIO
        using var contentStream = await objectStorage.GetAsync("cirth-uploads", payload.StorageKey, ct);

        // Parse
        var parser = compositeParser.GetParser(payload.MimeType)
            ?? throw new NotSupportedException($"No parser for mime type: {payload.MimeType}");
        var text = await parser.ExtractTextAsync(contentStream, ct);

        // Chunk
        var chunks = chunker.Chunk(text, maxTokens: 800, overlapTokens: 100);

        // Embed and index
        var contents = chunks.Select(c => c.Content).ToList();
        var embeddings = await embeddingService.EmbedBatchAsync(contents, ct);

        var domainChunks = new List<Domain.Documents.Chunk>();
        for (int i = 0; i < chunks.Count; i++)
        {
            var qdrantId = Guid.NewGuid();
            var chunk = Domain.Documents.Chunk.Create(tenantId, versionId, chunks[i].Ordinal, chunks[i].Content, chunks[i].TokenCount, qdrantId);
            domainChunks.Add(chunk);

            await vectorStore.UpsertAsync(tenantId, chunk.Id, embeddings[i],
                new Dictionary<string, object>
                {
                    ["document_id"] = payload.DocumentId.ToString(),
                    ["version_id"] = payload.VersionId.ToString(),
                    ["chunk_id"] = chunk.Id.Value.ToString(),
                    ["ordinal"] = i.ToString()
                }, ct);
        }

        await chunkRepo.AddRangeAsync(domainChunks, ct);
        document.MarkAsIndexed();
        await uow.CommitAsync(ct);

        await notificationHub.NotifyDocumentIndexedAsync(
            tenantId, new UserId(payload.UserId), payload.DocumentId, document.Title, ct);

        // Enqueue tag suggestion job
        var jobQueue = sp.GetRequiredService<IJobQueue>();
        await jobQueue.EnqueueAsync("SuggestTags",
            JsonSerializer.Serialize(new { payload.DocumentId, payload.TenantId, payload.UserId }), ct);
        await uow.CommitAsync(ct);
    }

    private static async Task SuggestTagsJobAsync(
        IServiceProvider sp, JobRecord job, CancellationToken ct)
    {
        // The actual suggestion is handled by the SuggestTagsCommand handler.
        // Here we just dispatch it via MediatR.
        var mediator = sp.GetRequiredService<MediatR.IMediator>();
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(job.PayloadJson)!;
        if (!Guid.TryParse(payload.GetValueOrDefault("DocumentId"), out var docId)) return;

        // Set tenant context
        var tenantProvider = sp.GetRequiredService<ITenantProvider>();
        // Note: in Worker context, TenantProvider reads from job payload via IWorkerTenantContext
        await mediator.Send(new Application.Features.Tags.SuggestTags.SuggestTagsCommand(docId), ct);
    }
}
