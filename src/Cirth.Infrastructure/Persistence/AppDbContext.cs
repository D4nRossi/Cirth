using Cirth.Domain.Auth;
using Cirth.Domain.Collections;
using Cirth.Domain.Common;
using Cirth.Domain.Conversations;
using Cirth.Domain.Documents;
using Cirth.Domain.Jobs;
using Cirth.Domain.Quotas;
using Cirth.Domain.SavedAnswers;
using Cirth.Domain.Tags;
using Cirth.Domain.Tenants;
using Cirth.Domain.Users;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<Chunk> Chunks => Set<Chunk>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<SavedAnswer> SavedAnswers => Set<SavedAnswer>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<UserInvite> UserInvites => Set<UserInvite>();
    public DbSet<UserQuota> UserQuotas => Set<UserQuota>();
    public DbSet<IngestionJob> Jobs => Set<IngestionJob>();
    public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();
    public DbSet<CollectionDocument> CollectionDocuments => Set<CollectionDocument>();

    private TenantId? _currentTenantId;
    public void SetTenant(TenantId tenantId) => _currentTenantId = tenantId;
    public TenantId? CurrentTenantId => _currentTenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global tenant query filters — enforced at DB context level.
        // When _currentTenantId is null (migrations, seed, IBypassTenantScope) all records are visible.
        modelBuilder.Entity<Document>().HasQueryFilter(e => _currentTenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<Chunk>().HasQueryFilter(e => _currentTenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<Tag>().HasQueryFilter(e => _currentTenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<Collection>().HasQueryFilter(e => _currentTenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<Conversation>().HasQueryFilter(e => _currentTenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<SavedAnswer>().HasQueryFilter(e => _currentTenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<ApiKey>().HasQueryFilter(e => _currentTenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<UserInvite>().HasQueryFilter(e => _currentTenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<UserQuota>().HasQueryFilter(e => _currentTenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<IngestionJob>().HasQueryFilter(e => _currentTenantId == null || e.TenantId == _currentTenantId);
        modelBuilder.Entity<User>().HasQueryFilter(e => _currentTenantId == null || e.TenantId == _currentTenantId);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await DispatchDomainEventsAsync(cancellationToken);
        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var entities = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var events = entities.SelectMany(e => e.DomainEvents).ToList();
        foreach (var entity in entities) entity.ClearDomainEvents();

        foreach (var @event in events)
        {
            // IDomainEvent adapts to MediatR.INotification at the infrastructure boundary
            if (@event is INotification notification)
                await mediator.Publish(notification, ct);
        }
    }
}
