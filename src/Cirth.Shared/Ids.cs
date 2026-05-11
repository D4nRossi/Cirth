namespace Cirth.Shared;

// Strongly-typed IDs evitam misturar identifiers de diferentes entidades.
// Compile-time safety vs primitive obsession.

public readonly record struct TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct DocumentId(Guid Value)
{
    public static DocumentId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public readonly record struct DocumentVersionId(Guid Value)
{
    public static DocumentVersionId New() => new(Guid.NewGuid());
}

public readonly record struct ChunkId(Guid Value)
{
    public static ChunkId New() => new(Guid.NewGuid());
}

public readonly record struct ConversationId(Guid Value)
{
    public static ConversationId New() => new(Guid.NewGuid());
}

public readonly record struct MessageId(Guid Value)
{
    public static MessageId New() => new(Guid.NewGuid());
}

public readonly record struct TagId(Guid Value)
{
    public static TagId New() => new(Guid.NewGuid());
}

public readonly record struct CollectionId(Guid Value)
{
    public static CollectionId New() => new(Guid.NewGuid());
}

public readonly record struct SavedAnswerId(Guid Value)
{
    public static SavedAnswerId New() => new(Guid.NewGuid());
}

public readonly record struct ApiKeyId(Guid Value)
{
    public static ApiKeyId New() => new(Guid.NewGuid());
}

public readonly record struct JobId(Guid Value)
{
    public static JobId New() => new(Guid.NewGuid());
}

public readonly record struct UserInviteId(Guid Value)
{
    public static UserInviteId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
