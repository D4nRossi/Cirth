using Cirth.Shared;

namespace Cirth.Domain.Documents;

public sealed class DocumentTag
{
    public DocumentId DocumentId { get; set; }
    public TagId TagId { get; set; }
}
