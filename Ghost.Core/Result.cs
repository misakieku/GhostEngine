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

    public static Result Failure(string? message)
    {
        return new Result(false, message);
    }

    public void ThrowIfFailed()
    {
        if (!success)
        {
            throw new InvalidOperationException($"Operation failed: {message}");
        }
    }

    public override string ToString() => success ? "OK" : $"Error: {message}";
}

public readonly struct Result<T>
{
    public readonly bool success;
    public readonly T value;

    public readonly string? message;

    public Result(bool success, T data, string? message = null)
    {
        this.success = success;
        this.value = data;
        this.message = message;
    }

    public static Result<T> Success(T data)
    {
        return new Result<T>(true, data);
    }

    public static Result<T> Failure(string? message)
    {
        return new Result<T>(false, default!, message);
    }

    public void ThrowIfFailed()
    {
        if (!success)
        {
            throw new InvalidOperationException($"Operation failed: {message}");
        }
    }

    public override string ToString() => success ? $"OK: {value}" : $"Error: {message}";
}
