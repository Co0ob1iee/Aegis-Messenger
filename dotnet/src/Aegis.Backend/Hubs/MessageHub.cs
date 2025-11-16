using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Aegis.Backend.Hubs;

/// <summary>
/// SignalR hub for real-time messaging
/// </summary>
[Authorize]
public class MessageHub : Hub
{
    private readonly ILogger<MessageHub> _logger;

    public MessageHub(ILogger<MessageHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            // Add user to their personal group (for receiving messages)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            _logger.LogInformation("User {UserId} connected: {ConnectionId}", userId, Context.ConnectionId);

            // Notify contacts that user is online
            await Clients.Others.SendAsync("UserOnline", userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            _logger.LogInformation("User {UserId} disconnected: {ConnectionId}", userId, Context.ConnectionId);

            // Notify contacts that user is offline
            await Clients.Others.SendAsync("UserOffline", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send encrypted message to a specific user
    /// </summary>
    public async Task SendMessage(string recipientId, byte[] encryptedContent, string messageType)
    {
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (senderId == null)
        {
            _logger.LogWarning("Unauthenticated message send attempt");
            return;
        }

        _logger.LogDebug("Message from {SenderId} to {RecipientId}", senderId, recipientId);

        // Send message to recipient's group
        await Clients.Group(recipientId).SendAsync("ReceiveMessage", new
        {
            senderId,
            encryptedContent,
            messageType,
            timestamp = DateTime.UtcNow
        });

        // Send delivery confirmation to sender
        await Clients.Caller.SendAsync("MessageSent", new
        {
            recipientId,
            timestamp = DateTime.UtcNow,
            status = "sent"
        });
    }

    /// <summary>
    /// Send encrypted message to a group
    /// </summary>
    public async Task SendGroupMessage(string groupId, byte[] encryptedContent)
    {
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (senderId == null)
            return;

        _logger.LogDebug("Group message from {SenderId} to group {GroupId}", senderId, groupId);

        // Send to all group members
        await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupMessage", new
        {
            groupId,
            senderId,
            encryptedContent,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Join a group
    /// </summary>
    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
        _logger.LogInformation("User joined group {GroupId}", groupId);
    }

    /// <summary>
    /// Leave a group
    /// </summary>
    public async Task LeaveGroup(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
        _logger.LogInformation("User left group {GroupId}", groupId);
    }

    /// <summary>
    /// Send typing indicator
    /// </summary>
    public async Task SendTypingIndicator(string recipientId, bool isTyping)
    {
        var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (senderId == null)
            return;

        await Clients.Group(recipientId).SendAsync("TypingIndicator", new
        {
            senderId,
            isTyping
        });
    }

    /// <summary>
    /// Mark message as read
    /// </summary>
    public async Task MarkAsRead(string senderId, string messageId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return;

        // Notify sender that message was read
        await Clients.Group(senderId).SendAsync("MessageRead", new
        {
            messageId,
            readBy = userId,
            timestamp = DateTime.UtcNow
        });
    }
}
