using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aegis.Core.Interfaces;
using Aegis.Core.Models;
using Aegis.Data.Context;
using Aegis.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aegis.Data.Repositories;

/// <summary>
/// Repository implementation for message persistence
/// </summary>
public class MessageRepository : IMessageRepository
{
    private readonly AegisDbContext _context;
    private readonly ILogger<MessageRepository> _logger;

    public MessageRepository(AegisDbContext context, ILogger<MessageRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Message> InsertAsync(Message message)
    {
        var entity = MapToEntity(message);
        await _context.Messages.AddAsync(entity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Inserted message {MessageId}", message.Id);
        return MapToModel(entity);
    }

    public async Task<List<Message>> GetConversationMessagesAsync(
        Guid userId, Guid contactId, int limit = 50, int offset = 0)
    {
        var messages = await _context.Messages
            .Where(m => (m.SenderId == userId && m.ReceiverId == contactId) ||
                       (m.SenderId == contactId && m.ReceiverId == userId))
            .OrderByDescending(m => m.Timestamp)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return messages.Select(MapToModel).ToList();
    }

    public async Task<List<Message>> GetGroupMessagesAsync(
        Guid groupId, int limit = 50, int offset = 0)
    {
        var messages = await _context.Messages
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.Timestamp)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return messages.Select(MapToModel).ToList();
    }

    public async Task<Message?> GetByIdAsync(Guid messageId)
    {
        var entity = await _context.Messages.FindAsync(messageId);
        return entity != null ? MapToModel(entity) : null;
    }

    public async Task<bool> UpdateStatusAsync(Guid messageId, MessageStatus status)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null) return false;

        message.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid messageId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null) return false;

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Messages
            .Where(m => m.ReceiverId == userId && m.Status != MessageStatus.Read)
            .CountAsync();
    }

    public async Task<int> MarkConversationAsReadAsync(Guid userId, Guid contactId)
    {
        var messages = await _context.Messages
            .Where(m => m.SenderId == contactId &&
                       m.ReceiverId == userId &&
                       m.Status != MessageStatus.Read)
            .ToListAsync();

        foreach (var message in messages)
        {
            message.Status = MessageStatus.Read;
        }

        await _context.SaveChangesAsync();
        return messages.Count;
    }

    public async Task<List<Message>> GetMessagesSinceAsync(Guid userId, DateTime since)
    {
        var messages = await _context.Messages
            .Where(m => (m.SenderId == userId || m.ReceiverId == userId) &&
                       m.Timestamp > since)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return messages.Select(MapToModel).ToList();
    }

    public async Task<int> DeleteOldMessagesAsync(DateTime before)
    {
        var messages = await _context.Messages
            .Where(m => m.Timestamp < before)
            .ToListAsync();

        _context.Messages.RemoveRange(messages);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} old messages", messages.Count);
        return messages.Count;
    }

    // Mapping methods
    private static MessageEntity MapToEntity(Message model)
    {
        return new MessageEntity
        {
            Id = model.Id,
            SenderId = model.SenderId,
            ReceiverId = model.ReceiverId,
            EncryptedContent = model.EncryptedContent,
            Type = model.Type,
            Timestamp = model.Timestamp,
            IsGroup = model.IsGroup,
            GroupId = model.GroupId,
            Status = model.Status,
            IsSealedSender = model.IsSealedSender,
            FileAttachmentId = model.FileAttachmentId,
            ServerMessageId = model.ServerMessageId
        };
    }

    private static Message MapToModel(MessageEntity entity)
    {
        return new Message
        {
            Id = entity.Id,
            SenderId = entity.SenderId,
            ReceiverId = entity.ReceiverId,
            EncryptedContent = entity.EncryptedContent,
            Type = entity.Type,
            Timestamp = entity.Timestamp,
            IsGroup = entity.IsGroup,
            GroupId = entity.GroupId,
            Status = entity.Status,
            IsSealedSender = entity.IsSealedSender,
            FileAttachmentId = entity.FileAttachmentId,
            ServerMessageId = entity.ServerMessageId
        };
    }
}
