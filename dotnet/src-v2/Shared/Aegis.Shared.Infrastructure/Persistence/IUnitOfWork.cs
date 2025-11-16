namespace Aegis.Shared.Infrastructure.Persistence;

/// <summary>
/// Unit of Work pattern for managing database transactions
/// Ensures all repository operations within a scope are committed atomically
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commit all pending changes to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities affected</returns>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback all pending changes
    /// </summary>
    Task RollbackAsync();

    /// <summary>
    /// Begin a new transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    Task RollbackTransactionAsync();
}
