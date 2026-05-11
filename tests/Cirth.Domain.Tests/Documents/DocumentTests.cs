using Cirth.Domain.Documents;
using Cirth.Shared;
using FluentAssertions;

namespace Cirth.Domain.Tests.Documents;

public sealed class DocumentTests
{
    private static readonly TenantId TenantId = new(Guid.NewGuid());

    [Fact]
    public void Create_WithValidData_ShouldReturnPendingDocument()
    {
        var doc = Document.Create(TenantId, "My PDF", DocumentSourceType.Pdf, "Author");

        doc.TenantId.Should().Be(TenantId);
        doc.Title.Should().Be("My PDF");
        doc.Status.Should().Be(DocumentStatus.Pending);
        doc.SourceType.Should().Be(DocumentSourceType.Pdf);
        doc.Versions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithEmptyTitle_ShouldThrow(string title)
    {
        var act = () => Document.Create(TenantId, title, DocumentSourceType.Text);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddVersion_FirstVersion_ShouldSetCurrentVersionAndStatus()
    {
        var doc = Document.Create(TenantId, "Doc", DocumentSourceType.Pdf);
        var version = doc.AddVersion("abc123", "bucket/key", 1024, "application/pdf");

        doc.CurrentVersionId.Should().Be(version.Id);
        doc.Versions.Should().HaveCount(1);
        version.IsCurrent.Should().BeTrue();
    }

    [Fact]
    public void AddVersion_SecondVersion_ShouldMarkPreviousAsHistorical()
    {
        var doc = Document.Create(TenantId, "Doc", DocumentSourceType.Pdf);
        var v1 = doc.AddVersion("hash1", "key1", 1024, "application/pdf");
        var v2 = doc.AddVersion("hash2", "key2", 2048, "application/pdf");

        v1.IsCurrent.Should().BeFalse();
        v2.IsCurrent.Should().BeTrue();
        doc.CurrentVersionId.Should().Be(v2.Id);
        doc.Versions.Should().HaveCount(2);
    }

    [Fact]
    public void MarkAsIndexed_ShouldSetStatusIndexed()
    {
        var doc = Document.Create(TenantId, "Doc", DocumentSourceType.Pdf);
        doc.MarkAsIndexed();
        doc.Status.Should().Be(DocumentStatus.Indexed);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetStatusFailed()
    {
        var doc = Document.Create(TenantId, "Doc", DocumentSourceType.Pdf);
        doc.MarkAsFailed("parse error");
        doc.Status.Should().Be(DocumentStatus.Failed);
    }
}
