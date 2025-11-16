namespace Ghost.Core;

public readonly struct Result
{
    public readonly bool success;

    public readonly string? message;

    public Result(bool success, string? message = null)
    {
        this.success = success;
        this.message = message;
    }

    public static Result Success()
    {
        return new Result(true);
    }

    public static Result Fail(string? message)
    {
        return new Result(false, message);
    }

    public static Result<T> Success<T>(T value)
    {
        return Result<T>.Success(value);
    }

    public override string ToString() => success ? "OK" : $"Error: {message}";

    public static implicit operator bool(Result result) => result.success;
}

public readonly struct Result<T>
{
    public readonly bool success;
    public readonly T value;

    public readonly string? message;

    public Result(bool success, T value, string? message = null)
    {
        this.success = success;
        this.value = value;
        this.message = message;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value);
    }

    public static Result<T> Fail(string? message)
    {
        return new Result<T>(false, default!, message);
    }

    public override string ToString() => success ? $"OK: {value}" : $"Error: {message}";

    public static implicit operator Result<T>(T? data) => data is not null ? Success(data) : Fail(null);
    public static implicit operator Result<T>(Result result) => result.success ? Success(default!) : Fail(result.message);
}

public readonly struct Result<T, S>
{
    public readonly T value;
    public readonly S status;

    public Result(T value, S status)
    {
        this.value = value;
        this.status = status;
    }

    public static Result<T, S> Create(T value, S status)
    {
        return new Result<T, S>(value, status);
    }

    public override string ToString() => $"Value: {value}, Status: {status}";
}

public static class ResultExtensions
{
    public static void ThrowIfFailed(this Result result)
    {
        if (!result.success)
        {
            throw new InvalidOperationException($"Operation failed: {result.message}");
        }
    }

    public static T GetValueOrThrow<T>(this Result<T> result)
    {
        if (!result.success)
        {
            throw new InvalidOperationException($"Operation failed: {result.message}");
        }

        return result.value;
    }
}