using Cirth.Application.Common.Ports;
using Cirth.Domain.Auth;
using Cirth.Domain.Collections;
using Cirth.Domain.Conversations;
using Cirth.Domain.Documents;
using Cirth.Domain.Jobs;
using Cirth.Domain.Quotas;
using Cirth.Domain.SavedAnswers;
using Cirth.Domain.Tags;
using Cirth.Domain.Tenants;
using Cirth.Domain.Users;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Infrastructure.Persistence;

internal sealed class DocumentRepository(AppDbContext db) : IDocumentRepository
{
    public Task<Document?> GetByIdAsync(DocumentId id, CancellationToken ct)
        => db.Documents.Include(d => d.Versions).FirstOrDefaultAsync(d => d.Id == id, ct);
    public Task AddAsync(Document document, CancellationToken ct) => db.Documents.AddAsync(document, ct).AsTask();
    public void Remove(Document document) => db.Documents.Remove(document);
}

internal sealed class ChunkRepository(AppDbContext db) : IChunkRepository
{
    public Task AddRangeAsync(IEnumerable<Chunk> chunks, CancellationToken ct) =>
        db.Chunks.AddRangeAsync(chunks, ct);

    public async Task MarkVersionHistoricalAsync(DocumentVersionId versionId, CancellationToken ct)
    {
        var chunks = await db.Chunks
            .Where(c => c.DocumentVersionId == versionId && c.IsCurrent)
            .ToListAsync(ct);
        foreach (var c in chunks) c.MarkAsHistorical();
    }

    public async Task<IReadOnlyList<Chunk>> GetByVersionAsync(DocumentVersionId versionId, CancellationToken ct)
        => await db.Chunks.Where(c => c.DocumentVersionId == versionId).ToListAsync(ct);
}

internal sealed class TagRepository(AppDbContext db) : ITagRepository
{
    public Task<Tag?> GetByIdAsync(TagId id, CancellationToken ct)
        => db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
    public Task<Tag?> GetByNameAsync(TenantId tenantId, string name, CancellationToken ct)
        => db.Tags.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Name == name.ToLowerInvariant(), ct);
    public async Task<IReadOnlyList<Tag>> ListAsync(TenantId tenantId, CancellationToken ct)
        => await db.Tags.Where(t => t.TenantId == tenantId).OrderBy(t => t.Name).ToListAsync(ct);
    public Task AddAsync(Tag tag, CancellationToken ct) => db.Tags.AddAsync(tag, ct).AsTask();
    public void Remove(Tag tag) => db.Tags.Remove(tag);
}

internal sealed class CollectionRepository(AppDbContext db) : ICollectionRepository
{
    public Task<Collection?> GetByIdAsync(CollectionId id, CancellationToken ct)
        => db.Collections.FirstOrDefaultAsync(c => c.Id == id, ct);
    public async Task<IReadOnlyList<Collection>> ListAsync(TenantId tenantId, CancellationToken ct)
        => await db.Collections.Where(c => c.TenantId == tenantId).ToListAsync(ct);
    public Task AddAsync(Collection collection, CancellationToken ct) => db.Collections.AddAsync(collection, ct).AsTask();
    public void Remove(Collection collection) => db.Collections.Remove(collection);
}

internal sealed class ConversationRepository(AppDbContext db) : IConversationRepository
{
    public Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken ct)
        => db.Conversations.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == id, ct);
    public async Task<IReadOnlyList<Conversation>> ListByUserAsync(TenantId tenantId, UserId userId, CancellationToken ct)
        => await db.Conversations
            .Where(c => c.TenantId == tenantId && c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);
    public Task AddAsync(Conversation conversation, CancellationToken ct) => db.Conversations.AddAsync(conversation, ct).AsTask();
}

