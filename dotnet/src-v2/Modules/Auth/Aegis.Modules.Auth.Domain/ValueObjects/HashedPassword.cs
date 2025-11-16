using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;
using Aegis.Shared.Kernel.ValueObjects;

namespace Aegis.Modules.Auth.Domain.ValueObjects;

/// <summary>
/// Value object representing a hashed password
/// Includes the hash and salt for verification
/// </summary>
public sealed class HashedPassword : ValueObject
{
    public string Hash { get; }
    public string Salt { get; }

    private HashedPassword(string hash, string salt)
    {
        Hash = hash;
        Salt = salt;
    }

    /// <summary>
    /// Create a hashed password from hash and salt
    /// </summary>
    public static Result<HashedPassword> Create(string hash, string salt)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return Result.Failure<HashedPassword>(new Error(
                "HashedPassword.HashEmpty",
                "Password hash cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(salt))
        {
            return Result.Failure<HashedPassword>(new Error(
                "HashedPassword.SaltEmpty",
                "Password salt cannot be empty"));
        }

        return Result.Success(new HashedPassword(hash, salt));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Hash;
        yield return Salt;
    }
}
