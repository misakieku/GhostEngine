using System.Collections.ObjectModel;

namespace Ghost.Core;

public enum LogLevel
{
    Info,
    Warning,
    Error
}

public readonly struct LogMessage
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

public interface ILogger
{
    ReadOnlyObservableCollection<LogMessage> Logs
    {
        get;
    }

    void Log(string message, LogLevel level);
    void Log(Exception exception);
    void Assert(bool condition, string message);
    void Clear();
}

public static class Logger
{
    // TODO: Add file logging.
    private class LoggerImpl : ILogger
    {
        private readonly ObservableCollection<LogMessage> _logs = new();
        private readonly ReadOnlyObservableCollection<LogMessage> _readOnly;
        private readonly Lock _lock = new();

        public ReadOnlyObservableCollection<LogMessage> Logs => _readOnly;

        public LoggerImpl()
        {
            _readOnly = new ReadOnlyObservableCollection<LogMessage>(_logs);
        }

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

    private static readonly ILogger s_logger = new LoggerImpl();

    public static ReadOnlyObservableCollection<LogMessage> Logs => s_logger.Logs;

    public static void Log(LogLevel level, object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", level);
    }

    public static void Log(LogLevel level, string message)
    {
        s_logger.Log(message, level);
    }

    public static void Log(LogLevel level, string format, params object?[] args)
    {
        s_logger.Log(string.Format(format, args), level);
    }

    public static void LogInfo(object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", LogLevel.Info);
    }

    public static void LogInfo(string message)
    {
        s_logger.Log(message, LogLevel.Info);
    }

    public static void LogInfo(string format, params object?[] args)
    {
        s_logger.Log(string.Format(format, args), LogLevel.Info);
    }

    public static void LogWarning(object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", LogLevel.Warning);
    }

    public static void LogWarning(string message)
    {
        s_logger.Log(message, LogLevel.Warning);
    }

    public static void LogWarning(string format, params object?[] args)
    {
        s_logger.Log(string.Format(format, args), LogLevel.Warning);
    }

    public static void LogError(object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", LogLevel.Error);
    }

    public static void LogError(string message)
    {
        s_logger.Log(message, LogLevel.Error);
    }

    public static void LogError(string format, params object?[] args)
    {
        s_logger.Log(string.Format(format, args), LogLevel.Error);
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
