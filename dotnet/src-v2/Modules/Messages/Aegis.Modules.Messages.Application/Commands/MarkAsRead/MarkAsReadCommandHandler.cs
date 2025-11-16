using Aegis.Modules.Messages.Domain.Repositories;
using Aegis.Shared.Infrastructure.EventBus;
using Aegis.Shared.Infrastructure.Persistence;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Messages.Application.Commands.MarkAsRead;

/// <summary>
/// Handler for MarkAsReadCommand
/// </summary>
public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Result>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public MarkAsReadCommandHandler(
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        IEventBus eventBus)
    {
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Result> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(request.MessageId, cancellationToken);
        if (message == null)
        {
            return Result.Failure(new Error(
                "Message.NotFound",
                $"Message with ID {request.MessageId} not found"));
        }

        var result = message.MarkAsRead();
        if (result.IsFailure)
        {
            return result;
        }

        _messageRepository.Update(message);
        await _unitOfWork.CommitAsync(cancellationToken);

        await _eventBus.PublishManyAsync(message.DomainEvents, cancellationToken);
        message.ClearDomainEvents();

        return Result.Success();
    }
}
