using Aegis.Shared.Kernel.Interfaces;

namespace Aegis.Shared.Kernel.Primitives;

/// <summary>
/// Base class for aggregate roots in Domain-Driven Design
/// Aggregate root is the entry point to the aggregate and ensures consistency
/// </summary>
/// <typeparam name="TId">Type of the aggregate root identifier</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    // Aggregate roots can have additional logic
    // for managing the aggregate boundary and consistency
}
