using Aegis.Modules.Auth.Domain.ValueObjects;
using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;
using Aegis.Shared.Kernel.ValueObjects;
using Aegis.Shared.Contracts.Events.Auth;

namespace Aegis.Modules.Auth.Domain.Entities;

/// <summary>
/// User aggregate root
/// Manages user authentication and profile information
/// </summary>
public class User : AggregateRoot<Guid>
{
    private readonly List<RefreshToken> _refreshTokens = new();

    public Username Username { get; private set; }
    public Email Email { get; private set; }
    public HashedPassword Password { get; private set; }
    public PhoneNumber? PhoneNumber { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // EF Core constructor
    private User() { }

    private User(
        Guid id,
        Username username,
        Email email,
        HashedPassword password,
        PhoneNumber? phoneNumber)
    {
        Id = id;
        Username = username;
        Email = email;
        Password = password;
        PhoneNumber = phoneNumber;
        IsEmailVerified = false;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    public static Result<User> Create(
        Username username,
        Email email,
        HashedPassword password,
        PhoneNumber? phoneNumber = null)
    {
        var user = new User(
            Guid.NewGuid(),
            username,
            email,
            password,
            phoneNumber);

        // Raise domain event
        user.RaiseDomainEvent(new UserRegisteredEvent(
            user.Id,
            username.Value,
            email.Value,
            DateTime.UtcNow));

        return Result.Success(user);
    }

    /// <summary>
    /// Update user password
    /// </summary>
    public Result UpdatePassword(HashedPassword newPassword)
    {
        Password = newPassword;

        // Revoke all existing refresh tokens for security
        foreach (var token in _refreshTokens)
        {
            token.Revoke();
        }

        return Result.Success();
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    public Result UpdateProfile(Email? email = null, PhoneNumber? phoneNumber = null)
    {
        if (email != null)
        {
            Email = email;
            IsEmailVerified = false; // Require re-verification
        }

        if (phoneNumber != null)
        {
            PhoneNumber = phoneNumber;
        }

        return Result.Success();
    }

    /// <summary>
    /// Record successful login
    /// </summary>
    public Result RecordLogin()
    {
        if (!IsActive)
        {
            return Result.Failure(new Error(
                "User.Inactive",
                "User account is inactive"));
        }

        LastLoginAt = DateTime.UtcNow;

        // Raise domain event
        RaiseDomainEvent(new UserLoggedInEvent(
            Id,
            DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Add refresh token for user
    /// </summary>
    public Result<RefreshToken> AddRefreshToken(string token, DateTime expiresAt)
    {
        var refreshTokenResult = RefreshToken.Create(token, expiresAt);
        if (refreshTokenResult.IsFailure)
        {
            return Result.Failure<RefreshToken>(refreshTokenResult.Error);
        }

        var refreshToken = refreshTokenResult.Value;
        _refreshTokens.Add(refreshToken);

        // Limit number of active refresh tokens (security best practice)
        var activeTokens = _refreshTokens.Where(t => t.IsValid).ToList();
        if (activeTokens.Count > 5)
        {
            // Revoke oldest tokens
            var tokensToRevoke = activeTokens
                .OrderBy(t => t.CreatedAt)
                .Take(activeTokens.Count - 5);

            foreach (var oldToken in tokensToRevoke)
            {
                oldToken.Revoke();
            }
        }

        return Result.Success(refreshToken);
    }

    /// <summary>
    /// Revoke refresh token
    /// </summary>
    public Result RevokeRefreshToken(string token)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(t => t.Token == token);
        if (refreshToken == null)
        {
            return Result.Failure(new Error(
                "RefreshToken.NotFound",
                "Refresh token not found"));
        }

        refreshToken.Revoke();
        return Result.Success();
    }

    /// <summary>
    /// Verify email address
    /// </summary>
    public Result VerifyEmail()
    {
        IsEmailVerified = true;
        return Result.Success();
    }

    /// <summary>
    /// Deactivate user account
    /// </summary>
    public Result Deactivate()
    {
        IsActive = false;

        // Revoke all refresh tokens
        foreach (var token in _refreshTokens)
        {
            token.Revoke();
        }

        return Result.Success();
    }

    /// <summary>
    /// Activate user account
    /// </summary>
    public Result Activate()
    {
        IsActive = true;
        return Result.Success();
    }
}
