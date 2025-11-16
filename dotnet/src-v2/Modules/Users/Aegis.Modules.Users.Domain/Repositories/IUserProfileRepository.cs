using Aegis.Modules.Users.Domain.Entities;

namespace Aegis.Modules.Users.Domain.Repositories;

/// <summary>
/// Repository interface for UserProfile aggregate
/// </summary>
public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserProfile>> GetByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<UserProfile> AddAsync(UserProfile profile, CancellationToken cancellationToken = default);
    void Update(UserProfile profile);
    void Delete(UserProfile profile);
}
