using Cirth.Shared;

namespace Cirth.Domain.Common;

public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public abstract class Entity<TId> : IHasDomainEvents where TId : struct
{
    public TId Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

/// <summary>Marker para raízes de agregado. Só raízes de agregado têm repositórios.</summary>
public interface IAggregateRoot { }
