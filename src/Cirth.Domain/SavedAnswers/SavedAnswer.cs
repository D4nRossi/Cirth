using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.SavedAnswers;

public sealed class SavedAnswer : Entity<SavedAnswerId>, IAggregateRoot
{
    public TenantId TenantId { get; private set; }
    public string Question { get; private set; } = string.Empty;
    public string Answer { get; private set; } = string.Empty;
    public Guid[] CitedChunkIds { get; private set; } = [];
    public Guid[] TagIds { get; private set; } = [];
    public int UsageCount { get; private set; }
    public int UtilityScore { get; private set; }
    public Guid QdrantPointId { get; private set; }

    private SavedAnswer() { }

    public static SavedAnswer Create(
        TenantId tenantId,
        string question,
        string answer,
        IEnumerable<Guid> citedChunkIds,
        Guid qdrantPointId)
    {
        return new SavedAnswer
        {
            Id = SavedAnswerId.New(),
            TenantId = tenantId,
            Question = question.Trim(),
            Answer = answer,
            CitedChunkIds = citedChunkIds.ToArray(),
            TagIds = [],
            UsageCount = 0,
            UtilityScore = 0,
            QdrantPointId = qdrantPointId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void RecordUsage()
    {
        UsageCount++;
        Touch();
    }

    public void VoteUp()
    {
        UtilityScore++;
        Touch();
    }

    public void VoteDown()
    {
        UtilityScore--;
        Touch();
    }

    public void AddTag(Guid tagId)
    {
        if (!TagIds.Contains(tagId))
        {
            TagIds = [.. TagIds, tagId];
            Touch();
        }
    }
}
