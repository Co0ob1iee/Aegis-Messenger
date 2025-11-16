using Aegis.Modules.Messages.Domain.Entities;

namespace Aegis.Modules.Messages.Domain.Repositories;

/// <summary>
/// Repository interface for Conversation aggregate
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Get conversation by ID
    /// </summary>
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get direct conversation between two users
    /// </summary>
    Task<Conversation?> GetDirectConversationAsync(
        Guid user1Id,
        Guid user2Id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all conversations for a user
    /// </summary>
    Task<IReadOnlyList<Conversation>> GetUserConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get group conversation by group ID
    /// </summary>
    Task<Conversation?> GetGroupConversationAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new conversation
    /// </summary>
    Task<Conversation> AddAsync(Conversation conversation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update conversation
    /// </summary>
    void Update(Conversation conversation);

    /// <summary>
    /// Delete conversation
    /// </summary>
    void Delete(Conversation conversation);
}
