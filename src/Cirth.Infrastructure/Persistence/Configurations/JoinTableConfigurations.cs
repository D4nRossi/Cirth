using Cirth.Domain.Collections;
using Cirth.Domain.Documents;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class DocumentTagConfiguration : IEntityTypeConfiguration<DocumentTag>
{
    public void Configure(EntityTypeBuilder<DocumentTag> b)
    {
        b.ToTable("document_tags");
        b.HasKey(dt => new { dt.DocumentId, dt.TagId });
        b.Property(dt => dt.DocumentId).HasConversion(id => id.Value, v => new DocumentId(v)).IsRequired();
        b.Property(dt => dt.TagId).HasConversion(id => id.Value, v => new TagId(v)).IsRequired();
    }
}

internal sealed class CollectionDocumentConfiguration : IEntityTypeConfiguration<CollectionDocument>
{
    public void Configure(EntityTypeBuilder<CollectionDocument> b)
    {
        b.ToTable("collection_documents");
        b.HasKey(cd => new { cd.CollectionId, cd.DocumentId });
        b.Property(cd => cd.CollectionId).HasConversion(id => id.Value, v => new CollectionId(v)).IsRequired();
        b.Property(cd => cd.DocumentId).HasConversion(id => id.Value, v => new DocumentId(v)).IsRequired();
    }
}
