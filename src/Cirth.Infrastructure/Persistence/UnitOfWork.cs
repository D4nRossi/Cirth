using Cirth.Application.Common.Ports;

namespace Cirth.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task CommitAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
