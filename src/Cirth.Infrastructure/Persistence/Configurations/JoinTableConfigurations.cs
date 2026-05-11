using Cirth.Domain.Collections;
using Cirth.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class DocumentTagConfiguration : IEntityTypeConfiguration<DocumentTag>
{
    public void Configure(EntityTypeBuilder<DocumentTag> b)
    {
        b.ToTable("document_tags");
        b.HasKey(dt => new { dt.DocumentId, dt.TagId });
        b.Property(dt => dt.DocumentId).IsRequired();
        b.Property(dt => dt.TagId).IsRequired();
    }
}

internal sealed class CollectionDocumentConfiguration : IEntityTypeConfiguration<CollectionDocument>
{
    public void Configure(EntityTypeBuilder<CollectionDocument> b)
    {
        b.ToTable("collection_documents");
        b.HasKey(cd => new { cd.CollectionId, cd.DocumentId });
        b.Property(cd => cd.CollectionId).IsRequired();
        b.Property(cd => cd.DocumentId).IsRequired();
    }
}
