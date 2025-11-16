using Aegis.Modules.Groups.Domain.Entities;
using Aegis.Modules.Groups.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Groups.Infrastructure.Persistence.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly GroupsDbContext _context;

    public GroupRepository(GroupsDbContext context)
    {
        _context = context;
    }

    public async Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Groups.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> GetUserGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var groups = await _context.Groups.ToListAsync(cancellationToken);
        return groups.Where(g => g.IsMember(userId)).ToList();
    }

    public async Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        await _context.Groups.AddAsync(group, cancellationToken);
        return group;
    }

    public void Update(Group group)
    {
        _context.Groups.Update(group);
    }

    public void Delete(Group group)
    {
        _context.Groups.Remove(group);
    }
}
