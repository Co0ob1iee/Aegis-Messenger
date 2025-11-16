using Aegis.Modules.Messages.Domain.Entities;

namespace Aegis.Modules.Messages.Domain.Repositories;

/// <summary>
/// Repository interface for Message aggregate
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Get message by ID
    /// </summary>
    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages for a conversation
    /// </summary>
    Task<IReadOnlyList<Message>> GetConversationMessagesAsync(
        Guid conversationId,
        int limit = 50,
        DateTime? before = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unread messages for user
    /// </summary>
    Task<IReadOnlyList<Message>> GetUnreadMessagesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new message
    /// </summary>
    Task<Message> AddAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update message
    /// </summary>
    Task UpdateAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete message (soft delete)
    /// </summary>
    void Delete(Message message);

    /// <summary>
    /// Delete message permanently (hard delete)
    /// </summary>
    Task DeleteAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired disappearing messages
    /// </summary>
    Task<List<Message>> GetExpiredMessagesAsync(CancellationToken cancellationToken = default);
}
