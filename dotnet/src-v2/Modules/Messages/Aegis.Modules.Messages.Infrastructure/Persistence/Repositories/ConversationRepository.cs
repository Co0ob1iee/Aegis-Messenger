using Aegis.Modules.Messages.Domain.Entities;
using Aegis.Modules.Messages.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Messages.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Conversation aggregate
/// </summary>
public class ConversationRepository : IConversationRepository
{
    private readonly MessagesDbContext _context;

    public ConversationRepository(MessagesDbContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Conversation?> GetDirectConversationAsync(
        Guid user1Id,
        Guid user2Id,
        CancellationToken cancellationToken = default)
    {
        // Direct conversations have exactly 2 participants
        var conversations = await _context.Conversations
            .Where(c => !c.IsGroup)
            .ToListAsync(cancellationToken);

        return conversations.FirstOrDefault(c =>
            c.ParticipantIds.Count == 2 &&
            c.ParticipantIds.Contains(user1Id) &&
            c.ParticipantIds.Contains(user2Id));
    }

    public async Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conversations = await _context.Conversations
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(cancellationToken);

        return conversations
            .Where(c => c.IsParticipant(userId))
            .ToList();
    }

    public async Task<Conversation?> GetGroupConversationAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(c => c.GroupId == groupId, cancellationToken);
    }

    public async Task<Conversation> AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _context.Conversations.AddAsync(conversation, cancellationToken);
        return conversation;
    }

    public void Update(Conversation conversation)
    {
        _context.Conversations.Update(conversation);
    }

    public void Delete(Conversation conversation)
    {
        _context.Conversations.Remove(conversation);
    }
}
