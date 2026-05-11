using Cirth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Cirth.Integration.Tests.Infrastructure;

public sealed class CirthTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("cirth_test")
        .WithUsername("cirth")
        .WithPassword("test_pass")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;
    public AppDbContext DbContext => Services.GetRequiredService<AppDbContext>();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                ["Qdrant:Endpoint"] = "http://localhost:6334",
                ["Minio:Endpoint"] = "localhost:9000",
                ["Minio:AccessKey"] = "minioadmin",
                ["Minio:SecretKey"] = "minioadmin123",
                ["AzureAi:Endpoint"] = "https://placeholder.openai.azure.com",
                ["AzureAi:ApiKey"] = "placeholder"
            })
            .Build();

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(_postgres.GetConnectionString())
               .UseSnakeCaseNamingConvention());

        Services = services.BuildServiceProvider();

        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
