using Cirth.Application.Common.Ports;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Infrastructure.Persistence;

internal sealed class Bm25SearchService(AppDbContext db) : IBm25SearchService
{
    public async Task<IReadOnlyList<Guid>> SearchChunksAsync(Guid tenantId, string query, int topK, CancellationToken ct)
    {
        // Raw SQL required: EF Core cannot express ts_rank_cd ordering via LINQ
        return await db.Chunks
            .FromSqlInterpolated($"""
                SELECT * FROM chunks
                WHERE tenant_id = {tenantId}
                  AND is_current = true
                  AND content_tsv @@ plainto_tsquery('portuguese', {query})
                ORDER BY ts_rank_cd(content_tsv, plainto_tsquery('portuguese', {query})) DESC
                LIMIT {topK}
                """)
            .Select(c => c.Id.Value)
            .ToListAsync(ct);
    }
}
