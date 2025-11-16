namespace Aegis.Shared.Kernel.Results;

/// <summary>
/// Represents an error with a code and message
/// </summary>
public sealed record Error
{
    /// <summary>
    /// Special error representing no error (success case)
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Special error representing a null value
    /// </summary>
    public static readonly Error NullValue = new(
        "Error.NullValue",
        "The specified result value is null");

    /// <summary>
    /// Error code (e.g., "User.NotFound", "Message.InvalidFormat")
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; }

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static implicit operator string(Error error) => error.Code;

    public override string ToString() => $"{Code}: {Message}";
}
