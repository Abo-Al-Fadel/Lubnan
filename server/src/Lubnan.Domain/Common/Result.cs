namespace Lubnan.Domain.Common;

/// <summary>
/// The outcome of an operation that is allowed to fail.
/// </summary>
/// <remarks>
/// Exceptions are for bugs and for the genuinely exceptional. A slug that
/// nobody has published is not a bug — it is a normal Tuesday, and throwing
/// for it means the cost of a stack trace on a path that runs constantly, plus
/// a control flow the compiler cannot see. Returning a result puts the failure
/// in the signature, so a caller that ignores it is visible at review.
/// </remarks>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        // A successful result carrying an error, or a failure carrying none,
        // would make every downstream check ambiguous. Refuse to construct one.
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    public static Result<TValue> NotFound<TValue>(string code, string message) =>
        Failure<TValue>(Error.NotFound(code, message));
}

/// <summary>A result that carries a value when it succeeded.</summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;

    /// <summary>
    /// Throws when the result failed. That is deliberate: reading the value of
    /// a failure is a programming error, not a runtime condition.
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read the value of a failed result.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    /// <summary>Transform the value, carrying any failure through untouched.</summary>
    public Result<TNext> Map<TNext>(Func<TValue, TNext> map) =>
        IsSuccess ? Success(map(_value!)) : Failure<TNext>(Error);
}
