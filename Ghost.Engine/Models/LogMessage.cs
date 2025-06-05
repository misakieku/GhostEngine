using System.Diagnostics;

namespace Ghost.Engine.Models;

public enum LogLevel
{
    Info,
    Warning,
    Error
}

internal class LogMessage
{
    public LogLevel Level
    {
        get; set;
    }

    public string Message
    {
        get; set;
    }

    public StackTrace? StackTrace
    {
        get; set;
    }

    public DateTime Timestamp
    {
        get; set;
    }

    public LogMessage(LogLevel level, string message, StackTrace? stackTrace = null)
    {
        Level = level;
        Message = message;
        StackTrace = stackTrace;
        Timestamp = DateTime.Now;
    }

    public override string ToString()
    {
        return $"{Timestamp:HH:mm:ss} [{Level}] {Message}";
    }

    public string ToStringWithStackTrace()
    {
        if (StackTrace == null)
        {
            return ToString();
        }

        return $"{ToString()}\n{StackTrace}";
    }
}