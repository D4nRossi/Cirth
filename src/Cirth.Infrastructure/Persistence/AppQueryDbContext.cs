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
using Microsoft.EntityFrameworkCore;

namespace Cirth.Infrastructure.Persistence;

internal sealed class AppQueryDbContext(AppDbContext inner) : IQueryDbContext
{
    public IQueryable<Tenant> Tenants => inner.Tenants.AsNoTracking();
    public IQueryable<User> Users => inner.Users.AsNoTracking();
    public IQueryable<Document> Documents => inner.Documents.AsNoTracking();
    public IQueryable<DocumentVersion> DocumentVersions => inner.DocumentVersions.AsNoTracking();
    public IQueryable<Chunk> Chunks => inner.Chunks.AsNoTracking();
    public IQueryable<Tag> Tags => inner.Tags.AsNoTracking();
    public IQueryable<Collection> Collections => inner.Collections.AsNoTracking();
    public IQueryable<Conversation> Conversations => inner.Conversations.AsNoTracking();
    public IQueryable<Message> Messages => inner.Messages.AsNoTracking();
    public IQueryable<SavedAnswer> SavedAnswers => inner.SavedAnswers.AsNoTracking();
    public IQueryable<ApiKey> ApiKeys => inner.ApiKeys.AsNoTracking();
    public IQueryable<UserInvite> UserInvites => inner.UserInvites.AsNoTracking();
    public IQueryable<UserQuota> UserQuotas => inner.UserQuotas.AsNoTracking();
    public IQueryable<IngestionJob> Jobs => inner.Jobs.AsNoTracking();
    public IQueryable<DocumentTag> DocumentTags => inner.DocumentTags.AsNoTracking();
    public IQueryable<CollectionDocument> CollectionDocuments => inner.CollectionDocuments.AsNoTracking();
}
