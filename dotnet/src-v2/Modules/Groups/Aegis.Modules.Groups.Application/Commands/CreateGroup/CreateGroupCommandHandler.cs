using Aegis.Modules.Groups.Domain.Entities;
using Aegis.Modules.Groups.Domain.Repositories;
using Aegis.Shared.Contracts.DTOs.Groups;
using Aegis.Shared.Infrastructure.EventBus;
using Aegis.Shared.Infrastructure.Persistence;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Groups.Application.Commands.CreateGroup;

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, Result<GroupDto>>
{
    private readonly IGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public CreateGroupCommandHandler(IGroupRepository repository, IUnitOfWork unitOfWork, IEventBus eventBus)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Result<GroupDto>> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var groupResult = Group.Create(request.Name, request.Description, request.CreatedBy);
        if (groupResult.IsFailure)
            return Result.Failure<GroupDto>(groupResult.Error);

        var group = groupResult.Value;

        await _repository.AddAsync(group, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        await _eventBus.PublishManyAsync(group.DomainEvents, cancellationToken);
        group.ClearDomainEvents();

        var dto = new GroupDto(group.Id, group.Name, group.Description, group.CreatedBy, group.CreatedAt, group.Members.Count);

        return Result.Success(dto);
    }
}
