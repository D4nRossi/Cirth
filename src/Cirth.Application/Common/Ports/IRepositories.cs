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

namespace Cirth.Application.Common.Ports;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct = default);
}

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(DocumentId id, CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
    void Remove(Document document);
}

public interface IChunkRepository
{
    Task AddRangeAsync(IEnumerable<Domain.Documents.Chunk> chunks, CancellationToken ct = default);
    Task MarkVersionHistoricalAsync(DocumentVersionId versionId, CancellationToken ct = default);
    Task<IReadOnlyList<Domain.Documents.Chunk>> GetByVersionAsync(DocumentVersionId versionId, CancellationToken ct = default);
}

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(TagId id, CancellationToken ct = default);
    Task<Tag?> GetByNameAsync(TenantId tenantId, string name, CancellationToken ct = default);
    Task<IReadOnlyList<Tag>> ListAsync(TenantId tenantId, CancellationToken ct = default);
    Task AddAsync(Tag tag, CancellationToken ct = default);
    void Remove(Tag tag);
}

public interface ICollectionRepository
{
    Task<Collection?> GetByIdAsync(CollectionId id, CancellationToken ct = default);
    Task<IReadOnlyList<Collection>> ListAsync(TenantId tenantId, CancellationToken ct = default);
    Task AddAsync(Collection collection, CancellationToken ct = default);
    void Remove(Collection collection);
}

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken ct = default);
    Task<IReadOnlyList<Conversation>> ListByUserAsync(TenantId tenantId, UserId userId, CancellationToken ct = default);
    Task AddAsync(Conversation conversation, CancellationToken ct = default);
}

public interface ISavedAnswerRepository
{
    Task<SavedAnswer?> GetByIdAsync(SavedAnswerId id, CancellationToken ct = default);
    Task<IReadOnlyList<SavedAnswer>> ListAsync(TenantId tenantId, CancellationToken ct = default);
    Task AddAsync(SavedAnswer savedAnswer, CancellationToken ct = default);
    void Remove(SavedAnswer savedAnswer);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task<User?> GetByEntraObjectIdAsync(Guid entraObjectId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> ListByTenantAsync(TenantId tenantId, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct = default);
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
}

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKey>> ListByUserAsync(UserId userId, CancellationToken ct = default);
    Task AddAsync(ApiKey apiKey, CancellationToken ct = default);
}

public interface IUserInviteRepository
{
    Task<UserInvite?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(UserInvite invite, CancellationToken ct = default);
}

public interface IUserQuotaRepository
{
    Task<UserQuota?> GetByUserIdAsync(UserId userId, CancellationToken ct = default);
    Task AddAsync(UserQuota quota, CancellationToken ct = default);
}

public interface IDocumentRelationsRepository
{
    Task AddTagAsync(Guid documentId, Guid tagId, CancellationToken ct = default);
    Task RemoveTagAsync(Guid documentId, Guid tagId, CancellationToken ct = default);
    Task AddToCollectionAsync(Guid collectionId, Guid documentId, CancellationToken ct = default);
    Task RemoveFromCollectionAsync(Guid collectionId, Guid documentId, CancellationToken ct = default);
}