internal sealed class SavedAnswerRepository(AppDbContext db) : ISavedAnswerRepository
{
    public Task<SavedAnswer?> GetByIdAsync(SavedAnswerId id, CancellationToken ct)
        => db.SavedAnswers.FirstOrDefaultAsync(s => s.Id == id, ct);
    public async Task<IReadOnlyList<SavedAnswer>> ListAsync(TenantId tenantId, CancellationToken ct)
        => await db.SavedAnswers.Where(s => s.TenantId == tenantId).ToListAsync(ct);
    public Task AddAsync(SavedAnswer savedAnswer, CancellationToken ct) => db.SavedAnswers.AddAsync(savedAnswer, ct).AsTask();
    public void Remove(SavedAnswer savedAnswer) => db.SavedAnswers.Remove(savedAnswer);
}

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct)
        => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    public Task<User?> GetByEntraObjectIdAsync(Guid entraObjectId, CancellationToken ct)
        => db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, ct);
    public async Task<IReadOnlyList<User>> ListByTenantAsync(TenantId tenantId, CancellationToken ct)
        => await db.Users.Where(u => u.TenantId == tenantId).ToListAsync(ct);
    public Task AddAsync(User user, CancellationToken ct) => db.Users.AddAsync(user, ct).AsTask();
}

internal sealed class TenantRepository(AppDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct)
        => db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
    public Task AddAsync(Tenant tenant, CancellationToken ct) => db.Tenants.AddAsync(tenant, ct).AsTask();
}

internal sealed class ApiKeyRepository(AppDbContext db) : IApiKeyRepository
{
    public Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct)
        => db.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == keyHash, ct);
    public async Task<IReadOnlyList<ApiKey>> ListByUserAsync(UserId userId, CancellationToken ct)
        => await db.ApiKeys.Where(k => k.UserId == userId && !k.Revoked).ToListAsync(ct);
    public Task AddAsync(ApiKey apiKey, CancellationToken ct) => db.ApiKeys.AddAsync(apiKey, ct).AsTask();
}

internal sealed class UserInviteRepository(AppDbContext db) : IUserInviteRepository
{
    public Task<UserInvite?> GetByTokenAsync(string token, CancellationToken ct)
        => db.UserInvites.FirstOrDefaultAsync(i => i.Token == token, ct);
    public Task AddAsync(UserInvite invite, CancellationToken ct) => db.UserInvites.AddAsync(invite, ct).AsTask();
}

internal sealed class UserQuotaRepository(AppDbContext db) : IUserQuotaRepository
{
    public Task<UserQuota?> GetByUserIdAsync(UserId userId, CancellationToken ct)
        => db.UserQuotas.FirstOrDefaultAsync(q => q.Id == userId, ct);
    public Task AddAsync(UserQuota quota, CancellationToken ct) => db.UserQuotas.AddAsync(quota, ct).AsTask();
}

internal sealed class DocumentRelationsRepository(AppDbContext db) : IDocumentRelationsRepository
{
    public Task AddTagAsync(Guid documentId, Guid tagId, CancellationToken ct)
        => db.DocumentTags.AddAsync(new() { DocumentId = new(documentId), TagId = new(tagId) }, ct).AsTask();

    public async Task RemoveTagAsync(Guid documentId, Guid tagId, CancellationToken ct)
    {
        var docId = new DocumentId(documentId);
        var tId = new TagId(tagId);
        var link = await db.DocumentTags
            .FirstOrDefaultAsync(dt => dt.DocumentId == docId && dt.TagId == tId, ct);
        if (link is not null)
            db.DocumentTags.Remove(link);
    }

    public Task AddToCollectionAsync(Guid collectionId, Guid documentId, CancellationToken ct)
        => db.CollectionDocuments.AddAsync(new() { CollectionId = new(collectionId), DocumentId = new(documentId) }, ct).AsTask();

    public async Task RemoveFromCollectionAsync(Guid collectionId, Guid documentId, CancellationToken ct)
    {
        var colId = new CollectionId(collectionId);
        var docId = new DocumentId(documentId);
        var link = await db.CollectionDocuments
            .FirstOrDefaultAsync(cd => cd.CollectionId == colId && cd.DocumentId == docId, ct);
        if (link is not null)
            db.CollectionDocuments.Remove(link);
    }
}
