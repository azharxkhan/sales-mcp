namespace SalesMcp.Domain.Common;

/// <summary>
/// The outcome of an operation that can fail for a business reason. Business-rule failures are
/// results, not exceptions; exceptions are reserved for bugs.
/// </summary>
public class Result
{
    private static readonly Result _successInstance = new(null);

    private readonly DomainError? _error;

    /// <summary>Creates a result. Use the static factory methods instead.</summary>
    protected Result(DomainError? error) => _error = error;

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess => _error is null;

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => _error is not null;

    /// <summary>The failure. Reading it on a success is a bug and throws.</summary>
    /// <exception cref="InvalidOperationException">The result is a success.</exception>
    public DomainError Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    /// <summary>A success with no value.</summary>
    public static Result Success() => _successInstance;

    /// <summary>A success carrying <paramref name="value"/>.</summary>
    public static Result<T> Success<T>(T value) => new(value, null);

    /// <summary>A failure with no value.</summary>
    public static Result Failure(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    /// <summary>A failure with no value.</summary>
    public static Result Failure(string code, string message) => Failure(new DomainError(code, message));

    /// <summary>A failure for an operation that would have returned a <typeparamref name="T"/>.</summary>
    public static Result<T> Failure<T>(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(default, error);
    }

    /// <summary>A failure for an operation that would have returned a <typeparamref name="T"/>.</summary>
    public static Result<T> Failure<T>(string code, string message) => Failure<T>(new DomainError(code, message));

    /// <inheritdoc />
    public override string ToString() => IsSuccess ? "Success" : $"Failure({Error})";
}

/// <summary>The outcome of an operation that returns a <typeparamref name="T"/> or fails for a business reason.</summary>
/// <typeparam name="T">The value type on success.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, DomainError? error)
        : base(error) => _value = value;

    /// <summary>The value. Reading it on a failure is a bug and throws.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"A failed result has no value ({Error}).");

    /// <summary>Transforms the value of a success; passes a failure through unchanged.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsSuccess ? Success(map(Value)) : Failure<TOut>(Error);
    }

    /// <summary>Chains another operation that can fail; passes a failure through unchanged.</summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        return IsSuccess ? next(Value) : Failure<TOut>(Error);
    }

    /// <summary>Handles both outcomes and returns a single value.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<DomainError, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }

    /// <inheritdoc />
    public override string ToString() => IsSuccess ? $"Success({_value})" : $"Failure({Error})";
}
