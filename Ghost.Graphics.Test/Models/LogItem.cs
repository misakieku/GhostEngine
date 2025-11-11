namespace Ghost.Graphics.Test.Models;

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Debug
}

internal struct LogItem
{
    public LogLevel Level
    {
        get; init;
    }
    public string Message
    {
        get; init;
    }
    public DateTime Timestamp
    {
        get; init;
    }
    public string? StackTrace
    {
        get; init;
    }

    public LogItem(LogLevel level, string message, string? stackTrace = null)
    {
        Level = level;
        Message = message;
        StackTrace = stackTrace;
        Timestamp = DateTime.Now;
    }

    public override readonly string ToString()
    {
        return $"{Timestamp:HH:mm:ss.fff} [{Level}] {Message}";
    }

    public readonly string ToStringWithStackTrace()
    {
        if (string.IsNullOrEmpty(StackTrace))
        {
            return ToString();
        }

        return $"{ToString()}\n{StackTrace}";
    }
}