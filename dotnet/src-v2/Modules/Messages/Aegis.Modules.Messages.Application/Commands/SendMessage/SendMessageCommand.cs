using Aegis.Modules.Messages.Domain.Enums;
using Aegis.Shared.Contracts.DTOs.Messages;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Messages.Application.Commands.SendMessage;

/// <summary>
/// Command to send an encrypted message
/// </summary>
public record SendMessageCommand(
    Guid SenderId,
    Guid RecipientId,
    string PlainTextContent,
    MessageType Type = MessageType.Text,
    bool IsGroup = false,
    Guid? GroupId = null,
    Guid? ReplyToMessageId = null
) : IRequest<Result<MessageDto>>;
