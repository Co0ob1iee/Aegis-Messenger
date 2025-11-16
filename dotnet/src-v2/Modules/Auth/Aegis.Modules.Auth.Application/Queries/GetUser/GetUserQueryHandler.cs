using Aegis.Modules.Auth.Domain.Repositories;
using Aegis.Shared.Contracts.DTOs.Users;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Auth.Application.Queries.GetUser;

/// <summary>
/// Handler for GetUserQuery
/// </summary>
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(
        GetUserQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<UserDto>(new Error(
                "User.NotFound",
                $"User with ID {request.UserId} not found"));
        }

        var userDto = new UserDto(
            user.Id,
            user.Username.Value,
            user.Email.Value,
            user.PhoneNumber?.Value,
            false, // IsOnline - will be managed by separate module
            user.LastLoginAt,
            user.CreatedAt);

        return Result.Success(userDto);
    }
}
