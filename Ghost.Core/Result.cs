using Misaki.HighPerformance.LowLevel;
using System.Runtime.CompilerServices;

namespace Ghost.Core;

public readonly struct Result
{
    private readonly string? _message;
    private readonly bool _isSuccess;

    public readonly string? Message => _message;
    public readonly bool IsSuccess => _isSuccess;
    public readonly bool IsFailure => !_isSuccess;

    public Result(bool success, string? message = null)
    {
        _isSuccess = success;
        _message = message;
    }

    public static Result Success()
    {
        return new Result(true);
    }

    public static Result Failure(string? message = null)
    {
        return new Result(false, message);
    }

    public static Result Failure(ErrorStatus status)
    {
        return new Result(false, status.ToString());
    }

    public static Result<T> Success<T>(T value)
    {
        return Result<T>.Success(value);
    }

    public static Result<T> Failure<T>(string? message = null)
    {
        return Result<T>.Failure(message);
    }

    public static Result<T> Failure<T>(ErrorStatus status)
    {
        return Result<T>.Failure(status.ToString());
    }

    public void Deconstruct(out bool success, out string? message)
    {
        success = IsSuccess;
        message = Message;
    }

    public override string ToString() => IsSuccess ? "OK" : $"Error: {Message}";

    public static implicit operator bool(Result result) => result.IsSuccess;
}

public readonly struct Result<T>
{
    private readonly T _value;
    private readonly string? _message;
    private readonly bool _isSuccess;

    public T Value
    {
        get
        {
#if DEBUG || GHOST_EDITOR
            if (!_isSuccess)
            {
                throw new InvalidOperationException($"Cannot access Value when Result is a failure. {_message}");
            }
#endif
            return _value;
        }
    }

    public readonly string? Message => _message;
    public readonly bool IsSuccess => _isSuccess;
    public readonly bool IsFailure => !_isSuccess;

    public Result(bool success, T value, string? message = null)
    {
        _isSuccess = success;
        _value = value;
        _message = message;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value);
    }

    public static Result<T> Failure(string? message = null)
    {
        return new Result<T>(false, default!, message);
    }

    public void Deconstruct(out bool success, out T value, out string? message)
    {
        success = IsSuccess;
        value = Value;
        message = Message;
    }

    public override string ToString() => IsSuccess ? $"OK: {Value}" : $"Error: {Message}";

    public static implicit operator Result<T>(T? data) => data is not null ? Success(data) : Failure(null);
    public static implicit operator Result<T>(Result result) => result.IsSuccess ? Success(default!) : Failure(result.Message);
    public static implicit operator bool(Result<T> result) => result.IsSuccess;
}

public enum ErrorStatus : byte
{
    None,
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

public readonly struct Result<T, E>
    where E : struct, Enum
{
    private readonly T _value;
    private readonly E _error;
    private readonly bool _isSuccess;

    public T Value
    {
        get
        {
#if DEBUG || GHOST_EDITOR
            if (!_isSuccess)
            {
                throw new InvalidOperationException($"Cannot access Value when Result is a failure. Error: {_error}");
            }
#endif
            return _value;
        }
    }

    public E Error => _error;
    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;

    public Result(T value, E status, bool isSuccess)
    {
        _value = value;
        _error = status;
        _isSuccess = isSuccess;
    }

    public static Result<T, E> Success(T value)
    {
        return new Result<T, E>(value, default, true);
    }

    public static Result<T, E> Failure(E status)
    {
        return new Result<T, E>(default!, status, false);
    }

    public void Deconstruct(out bool success, out T value, out E status)
    {
        success = IsSuccess;
        value = Value;
        status = Error;
    }

    public override string ToString() => $"Value: {_value}, Status: {_error}";

    public static implicit operator Result<T, E>(T data) => new(data, default, true);
    public static implicit operator Result<T, E>(E status) => new(default!, status, false);
    public static implicit operator bool(Result<T, E> result) => result.IsSuccess;
}

public readonly ref struct RefResult<T, E>
    where E : struct, Enum
{
    private readonly ref T _value;
    private readonly E _error;
    private readonly bool _isSuccess;

    public ref T Value
    {
        get
        {
#if DEBUG || GHOST_EDITOR
            if (!_isSuccess)
            {
                throw new InvalidOperationException($"Cannot access Value when Result is a failure. Error: {_error}");
            }
#endif
            return ref _value;
        }
    }

    public E Error => _error;
    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;

    public RefResult(ref T value, E error, bool isSuccess)
    {
        _value = ref value;
        _error = error;
        _isSuccess = isSuccess;
    }

    public static RefResult<T, E> Success(ref T value)
    {
        return new RefResult<T, E>(ref value, default, true);
    }

    public static RefResult<T, E> Failure(E error)
    {
        return new RefResult<T, E>(ref Unsafe.NullRef<T>(), error, false);
    }

    public void Deconstruct(out bool success, out Ref<T> value, out E status)
    {
        success = IsSuccess;
        value = new Ref<T>(ref Value);
        status = Error;
    }

    public override string ToString() => $"Value: {_value}, Status: {_error}";

    public static implicit operator RefResult<T, E>(Ref<T> data) => new(ref data.Get(), default, true);
    public static implicit operator RefResult<T, E>(E error) => new(ref Unsafe.NullRef<T>(), error, false);
    public static implicit operator bool(RefResult<T, E> result) => result.IsSuccess;
}

public static class ResultExtensions
{
    public static void ThrowIfFailed(this ErrorStatus result, [CallerArgumentExpression(nameof(result))] string? op = null)
    {
        if (result != ErrorStatus.None)
        {
            throw new InvalidOperationException($"{op} failed: {result}");
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

    public static T GetValueOrThrow<T, S>(this Result<T, S> result, [CallerArgumentExpression(nameof(result))] string? op = null)
        where S : struct, Enum
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"{op} failed: status {result.Error}");
        }

        return result.Value;
    }

    public static T? GetValueOrDefault<T>(this Result<T> result, T? defaultValue = default)
    {
        return result.IsSuccess ? result.Value : defaultValue;
    }

    public static T? GetValueOrDefault<T, S>(this Result<T, S> result, T? defaultValue = default)
        where S : struct, Enum
    {
        return result.IsSuccess ? result.Value : defaultValue;
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
        where E : struct, Enum
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
        where E : struct, Enum
    {
        if (result.IsFailure)
        {
            action(result.Error);
        }

        return result;
    }
}
