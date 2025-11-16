using Aegis.Modules.Groups.Domain.Entities;

namespace Aegis.Modules.Groups.Domain.Repositories;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Group>> GetUserGroupsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default);
    void Update(Group group);
    void Delete(Group group);
}
