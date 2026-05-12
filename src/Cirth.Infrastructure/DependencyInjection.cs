using Azure.AI.OpenAI;
using Cirth.Infrastructure.Health;
using Cirth.Application.Common.Ports;
using Cirth.Infrastructure.Ai.Adapters;
using Cirth.Infrastructure.Auth;
using Cirth.Infrastructure.Parsers;
using Cirth.Infrastructure.Persistence;
using Cirth.Infrastructure.Queue;
using Cirth.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Qdrant.Client;
using StackExchange.Redis;

namespace Cirth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Postgres + EF Core
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Postgres"))
               .UseSnakeCaseNamingConvention());

        services.AddScoped<IDbContextAccessor, AppDbContextAccessor>();
        services.AddScoped<IQueryDbContext, AppQueryDbContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<ISavedAnswerRepository, SavedAnswerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IUserInviteRepository, UserInviteRepository>();
        services.AddScoped<IUserQuotaRepository, UserQuotaRepository>();
        services.AddScoped<IDocumentRelationsRepository, DocumentRelationsRepository>();

        // BM25 (Postgres full-text search)
        services.AddScoped<IBm25SearchService, Bm25SearchService>();

        // Job queue
        services.AddScoped<IJobQueue, PostgresJobQueue>();

        // Qdrant
        services.AddSingleton(_ =>
        {
            var endpoint = configuration["Qdrant:Endpoint"] ?? "http://localhost:6334";
            var uri = new Uri(endpoint);
            var apiKey = configuration["Qdrant:ApiKey"];
            return new QdrantClient(uri.Host, uri.Port, apiKey: apiKey);
        });
        services.AddScoped<IVectorStore, QdrantVectorStore>();

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));

        // MinIO
        services.AddSingleton<IMinioClient>(_ =>
        {
            var endpoint = configuration["Minio:Endpoint"] ?? "localhost:9000";
            var accessKey = configuration["Minio:AccessKey"]!;
            var secretKey = configuration["Minio:SecretKey"]!;
            var useSsl = bool.TryParse(configuration["Minio:UseSsl"], out var ssl) && ssl;
            var client = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey);
            if (useSsl) client = client.WithSSL();
            return client.Build();
        });
        services.AddScoped<IObjectStorage, MinioObjectStorage>();

        // Azure AI Foundry — embeddings (legacy endpoint pattern, AzureOpenAIClient handles it)
        // URL: https://X.cognitiveservices.azure.com/openai/deployments/{model}/embeddings?api-version=...
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ =>
        {
            var endpoint = configuration["AzureAi:Embedding:Endpoint"]
                ?? throw new InvalidOperationException("AzureAi:Embedding:Endpoint is required.");
            var apiKey = configuration["AzureAi:Embedding:ApiKey"]
                ?? configuration["AzureAi:ApiKey"]
                ?? throw new InvalidOperationException("AzureAi:Embedding:ApiKey (or fallback AzureAi:ApiKey) is required.");
            var deployment = configuration["AzureAi:Embedding:Deployment"] ?? "text-embedding-ada-002";
            return new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey))
                .GetEmbeddingClient(deployment)
                .AsIEmbeddingGenerator();
        });
        services.AddScoped<IEmbeddingService, AzureAiEmbeddingService>();

        // Azure AI Foundry — chat (NEW /openai/v1 endpoint, requires raw OpenAI SDK with endpoint override)
        // URL: https://X.openai.azure.com/openai/v1/chat/completions  — body carries model id.
        // AzureOpenAIClient still uses the legacy /openai/deployments/{name}/... path which 404s here.
        services.AddSingleton<IChatClient>(_ =>
        {
            var endpoint = configuration["AzureAi:Chat:Endpoint"]
                ?? throw new InvalidOperationException("AzureAi:Chat:Endpoint is required.");
            var apiKey = configuration["AzureAi:Chat:ApiKey"]
                ?? configuration["AzureAi:ApiKey"]
                ?? throw new InvalidOperationException("AzureAi:Chat:ApiKey (or fallback AzureAi:ApiKey) is required.");
            var deployment = configuration["AzureAi:Chat:Deployment"] ?? "gpt-4.1";
            var chatClient = new OpenAI.Chat.ChatClient(
                model: deployment,
                credential: new System.ClientModel.ApiKeyCredential(apiKey),
                options: new OpenAI.OpenAIClientOptions { Endpoint = new Uri(endpoint) });
            return chatClient.AsIChatClient();
        });
        services.AddScoped<ILlmChatService, AzureAiLlmService>();

        // Document parsers
        services.AddScoped<IDocumentParser, PdfDocumentParser>();
        services.AddScoped<IDocumentParser, DocxDocumentParser>();
        services.AddScoped<IDocumentParser, MarkdownDocumentParser>();
        services.AddScoped<IDocumentParser, HtmlDocumentParser>();
        services.AddScoped<IDocumentParser, PlainTextDocumentParser>();
        services.AddScoped<IDocumentParser, WebLinkParser>();
        services.AddScoped<CompositeDocumentParser>();

        // Chunker
        services.AddScoped<IChunker, SemanticKernelChunker>();

        // Auth
        services.AddScoped<IApiKeyHasher, ApiKeyHasher>();

        // Notification hub — null by default; Web overrides with SignalRNotificationHub via AddSignalRNotifications().
        services.AddScoped<INotificationHub, NullNotificationHub>();

        // System health checks (admin dashboard).
        services.AddScoped<ISystemHealthService, SystemHealthService>();

        // HTTP client for web link parsing
        services.AddHttpClient("web-fetch", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Cirth/1.0 (+https://cirth.local)");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    /// <summary>
    /// Registers SignalR-backed INotificationHub. Call this only from the Web host, after AddSignalR().
    /// </summary>
    public static IServiceCollection AddSignalRNotifications(this IServiceCollection services)
    {
        services.AddScoped<INotificationHub, SignalRNotificationHub>();
        return services;
    }
}
