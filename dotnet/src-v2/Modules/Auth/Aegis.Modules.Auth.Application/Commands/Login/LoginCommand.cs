using Aegis.Shared.Contracts.DTOs.Auth;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Auth.Application.Commands.Login;

/// <summary>
/// Command to authenticate a user
/// </summary>
public record LoginCommand(
    string Username,
    string Password
) : IRequest<Result<LoginResponse>>;
