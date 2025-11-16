using Aegis.Modules.Security.Application.Services;
using Aegis.Modules.Security.Domain.Enums;
using Aegis.Shared.Contracts.Events.Auth;
using Aegis.Shared.Contracts.Events.Messages;
using Aegis.Shared.Kernel.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aegis.Modules.Security.Application.EventHandlers;

/// <summary>
/// Automatically logs all domain events to security audit log
/// This provides a complete audit trail of all domain-level operations
/// </summary>
public class DomainEventAuditHandler :
    INotificationHandler<UserRegisteredEvent>,
    INotificationHandler<UserLoggedInEvent>,
    INotificationHandler<MessageSentEvent>,
    INotificationHandler<MessageDeliveredEvent>,
    INotificationHandler<MessageReadEvent>
{
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<DomainEventAuditHandler> _logger;

    public DomainEventAuditHandler(
        ISecurityAuditService auditService,
        ILogger<DomainEventAuditHandler> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Logging UserRegisteredEvent for user {UserId}", notification.UserId);

        await _auditService.LogSuccessAsync(
            SecurityEventType.AccountCreated,
            notification.UserId,
            details: $"New account registered: {notification.Username} ({notification.Email})",
            cancellationToken: cancellationToken);
    }

    public async Task Handle(UserLoggedInEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Logging UserLoggedInEvent for user {UserId}", notification.UserId);

        await _auditService.LogSuccessAsync(
            SecurityEventType.LoginSuccess,
            notification.UserId,
            details: $"User logged in successfully",
            cancellationToken: cancellationToken);
    }

    public async Task Handle(MessageSentEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Logging MessageSentEvent: {MessageId} from {SenderId} to {RecipientId}",
            notification.MessageId,
            notification.SenderId,
            notification.RecipientId);

        var messageType = notification.IsGroupMessage ? "group message" : "direct message";
        var details = notification.IsGroupMessage
            ? $"Sent {messageType} to group {notification.GroupId}"
            : $"Sent {messageType} to user {notification.RecipientId}";

        await _auditService.LogSuccessAsync(
            SecurityEventType.MessageSent,
            notification.SenderId,
            details: details,
            relatedEntityId: notification.MessageId,
            relatedEntityType: "Message",
            cancellationToken: cancellationToken);
    }

    public async Task Handle(MessageDeliveredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Logging MessageDeliveredEvent: {MessageId} to {RecipientId}",
            notification.MessageId,
            notification.RecipientId);

        // We don't log every delivery to avoid noise
        // This could be enabled based on configuration
        await Task.CompletedTask;
    }

    public async Task Handle(MessageReadEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Logging MessageReadEvent: {MessageId} by {RecipientId}",
            notification.MessageId,
            notification.RecipientId);

        // We don't log every read to avoid noise
        // This could be enabled based on configuration
        await Task.CompletedTask;
    }
}
