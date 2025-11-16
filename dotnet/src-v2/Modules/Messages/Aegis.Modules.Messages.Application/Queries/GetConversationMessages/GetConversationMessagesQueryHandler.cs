using Aegis.Modules.Messages.Domain.Repositories;
using Aegis.Shared.Contracts.DTOs.Messages;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Messages.Application.Queries.GetConversationMessages;

/// <summary>
/// Handler for GetConversationMessagesQuery
/// </summary>
public class GetConversationMessagesQueryHandler
    : IRequestHandler<GetConversationMessagesQuery, Result<IReadOnlyList<MessageDto>>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;

    public GetConversationMessagesQueryHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> Handle(
        GetConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        // Verify user is participant
        var conversation = await _conversationRepository.GetByIdAsync(
            request.ConversationId,
            cancellationToken);

        if (conversation == null)
        {
            return Result.Failure<IReadOnlyList<MessageDto>>(new Error(
                "Conversation.NotFound",
                "Conversation not found"));
        }

        if (!conversation.IsParticipant(request.UserId))
        {
            return Result.Failure<IReadOnlyList<MessageDto>>(new Error(
                "Conversation.Unauthorized",
                "User is not a participant of this conversation"));
        }

        // Get messages
        var messages = await _messageRepository.GetConversationMessagesAsync(
            request.ConversationId,
            request.Limit,
            request.Before,
            cancellationToken);

        var dtos = messages.Select(m => new MessageDto(
            m.Id,
            m.SenderId,
            m.RecipientId,
            m.Content.Ciphertext,
            m.SentAt,
            m.DeliveredAt,
            m.ReadAt,
            m.IsGroupMessage,
            m.GroupId
        )).ToList();

        return Result.Success<IReadOnlyList<MessageDto>>(dtos);
    }
}
