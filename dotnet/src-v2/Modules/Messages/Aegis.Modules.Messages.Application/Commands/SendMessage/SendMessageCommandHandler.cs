using Aegis.Modules.Messages.Application.Abstractions;
using Aegis.Modules.Messages.Domain.Entities;
using Aegis.Modules.Messages.Domain.Repositories;
using Aegis.Modules.Messages.Domain.ValueObjects;
using Aegis.Shared.Contracts.DTOs.Messages;
using Aegis.Shared.Infrastructure.EventBus;
using Aegis.Shared.Infrastructure.Persistence;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Messages.Application.Commands.SendMessage;

/// <summary>
/// Handler for SendMessageCommand
/// Encrypts and sends message using Signal Protocol
/// </summary>
public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<MessageDto>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public SendMessageCommandHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IEncryptionService encryptionService,
        IUnitOfWork unitOfWork,
        IEventBus eventBus)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _encryptionService = encryptionService;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Result<MessageDto>> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        // Get or create conversation
        Conversation? conversation;

        if (request.IsGroup && request.GroupId.HasValue)
        {
            conversation = await _conversationRepository.GetGroupConversationAsync(
                request.GroupId.Value,
                cancellationToken);

            if (conversation == null)
            {
                return Result.Failure<MessageDto>(new Error(
                    "Conversation.NotFound",
                    "Group conversation not found"));
            }
        }
        else
        {
            conversation = await _conversationRepository.GetDirectConversationAsync(
                request.SenderId,
                request.RecipientId,
                cancellationToken);

            if (conversation == null)
            {
                var createResult = Conversation.CreateDirect(request.SenderId, request.RecipientId);
                if (createResult.IsFailure)
                {
                    return Result.Failure<MessageDto>(createResult.Error);
                }

                conversation = createResult.Value;
                await _conversationRepository.AddAsync(conversation, cancellationToken);
            }
        }

        // Encrypt message
        var (ciphertext, isPreKeyMessage) = await _encryptionService.EncryptMessageAsync(
            request.SenderId,
            request.RecipientId,
            request.PlainTextContent);

        var encryptedContentResult = EncryptedContent.Create(ciphertext, isPreKeyMessage);
        if (encryptedContentResult.IsFailure)
        {
            return Result.Failure<MessageDto>(encryptedContentResult.Error);
        }

        // Create message
        var messageResult = Message.Create(
            conversation.Id,
            request.SenderId,
            request.RecipientId,
            encryptedContentResult.Value,
            request.Type,
            request.IsGroup,
            request.GroupId,
            request.ReplyToMessageId);

        if (messageResult.IsFailure)
        {
            return Result.Failure<MessageDto>(messageResult.Error);
        }

        var message = messageResult.Value;

        // Update conversation
        conversation.UpdateLastMessage(message.Id);

        // Save to database
        await _messageRepository.AddAsync(message, cancellationToken);
        _conversationRepository.Update(conversation);
        await _unitOfWork.CommitAsync(cancellationToken);

        // Publish domain events
        await _eventBus.PublishManyAsync(message.DomainEvents, cancellationToken);
        message.ClearDomainEvents();

        // Return DTO
        var dto = new MessageDto(
            message.Id,
            message.SenderId,
            message.RecipientId,
            message.Content.Ciphertext,
            message.SentAt,
            message.DeliveredAt,
            message.ReadAt,
            message.IsGroupMessage,
            message.GroupId);

        return Result.Success(dto);
    }
}
