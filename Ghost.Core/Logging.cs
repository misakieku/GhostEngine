using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Ghost.Core;

public enum LogLevel
{
    Info,
    Warning,
    Error
}

internal readonly struct LogMessage
{
    public LogLevel Level
    {
        get;
    }

    public string Message
    {
        get;
    }

    public string? StackTrace
    {
        get;
    }

    public DateTime Timestamp
    {
        get;
    }

    public LogMessage(LogLevel level, string message, string? stackTrace = null)
    {
        Level = level;
        Message = message;
        StackTrace = stackTrace;
        Timestamp = DateTime.Now;
    }

    public override string ToString()
    {
        if (StackTrace != null)
        {
            return $"{Timestamp:HH:mm:ss} [{Level}] {Message}\n{StackTrace}";
        }

        return $"{Timestamp:HH:mm:ss} [{Level}] {Message}";
    }
}

internal interface ILogger
{
    public ReadOnlyObservableCollection<LogMessage> Logs
    {
        get;
    }

    public void Log(string message, LogLevel level);
    public void Log(Exception exception);
    public void Assert(bool condition, string message);
    public void Clear();
}

// TODO: Add file logging.
internal class LoggerImplementation : ILogger
{
    private readonly ObservableCollection<LogMessage> _logs = new();
    private readonly Lock _lock = new();

    public ReadOnlyObservableCollection<LogMessage> Logs => new(_logs);

    public void Log(string message, LogLevel level)
    {
        lock (_lock)
        {
            _logs.Add(new LogMessage(level, message));
        }
    }

    public void Log(Exception exception)
    {
        lock (_lock)
        {
            _logs.Add(new LogMessage(LogLevel.Error, exception.Message, exception.StackTrace));
        }
    }

    public void Assert(bool condition, string message)
    {
        lock (_lock)
        {
            if (!condition)
            {
                Log(message, LogLevel.Error);
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }
}

public static class Logger
{
    private static readonly ILogger s_logger = new LoggerImplementation();
    internal static ReadOnlyObservableCollection<LogMessage> Logs => s_logger.Logs;

    public static void Log(LogLevel level, object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", level);
    }

    public static void Log(LogLevel level, string message)
    {
        s_logger.Log(message, level);
    }

    public static void LogInfo(object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", LogLevel.Info);
    }

    public static void LogInfo(string message)
    {
        s_logger.Log(message, LogLevel.Info);
    }

    public static void LogWarning(object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", LogLevel.Warning);
    }

    public static void LogWarning(string message)
    {
        s_logger.Log(message, LogLevel.Warning);
    }

    public static void LogError(object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", LogLevel.Error);
    }

    public static void LogError(string message)
    {
        s_logger.Log(message, LogLevel.Error);
    }

    public static void LogError(Exception ex)
    {
        s_logger.Log(ex);
    }

    public static void Assert(bool condition, string message)
    {
        s_logger.Assert(condition, message);
    }

    public static void Clear()
    {
        s_logger.Clear();
    }
}
