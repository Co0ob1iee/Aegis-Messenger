using Aegis.Modules.Messages.Domain.Entities;
using Aegis.Modules.Messages.Domain.Enums;
using Aegis.Modules.Messages.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Modules.Messages.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Message aggregate
/// </summary>
public class MessageRepository : IMessageRepository
{
    private readonly MessagesDbContext _context;

    public MessageRepository(MessagesDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetConversationMessagesAsync(
        Guid conversationId,
        int limit = 50,
        DateTime? before = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted);

        if (before.HasValue)
        {
            query = query.Where(m => m.SentAt < before.Value);
        }

        return await query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetUnreadMessagesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Where(m => m.RecipientId == userId
                && m.Status != MessageStatus.Read
                && !m.IsDeleted)
            .OrderBy(m => m.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Message> AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
        return message;
    }

    public void Update(Message message)
    {
        _context.Messages.Update(message);
    }

    public async Task UpdateAsync(Message message, CancellationToken cancellationToken = default)
    {
        _context.Messages.Update(message);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void Delete(Message message)
    {
        _context.Messages.Remove(message);
    }

    public async Task DeleteAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await GetByIdAsync(messageId, cancellationToken);
        if (message != null)
        {
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<Message>> GetExpiredMessagesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.Messages
            .Where(m => m.DisappearsAt.HasValue
                && m.DisappearsAt.Value <= now
                && !m.IsDeleted)
            .ToListAsync(cancellationToken);
    }
}
