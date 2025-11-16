using Aegis.Modules.Auth.Application.Abstractions;
using Aegis.Modules.Auth.Domain.ValueObjects;
using Aegis.Shared.Cryptography.Interfaces;
using Aegis.Shared.Kernel.Results;

namespace Aegis.Modules.Auth.Infrastructure.Services;

/// <summary>
/// Password hashing service using PBKDF2
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly IKeyDerivation _keyDerivation;

    public PasswordHasher(IKeyDerivation keyDerivation)
    {
        _keyDerivation = keyDerivation;
    }

    public HashedPassword HashPassword(string password)
    {
        var (hash, salt) = _keyDerivation.HashPassword(password);

        var result = HashedPassword.Create(hash, salt);
        if (result.IsFailure)
        {
            throw new InvalidOperationException("Failed to create hashed password");
        }

        return result.Value;
    }

    public bool VerifyPassword(string password, HashedPassword hashedPassword)
    {
        return _keyDerivation.VerifyPassword(password, hashedPassword.Hash, hashedPassword.Salt);
    }
}
