using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;

namespace Ghost.Core;

public readonly struct Result
{
    private readonly bool _isSuccess;
    private readonly string? _message;

    public readonly bool IsSuccess => _isSuccess;
    public readonly bool IsFailure => !_isSuccess;

    public readonly string? Message => _message;

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

    public static Result<T> Success<T>(T value)
    {
        return Result<T>.Success(value);
    }

    public static Result<T> Failure<T>(string? message = null)
    {
        return Result<T>.Failure(message);
    }

    public static Result<T, S> Create<T, S>(T value, S status)
    {
        return new Result<T, S>(value, status);
    }

    public override string ToString() => IsSuccess ? "OK" : $"Error: {Message}";

    public static implicit operator bool(Result result) => result.IsSuccess;
}

public readonly struct Result<T>
{
    private readonly bool _isSuccess;
    private readonly T _value;
    private readonly string? _message;

    public readonly bool IsSuccess => _isSuccess;
    public readonly bool IsFailure => !_isSuccess;

    public readonly T Value => _value;
    public readonly string? Message => _message;

    public Result(bool success, T value, string? message = null)
    {
        _isSuccess = success;
        _value = value;
        _message = message;
    }

    public ref readonly T GetValueRef()
    {
        if (!IsSuccess)
        {
            throw new InvalidOperationException("Cannot get value from a failed Result.");
        }

        return ref Unsafe.AsRef(in _value);
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value);
    }

    public static Result<T> Failure(string? message = null)
    {
        return new Result<T>(false, default!, message);
    }

    public override string ToString() => IsSuccess ? $"OK: {Value}" : $"Error: {Message}";

    public static implicit operator Result<T>(T? data) => data is not null ? Success(data) : Failure(null);
    public static implicit operator Result<T>(Result result) => result.IsSuccess ? Success(default!) : Failure(result.Message);

    public static implicit operator bool(Result<T> result) => result.IsSuccess;
    public static implicit operator Result(Result<T> result) => result.IsSuccess ? Result.Success() : Result.Failure(result.Message);
}

public enum ResultStatus : byte
{
    Success,
    NotFound,
    InvalidArgument,
    InvalidState,
    InternalError,
    PermissionDenied,
    NotSupported,
    OutOfMemory,
    Timeout,
    Cancelled,
    UnknownError
}

public readonly struct Result<T, S>
{
    private readonly T _value;
    private readonly S _status;

    public T Value => _value;
    public S Status => _status;

    public Result(T value, S status)
    {
        _value = value;
        _status = status;
    }

    public ref readonly T GetValueRef()
    {
        return ref Unsafe.AsRef(in _value);
    }

    public static Result<T, S> Create(T value, S status)
    {
        return new Result<T, S>(value, status);
    }

    public override string ToString() => $"Value: {_value}, Status: {_status}";
}

public static class ResultExtensions
{
    public static void ThrowIfFailed(this Result result)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Operation failed: {result.Message}");
        }
    }

    public static T GetValueOrThrow<T>(this Result<T> result)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Operation failed: {result.Message}");
        }

        return result.Value;
    }

    public static T? GetValueOrDefault<T>(this Result<T> result, T? defaultValue = default)
    {
        return result.IsSuccess ? result.Value : defaultValue;
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
}
