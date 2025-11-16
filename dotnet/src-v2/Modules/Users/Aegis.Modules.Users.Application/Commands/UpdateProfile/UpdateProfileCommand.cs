using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Users.Application.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl
) : IRequest<Result>;
