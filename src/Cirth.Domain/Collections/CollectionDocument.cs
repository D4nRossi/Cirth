using Cirth.Shared;

namespace Cirth.Domain.Collections;

public sealed class CollectionDocument
{
    public CollectionId CollectionId { get; set; }
    public DocumentId DocumentId { get; set; }
}
