using System.Text.RegularExpressions;
using Aegis.Shared.Kernel.Primitives;
using Aegis.Shared.Kernel.Results;

namespace Aegis.Shared.Kernel.ValueObjects;

/// <summary>
/// Value object representing a phone number
/// Supports international format with country code
/// </summary>
public sealed class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = new(
        @"^\+[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a phone number value object from a string
    /// </summary>
    /// <param name="phoneNumber">Phone number in E.164 format (e.g., +48123456789)</param>
    /// <returns>Result with PhoneNumber if valid, error otherwise</returns>
    public static Result<PhoneNumber> Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return Result.Failure<PhoneNumber>(new Error(
                "PhoneNumber.Empty",
                "Phone number cannot be empty"));
        }

        // Remove common formatting characters
        var normalized = phoneNumber.Replace(" ", "")
                                    .Replace("-", "")
                                    .Replace("(", "")
                                    .Replace(")", "");

        if (!PhoneRegex.IsMatch(normalized))
        {
            return Result.Failure<PhoneNumber>(new Error(
                "PhoneNumber.InvalidFormat",
                "Phone number must be in E.164 format (e.g., +48123456789)"));
        }

        return Result.Success(new PhoneNumber(normalized));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;
}
