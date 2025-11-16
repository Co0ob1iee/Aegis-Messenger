using Aegis.Modules.Messages.Application.Commands.SendMessage;
using Aegis.Modules.Messages.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Aegis.Modules.Messages.API.Hubs;

/// <summary>
/// SignalR hub for real-time messaging
/// </summary>
[Authorize]
public class MessagingHub : Hub
{
    private readonly IMediator _mediator;
    private readonly ILogger<MessagingHub> _logger;

    public MessagingHub(IMediator mediator, ILogger<MessagingHub> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Send message to user (real-time)
    /// </summary>
    public async Task SendMessage(Guid recipientId, string content)
    {
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var senderId))
        {
            throw new HubException("Unauthorized");
        }

        var command = new SendMessageCommand(
            senderId,
            recipientId,
            content,
            MessageType.Text);

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            throw new HubException(result.Error.Message);
        }

        // Notify recipient
        await Clients.User(recipientId.ToString()).SendAsync("ReceiveMessage", result.Value);

        // Confirm to sender
        await Clients.Caller.SendAsync("MessageSent", result.Value);

        _logger.LogInformation(
            "Message sent from {SenderId} to {RecipientId} via SignalR",
            senderId, recipientId);
    }

    /// <summary>
    /// Notify typing status
    /// </summary>
    public async Task NotifyTyping(Guid recipientId, bool isTyping)
    {
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var senderId))
        {
            return;
        }

        await Clients.User(recipientId.ToString()).SendAsync("UserTyping", senderId, isTyping);
    }

    /// <summary>
    /// User connected
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogInformation("User {UserId} connected to MessagingHub", userId);
            await Clients.Others.SendAsync("UserConnected", userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// User disconnected
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogInformation("User {UserId} disconnected from MessagingHub", userId);
            await Clients.Others.SendAsync("UserDisconnected", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
