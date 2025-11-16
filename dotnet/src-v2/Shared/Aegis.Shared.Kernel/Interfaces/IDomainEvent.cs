using MediatR;

namespace Aegis.Shared.Kernel.Interfaces;

/// <summary>
/// Marker interface for domain events
/// Domain events represent something that happened in the domain
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Unique identifier for this event occurrence
    /// </summary>
    Guid EventId => Guid.NewGuid();

    /// <summary>
    /// When the event occurred
    /// </summary>
    DateTime OccurredAt => DateTime.UtcNow;
}
