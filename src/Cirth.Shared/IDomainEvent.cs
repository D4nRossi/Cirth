namespace Cirth.Shared;

/// <summary>
/// Marker interface for domain events. Implemented in Cirth.Shared to keep Domain zero-dep.
/// In Application layer these are dispatched via MediatR.INotification adapters.
/// </summary>
public interface IDomainEvent { }
