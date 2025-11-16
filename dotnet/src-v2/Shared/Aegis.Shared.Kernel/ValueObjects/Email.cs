using System.Text.RegularExpressions;
using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;

namespace Aegis.Shared.Kernel.ValueObjects;

/// <summary>
/// Value object representing an email address
/// Ensures email is always valid
/// </summary>
public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an email value object from a string
    /// </summary>
    /// <param name="email">Email address string</param>
    /// <returns>Result with Email if valid, error otherwise</returns>
    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<Email>(new Error(
                "Email.Empty",
                "Email address cannot be empty"));
        }

        if (email.Length > 255)
        {
            return Result.Failure<Email>(new Error(
                "Email.TooLong",
                "Email address cannot exceed 255 characters"));
        }

        if (!EmailRegex.IsMatch(email))
        {
            return Result.Failure<Email>(new Error(
                "Email.InvalidFormat",
                "Email address format is invalid"));
        }

        return Result.Success(new Email(email.ToLowerInvariant()));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
