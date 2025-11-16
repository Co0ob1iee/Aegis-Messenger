using Aegis.Modules.Auth.Application.Abstractions;
using Aegis.Modules.Auth.Domain.Repositories;
using Aegis.Shared.Contracts.DTOs.Auth;
using Aegis.Shared.Infrastructure.EventBus;
using Aegis.Shared.Infrastructure.Persistence;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Auth.Application.Commands.Login;

/// <summary>
/// Handler for LoginCommand
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IUnitOfWork unitOfWork,
        IEventBus eventBus)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // Try to find user by username or email
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        user ??= await _userRepository.GetByEmailAsync(request.Username, cancellationToken);

        if (user == null)
        {
            return Result.Failure<LoginResponse>(new Error(
                "Auth.InvalidCredentials",
                "Invalid username or password"));
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.Password))
        {
            return Result.Failure<LoginResponse>(new Error(
                "Auth.InvalidCredentials",
                "Invalid username or password"));
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Result.Failure<LoginResponse>(new Error(
                "Auth.UserInactive",
                "User account is inactive"));
        }

        // Record login
        var loginResult = user.RecordLogin();
        if (loginResult.IsFailure)
        {
            return Result.Failure<LoginResponse>(loginResult.Error);
        }

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiration = _jwtService.GetRefreshTokenExpiration();

        // Add refresh token to user
        var addTokenResult = user.AddRefreshToken(refreshToken, refreshTokenExpiration);
        if (addTokenResult.IsFailure)
        {
            return Result.Failure<LoginResponse>(addTokenResult.Error);
        }

        // Save changes
        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(cancellationToken);

        // Publish domain events
        await _eventBus.PublishManyAsync(user.DomainEvents, cancellationToken);
        user.ClearDomainEvents();

        // Return response
        var response = new LoginResponse(
            user.Id,
            accessToken,
            refreshToken,
            _jwtService.GetAccessTokenExpiration());

        return Result.Success(response);
    }
}
