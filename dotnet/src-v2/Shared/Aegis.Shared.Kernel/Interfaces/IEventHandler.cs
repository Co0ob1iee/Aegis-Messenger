namespace Aegis.Shared.Kernel.Interfaces;

/// <summary>
/// Handler for domain events
/// Implement this interface to handle specific domain events
/// </summary>
/// <typeparam name="TEvent">Type of domain event to handle</typeparam>
public interface IEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handle the domain event
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
