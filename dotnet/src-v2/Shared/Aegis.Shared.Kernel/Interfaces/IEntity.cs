namespace Aegis.Shared.Kernel.Interfaces;

/// <summary>
/// Marker interface for entities
/// Entities have a unique identifier and are distinguished by their ID
/// </summary>
/// <typeparam name="TId">Type of the entity identifier</typeparam>
public interface IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Unique identifier of the entity
    /// </summary>
    TId Id { get; }

    /// <summary>
    /// Domain events raised by this entity
    /// </summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clear all domain events
    /// </summary>
    void ClearDomainEvents();
}
