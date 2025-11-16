namespace Aegis.Shared.Kernel.Results;

/// <summary>
/// Represents the result of an operation that can succeed or fail
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Error details if the operation failed
    /// </summary>
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a success result
    /// </summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failure result with the specified error
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a success result with a value
    /// </summary>
    public static Result<TValue> Success<TValue>(TValue value) =>
        new(value, true, Error.None);

    /// <summary>
    /// Creates a failure result with a value type
    /// </summary>
    public static Result<TValue> Failure<TValue>(Error error) =>
        new(default, false, error);

    /// <summary>
    /// Creates a result from a nullable value
    /// Returns success if value is not null, failure otherwise
    /// </summary>
    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null
            ? Success(value)
            : Failure<TValue>(Error.NullValue);
}

/// <summary>
/// Represents the result of an operation that can succeed with a value or fail
/// </summary>
/// <typeparam name="TValue">Type of the value returned on success</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    /// <summary>
    /// The value if the operation succeeded
    /// Throws InvalidOperationException if accessed on a failed result
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result");

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Implicitly converts a value to a success result
    /// </summary>
    public static implicit operator Result<TValue>(TValue value) =>
        Success(value);
}
