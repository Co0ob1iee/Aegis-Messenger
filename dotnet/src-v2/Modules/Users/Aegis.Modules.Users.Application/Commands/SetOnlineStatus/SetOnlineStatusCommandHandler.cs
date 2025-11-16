using Aegis.Modules.Users.Domain.Repositories;
using Aegis.Shared.Infrastructure.Persistence;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Users.Application.Commands.SetOnlineStatus;

public class SetOnlineStatusCommandHandler : IRequestHandler<SetOnlineStatusCommand, Result>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SetOnlineStatusCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetOnlineStatusCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("UserProfile.NotFound", "User profile not found"));
        }

        var result = profile.SetStatus(request.Status);
        if (result.IsFailure) return result;

        _repository.Update(profile);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
