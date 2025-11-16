using Aegis.Modules.Users.Domain.Entities;
using Aegis.Modules.Users.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Users.Infrastructure.Persistence.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly UsersDbContext _context;

    public UserProfileRepository(UsersDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles.FindAsync(new object[] { userId }, cancellationToken);
    }

    public async Task<IReadOnlyList<UserProfile>> GetByIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        return await _context.UserProfiles.Where(p => userIds.Contains(p.Id)).ToListAsync(cancellationToken);
    }

    public async Task<UserProfile> AddAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await _context.UserProfiles.AddAsync(profile, cancellationToken);
        return profile;
    }

    public void Update(UserProfile profile)
    {
        _context.UserProfiles.Update(profile);
    }

    public void Delete(UserProfile profile)
    {
        _context.UserProfiles.Remove(profile);
    }
}
