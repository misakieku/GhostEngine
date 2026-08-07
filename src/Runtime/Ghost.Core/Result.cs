using Misaki.HighPerformance.LowLevel;
using System.Runtime.CompilerServices;

namespace Ghost.Core;

public enum Error
{
    None = 0,
    NotFound,
    InvalidArgument,
    InvalidState,
    InternalError,
    PermissionDenied,
    NotSupported,
    OutOfMemory,
    Timeout,
    Cancelled,
    UnknownError,

    Success = None,
}

public readonly struct Result
{
    private readonly string? _message;
    private readonly bool _isSuccess;

    public readonly string? Message => _message;
    public readonly bool IsSuccess => _isSuccess;
    public readonly bool IsFailure => !IsSuccess;

    public Result(bool success, string? message = null)
    {
        _isSuccess = success;
        _message = message;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Success()
    {
        return new Result(true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Failure(string? message = null)
    {
        return new Result(false, message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Failure(Error status)
    {
        return new Result(false, status.ToString());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Success<T>(T value)
    {
        return Result<T>.Success(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Failure<T>(string? message = null)
    {
        return Result<T>.Failure(message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Failure<T>(Error status)
    {
        return Result<T>.Failure(status.ToString());
    }

    public static Result Aggregate(params ReadOnlySpan<Result> results)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                sb.AppendLine(result.Message);
            }
        }

        if (sb.Length == 0)
        {
            return Success();
        }

        return Failure(sb.ToString());
    }

    public void Deconstruct(out bool success, out string? message)
    {
        success = _isSuccess;
        message = _message;
    }

    public override string ToString() => _isSuccess ? "OK" : $"Error: {_message}";
    public static implicit operator bool(Result result) => result.IsSuccess;
}

public readonly struct Result<T>
{
    private readonly T _value;
    private readonly string? _message;
    private readonly bool _isSuccess;

    /// <summary>
    /// Gets the value. Undefined behavior if the result is a failure.
    /// </summary>
    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if GHOST_SAFETY_CHECKS
            if (IsFailure)
            {
                throw new InvalidOperationException($"Cannot access Value when Result is a failure. {_message}");
            }
#endif
            return _value;
        }
    }

    public readonly string? Message => _message;
    public readonly bool IsSuccess => _isSuccess;
    public readonly bool IsFailure => !IsSuccess;

    public Result(bool success, T value, string? message = null)
    {
        _isSuccess = success;
        _value = value;
        _message = message;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Failure(string? message = null)
    {
        return new Result<T>(false, default!, message);
    }

    public void Deconstruct(out bool success, out T value, out string? message)
    {
        success = _isSuccess;
        value = _value;
        message = _message;
    }

    public override string ToString() => _isSuccess ? $"OK: {_value}" : $"Error: {_message}";

    public static implicit operator Result<T>(T? data) => data is not null ? Success(data) : Failure(null);
    public static implicit operator Result<T>(Result result) => result.IsSuccess ? Success(default!) : Failure(result.Message);
    public static implicit operator Result(Result<T> result) => result.IsSuccess ? Result.Success() : Result.Failure(result.Message);
    public static implicit operator bool(Result<T> result) => result.IsSuccess;
}

public readonly struct Result<T, E>
    where E : struct
{
    private readonly T _value;
    private readonly E _error;

    /// <summary>
    /// Gets the value. Undefined behavior if the result is a failure.
    /// </summary>
    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if GHOST_SAFETY_CHECKS
            if (IsFailure)
            {
                throw new InvalidOperationException($"Cannot access Value when Result is a failure. Error: {_error}");
            }
#endif
            return _value;
        }
    }

    public E Error => _error;
    public bool IsSuccess => EqualityComparer<E>.Default.Equals(_error, default);
    public bool IsFailure => !IsSuccess;

    public Result(T value, E status)
    {
        _value = value;
        _error = status;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T, E> Success(T value)
    {
        return new Result<T, E>(value, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T, E> Failure(E status)
    {
        return new Result<T, E>(default!, status);
    }

    public void Deconstruct(out T value, out E status)
    {
        value = _value;
        status = _error;
    }

    public override string ToString() => $"Value: {_value}, Status: {_error}";

    public static implicit operator Result<T, E>(T data) => new(data, default);
    public static implicit operator Result<T, E>(E status) => new(default!, status);
    public static implicit operator bool(Result<T, E> result) => result.IsSuccess;
}

public readonly ref struct RefResult<T, E>
    where E : struct
{
    private readonly ref T _value;
    private readonly E _error;

    /// <summary>
    /// Gets a reference to the value. Undefined behavior if the result is a failure.
    /// </summary>
    public ref T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if GHOST_SAFETY_CHECKS
            if (IsFailure)
            {
                throw new InvalidOperationException($"Cannot access Value when Result is a failure. Error: {_error}");
            }
#endif
            return ref _value;
        }
    }

    public E Error => _error;
    public bool IsSuccess => EqualityComparer<E>.Default.Equals(_error, default);
    public bool IsFailure => !IsSuccess;

    public RefResult(ref T value, E error)
    {
        _value = ref value;
        _error = error;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefResult<T, E> Success(ref T value)
    {
        return new RefResult<T, E>(ref value, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefResult<T, E> Failure(E error)
    {
        return new RefResult<T, E>(ref Unsafe.NullRef<T>(), error);
    }

    public void Deconstruct(out Ref<T> value, out E status)
    {
        value = new Ref<T>(ref _value);
        status = _error;
    }

    public override string ToString() => $"Value: {_value}, Status: {_error}";

    public static implicit operator RefResult<T, E>(Ref<T> data) => new(ref data.Get(), default);
    public static implicit operator RefResult<T, E>(E error) => new(ref Unsafe.NullRef<T>(), error);
    public static implicit operator bool(RefResult<T, E> result) => result.IsSuccess;
}

public class NotFoundException : Exception
{
    public NotFoundException(string? message = null) : base(message ?? "The requested resource was not found.")
    {
    }
}

public static class ResultExtensions
{
    extension(Error error)
    {
        public bool IsSuccess => error == Error.None;
        public bool IsFailure => error != Error.None;

        public static bool operator true(Error err) => err != Error.None;
        public static bool operator false(Error err) => err == Error.None;

        public static Error FromHResult(int hr)
        {
            return hr switch
            {
                0 => Error.None,
                unchecked((int)0x80070002) => Error.NotFound,
                unchecked((int)0x80070057) => Error.InvalidArgument,
                unchecked((int)0x8007139F) => Error.InvalidState,
                unchecked((int)0x80004005) => Error.InternalError,
                unchecked((int)0x80070005) => Error.PermissionDenied,
                unchecked((int)0x80004001) => Error.NotSupported,
                unchecked((int)0x8007000E) => Error.OutOfMemory,
                unchecked((int)0x800705B4) => Error.Timeout,
                unchecked((int)0x800704C7) => Error.Cancelled,
                _ => Error.UnknownError
            };
        }

        public int ToHResult()
        {
            return error switch
            {
                Error.None => 0,
                Error.NotFound => unchecked((int)0x80070002),
                Error.InvalidArgument => unchecked((int)0x80070057),
                Error.InvalidState => unchecked((int)0x8007139F),
                Error.InternalError => unchecked((int)0x80004005),
                Error.PermissionDenied => unchecked((int)0x80070005),
                Error.NotSupported => unchecked((int)0x80004001),
                Error.OutOfMemory => unchecked((int)0x8007000E),
                Error.Timeout => unchecked((int)0x800705B4),
                Error.Cancelled => unchecked((int)0x800704C7),
                _ => unchecked((int)0x80004005)
            };
        }

        public static Error FromExpection(Exception ex)
        {
            return ex switch
            {
                DirectoryNotFoundException or FileNotFoundException or KeyNotFoundException => Error.NotFound,
                _ => Error.UnknownError,
            };
        }
    }

    public static void ThrowIfFailed(this Error error, [CallerArgumentExpression(nameof(error))] string? op = null)
    {
        switch (error)
        {
            case Error.NotFound:
                throw new NotFoundException(op);
            case Error.InvalidArgument:
                throw new ArgumentException(op);
            case Error.InvalidState:
                throw new InvalidOperationException(op);
            case Error.InternalError:
                throw new InvalidOperationException(op);
            case Error.PermissionDenied:
                throw new UnauthorizedAccessException(op);
            case Error.NotSupported:
                throw new NotSupportedException(op);
            case Error.OutOfMemory:
                throw new OutOfMemoryException(op);
            case Error.Timeout:
                throw new TimeoutException(op);
            case Error.Cancelled:
                throw new OperationCanceledException(op);
            case Error.UnknownError:
                throw new Exception(op);
        }
    }

    public static void ThrowIfFailed(this Result result, [CallerArgumentExpression(nameof(result))] string? op = null)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"{op} failed: {result.Message}");
        }
    }

    public static T GetValueOrThrow<T>(this Result<T> result, [CallerArgumentExpression(nameof(result))] string? op = null)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"{op} failed: {result.Message}");
        }

        return result.Value;
    }

    public static T GetValueOrThrow<T, E>(this Result<T, E> result, [CallerArgumentExpression(nameof(result))] string? op = null)
        where E : struct
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"{op} failed: status {result.Error}");
        }

        return result.Value;
    }

    public static ref T GetValueOrThrow<T, E>(this RefResult<T, E> result, [CallerArgumentExpression(nameof(result))] string? op = null)
        where E : struct
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"{op} failed: status {result.Error}");
        }

        return ref result.Value;
    }

    public static T? GetValueOrDefault<T>(this Result<T> result, T? defaultValue = default)
    {
        return result.IsSuccess ? result.Value : defaultValue;
    }

    public static T? GetValueOrDefault<T, E>(this Result<T, E> result, T? defaultValue = default)
        where E : struct
    {
        return result.IsSuccess ? result.Value : defaultValue;
    }

    public static ref T GetValueOrDefault<T, E>(this RefResult<T, E> result)
        where E : struct
    {
        return ref result.IsSuccess ? ref result.Value : ref Unsafe.NullRef<T>();
    }

    public static bool TryGetValue<T>(this Result<T> result, out T value)
    {
        if (result.IsSuccess)
        {
            value = result.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public static bool TryGetValue<T, S>(this Result<T, S> result, out T value)
        where S : struct, Enum
    {
        if (result.IsSuccess)
        {
            value = result.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public static Result OnSuccess(this Result result, Action action)
    {
        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    public static Result<T, E> OnSuccess<T, E>(this Result<T, E> result, Action<T> action)
        where E : struct
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    public static Result OnFailed(this Result result, Action<string?> action)
    {
        if (result.IsFailure)
        {
            action(result.Message);
        }

        return result;
    }

    public static Result<T> OnFailed<T>(this Result<T> result, Action<string?> action)
    {
        if (result.IsFailure)
        {
            action(result.Message);
        }

        return result;
    }

    public static Result<T, E> OnFailed<T, E>(this Result<T, E> result, Action<E> action)
        where E : struct
    {
        if (result.IsFailure)
        {
            action(result.Error);
        }

        return result;
    }

    public static Result Then(this Result result, Func<Result> func)
    {
        if (result.IsFailure)
        {
            return Result.Failure(result.Message);
        }

        return func();
    }

    public static Result<U> Then<T, U>(this Result<T> result, Func<T, Result<U>> func)
    {
        if (result.IsFailure)
        {
            return Result<U>.Failure(result.Message);
        }

        return func(result.Value);
    }

    public static Result<U, E> Then<T, U, E>(this Result<T, E> result, Func<T, Result<U, E>> func)
        where E : struct
    {
        if (result.IsFailure)
        {
            return Result<U, E>.Failure(result.Error);
        }

        return func(result.Value);
    }

    public static void Match(this Result result, Action onSuccess, Action<string?> onFailure)
    {
        if (result.IsSuccess)
        {
            onSuccess();
        }
        else
        {
            onFailure(result.Message);
        }
    }

    public static U Match<T, U>(this Result<T> result, Func<T, U> onSuccess, Func<string?, U> onFailure)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }
        else
        {
            return onFailure(result.Message);
        }
    }

    public static U Match<T, U, E>(this Result<T, E> result, Func<T, U> onSuccess, Func<E, U> onFailure)
        where E : struct
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }
        else
        {
            return onFailure(result.Error);
        }
    }
}
