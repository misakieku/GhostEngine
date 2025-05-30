namespace Ghost.Data.Models;

public readonly struct Result
{
    public readonly bool success;

    public readonly string? message;

    public Result(bool success, string? message = null)
    {
        this.success = success;
        this.message = message;
    }

    public static Result OK()
    {
        return new Result(true);
    }

    public static Result Error(string? message)
    {
        return new Result(false, message);
    }

    public override string ToString() => success ? "OK" : $"Error: {message}";
}

public readonly struct Result<T>
{
    public readonly bool success;
    public readonly T? data;

    public readonly string? message;

    public Result(bool success, T? data, string? message = null)
    {
        this.success = success;
        this.data = data;
        this.message = message;
    }

    public static Result<T> OK(T data)
    {
        return new Result<T>(true, data);
    }

    public static Result<T> Error(string? message)
    {
        return new Result<T>(false, default, message);
    }

    public override string ToString() => success ? $"OK: {data}" : $"Error: {message}";
}
