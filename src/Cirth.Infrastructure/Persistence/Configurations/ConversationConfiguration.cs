using Cirth.Domain.Conversations;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("conversations");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasConversion(id => id.Value, v => new ConversationId(v));
        b.Property(c => c.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).IsRequired();
        b.Property(c => c.UserId).HasConversion(id => id.Value, v => new UserId(v)).IsRequired();
        b.Property(c => c.Title).HasMaxLength(300);
        b.Property(c => c.CreatedAt).IsRequired();
        b.Property(c => c.UpdatedAt).IsRequired();

        b.HasMany(c => c.Messages)
         .WithOne()
         .HasForeignKey(m => m.ConversationId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(c => new { c.TenantId, c.UserId });
    }
}

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("messages");
        b.HasKey(m => m.Id);
        b.Property(m => m.Id).HasConversion(id => id.Value, v => new MessageId(v));
        b.Property(m => m.ConversationId).HasConversion(id => id.Value, v => new ConversationId(v)).IsRequired();
        b.Property(m => m.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(m => m.Content).IsRequired();
        b.Property(m => m.CitedChunkIds).HasColumnType("uuid[]");
        b.Property(m => m.TokensUsed);
        b.Property(m => m.Model).HasMaxLength(50);
        b.Property(m => m.CreatedAt).IsRequired();
        b.Property(m => m.UpdatedAt).IsRequired();
        b.HasIndex(m => m.ConversationId);
    }
}
