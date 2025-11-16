using Aegis.Modules.Auth.Application.Abstractions;
using Aegis.Modules.Auth.Domain.Repositories;
using Aegis.Shared.Contracts.DTOs.Auth;
using Aegis.Shared.Infrastructure.Persistence;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Auth.Application.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtService jwtService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // Find user by refresh token
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (user == null)
        {
            return Result.Failure<LoginResponse>(new Error(
                "RefreshToken.Invalid",
                "Invalid refresh token"));
        }

        // Get the specific refresh token
        var refreshToken = user.RefreshTokens
            .FirstOrDefault(t => t.Token == request.RefreshToken);

        if (refreshToken == null || !refreshToken.IsValid)
        {
            return Result.Failure<LoginResponse>(new Error(
                "RefreshToken.Invalid",
                "Refresh token is invalid or expired"));
        }

        // Revoke old refresh token
        refreshToken.Revoke();

        // Generate new tokens
        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiration = _jwtService.GetRefreshTokenExpiration();

        // Add new refresh token
        var addTokenResult = user.AddRefreshToken(newRefreshToken, refreshTokenExpiration);
        if (addTokenResult.IsFailure)
        {
            return Result.Failure<LoginResponse>(addTokenResult.Error);
        }

        // Save changes
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);

        // Return response
        var response = new LoginResponse(
            user.Id,
            newAccessToken,
            newRefreshToken,
            _jwtService.GetAccessTokenExpiration());

        return Result.Success(response);
    }
}
