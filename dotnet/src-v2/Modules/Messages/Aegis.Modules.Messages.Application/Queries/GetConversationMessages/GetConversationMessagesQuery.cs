using Aegis.Shared.Contracts.DTOs.Messages;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Messages.Application.Queries.GetConversationMessages;

/// <summary>
/// Query to get messages for a conversation
/// </summary>
public record GetConversationMessagesQuery(
    Guid ConversationId,
    Guid UserId,
    int Limit = 50,
    DateTime? Before = null
) : IRequest<Result<IReadOnlyList<MessageDto>>>;
