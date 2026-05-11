using Cirth.Application.Common.Ports;
using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Logging;

namespace Cirth.Infrastructure.Storage;

internal sealed class MinioObjectStorage(IMinioClient minio, ILogger<MinioObjectStorage> logger) : IObjectStorage
{
    public async Task<string> PutAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct)
    {
        await EnsureBucketAsync(bucket, ct);

        var args = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(key)
            .WithStreamData(content)
            .WithObjectSize(content.Length == 0 ? -1 : content.Length)
            .WithContentType(contentType);

        await minio.PutObjectAsync(args, ct);
        logger.LogDebug("Stored object {Key} in bucket {Bucket}", key, bucket);
        return key;
    }

    public async Task<Stream> GetAsync(string bucket, string key, CancellationToken ct)
    {
        var ms = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(key)
            .WithCallbackStream(stream => stream.CopyTo(ms));

        await minio.GetObjectAsync(args, ct);
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string bucket, string key, CancellationToken ct)
    {
        var args = new RemoveObjectArgs().WithBucket(bucket).WithObject(key);
        await minio.RemoveObjectAsync(args, ct);
    }

    private async Task EnsureBucketAsync(string bucket, CancellationToken ct)
    {
        var exists = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct);
        if (!exists)
            await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct);
    }
}
