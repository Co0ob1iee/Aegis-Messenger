using System.Text.RegularExpressions;
using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;

namespace Aegis.Shared.Kernel.ValueObjects;

/// <summary>
/// Value object representing a username
/// Ensures username is always valid and follows business rules
/// </summary>
public sealed class Username : ValueObject
{
    private static readonly Regex UsernameRegex = new(
        @"^[a-zA-Z0-9_-]+$",
        RegexOptions.Compiled);

    private const int MinLength = 3;
    private const int MaxLength = 50;

    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a username value object from a string
    /// </summary>
    /// <param name="username">Username string</param>
    /// <returns>Result with Username if valid, error otherwise</returns>
    public static Result<Username> Create(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure<Username>(new Error(
                "Username.Empty",
                "Username cannot be empty"));
        }

        if (username.Length < MinLength)
        {
            return Result.Failure<Username>(new Error(
                "Username.TooShort",
                $"Username must be at least {MinLength} characters long"));
        }

        if (username.Length > MaxLength)
        {
            return Result.Failure<Username>(new Error(
                "Username.TooLong",
                $"Username cannot exceed {MaxLength} characters"));
        }

        if (!UsernameRegex.IsMatch(username))
        {
            return Result.Failure<Username>(new Error(
                "Username.InvalidFormat",
                "Username can only contain letters, numbers, underscores, and dashes"));
        }

        return Result.Success(new Username(username));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Username username) => username.Value;
}
