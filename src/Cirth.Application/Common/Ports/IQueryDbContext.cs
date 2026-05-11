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
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Common.Ports;

/// <summary>
/// Read-only view of the DbContext exposed to query handlers (not to domain/command handlers).
/// Keeps query handlers decoupled from the concrete AppDbContext type.
/// </summary>
public interface IQueryDbContext
{
    IQueryable<Tenant> Tenants { get; }
    IQueryable<User> Users { get; }
    IQueryable<Document> Documents { get; }
    IQueryable<DocumentVersion> DocumentVersions { get; }
    IQueryable<Chunk> Chunks { get; }
    IQueryable<Tag> Tags { get; }
    IQueryable<Collection> Collections { get; }
    IQueryable<Conversation> Conversations { get; }
    IQueryable<Message> Messages { get; }
    IQueryable<SavedAnswer> SavedAnswers { get; }
    IQueryable<ApiKey> ApiKeys { get; }
    IQueryable<UserInvite> UserInvites { get; }
    IQueryable<UserQuota> UserQuotas { get; }
    IQueryable<IngestionJob> Jobs { get; }
    IQueryable<DocumentTag> DocumentTags { get; }
    IQueryable<CollectionDocument> CollectionDocuments { get; }
}
