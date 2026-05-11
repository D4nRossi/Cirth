using Cirth.Domain.Documents;
using Cirth.Infrastructure.Persistence;
using Cirth.Integration.Tests.Infrastructure;
using Cirth.Shared;
using FluentAssertions;

namespace Cirth.Integration.Tests.Persistence;

[Collection("Integration")]
public sealed class DocumentRepositoryTests(CirthTestFixture fixture) : IClassFixture<CirthTestFixture>
{
    [Fact]
    public async Task AddAndRetrieve_Document_ShouldPersistCorrectly()
    {
        var db = fixture.DbContext;
        var tenantId = new TenantId(Guid.NewGuid());
        var repo = new DocumentRepository(db);

        var doc = Document.Create(tenantId, "Integration Test Doc", DocumentSourceType.Text, "Tester");
        doc.AddVersion("abc123", "test/key", 512, "text/plain");

        await repo.AddAsync(doc, CancellationToken.None);
        await db.SaveChangesAsync();

        var retrieved = await repo.GetByIdAsync(doc.Id, CancellationToken.None);

        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Integration Test Doc");
        retrieved.Versions.Should().HaveCount(1);
        retrieved.Status.Should().Be(DocumentStatus.Pending);
    }
}
