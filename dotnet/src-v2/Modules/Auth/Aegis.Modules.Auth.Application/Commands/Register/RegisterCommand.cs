using Aegis.Shared.Contracts.DTOs.Auth;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Auth.Application.Commands.Register;

/// <summary>
/// Command to register a new user
/// </summary>
public record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string? PhoneNumber = null
) : IRequest<Result<RegisterResponse>>;
