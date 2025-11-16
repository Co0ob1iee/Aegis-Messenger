using Aegis.Modules.Users.Domain.Repositories;
using Aegis.Shared.Infrastructure.Persistence;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Users.Application.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProfileCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("UserProfile.NotFound", "User profile not found"));
        }

        var result = profile.UpdateProfile(request.DisplayName, request.Bio, request.AvatarUrl);
        if (result.IsFailure) return result;

        _repository.Update(profile);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
