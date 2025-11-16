using Aegis.Shared.Contracts.DTOs.Auth;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Auth.Application.Commands.RefreshToken;

/// <summary>
/// Command to refresh access token using refresh token
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken
) : IRequest<Result<LoginResponse>>;
