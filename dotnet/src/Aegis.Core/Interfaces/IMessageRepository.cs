using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aegis.Core.Models;

namespace Aegis.Core.Interfaces;

/// <summary>
/// Repository interface for message persistence and retrieval
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Insert a new encrypted message
    /// </summary>
    /// <param name="message">Message to insert</param>
    /// <returns>Inserted message with generated ID</returns>
    Task<Message> InsertAsync(Message message);

    /// <summary>
    /// Get messages for a specific conversation (1-on-1)
    /// </summary>
    /// <param name="userId">Current user ID</param>
    /// <param name="contactId">Contact user ID</param>
    /// <param name="limit">Maximum number of messages to retrieve</param>
    /// <param name="offset">Offset for pagination</param>
    /// <returns>List of messages</returns>
    Task<List<Message>> GetConversationMessagesAsync(Guid userId, Guid contactId, int limit = 50, int offset = 0);

    /// <summary>
    /// Get messages for a group
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="limit">Maximum number of messages</param>
    /// <param name="offset">Offset for pagination</param>
    /// <returns>List of group messages</returns>
    Task<List<Message>> GetGroupMessagesAsync(Guid groupId, int limit = 50, int offset = 0);

    /// <summary>
    /// Get a specific message by ID
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <returns>Message or null if not found</returns>
    Task<Message?> GetByIdAsync(Guid messageId);

    /// <summary>
    /// Update message status (sent, delivered, read)
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="status">New status</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateStatusAsync(Guid messageId, MessageStatus status);

    /// <summary>
    /// Delete a message
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(Guid messageId);

    /// <summary>
    /// Get unread message count for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Count of unread messages</returns>
    Task<int> GetUnreadCountAsync(Guid userId);

    /// <summary>
    /// Mark all messages in a conversation as read
    /// </summary>
    /// <param name="userId">Current user ID</param>
    /// <param name="contactId">Contact user ID</param>
    /// <returns>Number of messages marked as read</returns>
    Task<int> MarkConversationAsReadAsync(Guid userId, Guid contactId);

    /// <summary>
    /// Get messages sent after a specific timestamp (for sync)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="since">Timestamp</param>
    /// <returns>List of messages</returns>
    Task<List<Message>> GetMessagesSinceAsync(Guid userId, DateTime since);

    /// <summary>
    /// Delete messages older than specified date (for disappearing messages)
    /// </summary>
    /// <param name="before">Delete messages before this date</param>
    /// <returns>Number of messages deleted</returns>
    Task<int> DeleteOldMessagesAsync(DateTime before);
}
