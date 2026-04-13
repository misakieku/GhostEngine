using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ghost.Core;

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Debug
}

public class LogMessage
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

public sealed class LogCollection : ReadOnlyObservableCollection<LogMessage>
{
    public LogCollection(ObservableCollection<LogMessage> list)
        : base(list)
    {
    }

    public event NotifyCollectionChangedEventHandler? LogChanged;

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        base.OnCollectionChanged(args);
        LogChanged?.Invoke(this, args);
    }
}

public interface ILogger
{
    LogCollection Logs
    {
        get;
    }

    public bool CaptureStackTrace
    {
        get; set;
    }

    void Log(string message, LogLevel level);
    void Log(Exception exception);
    void Assert(bool condition, string message);
    void Clear(bool includeFile = false);
}

public static class Logger
{
    // TODO: Add file logging.
    private class LoggerImpl : ILogger
    {
        private readonly ObservableCollection<LogMessage> _logs = new();
        private readonly LogCollection _readOnly;
        private readonly Lock _lock = new();

        public LogCollection Logs => _readOnly;

        public bool CaptureStackTrace
        {
            get; set;
        } = true;

        public LoggerImpl()
        {
            _readOnly = new LogCollection(_logs);
        }

        [StackTraceHidden]
        public void Log(string message, LogLevel level)
        {
            lock (_lock)
            {
                var stackTrace = CaptureStackTrace ? new StackTrace(true).ToString() : null;
                _logs.Add(new LogMessage(level, message, stackTrace));
            }
        }

        [StackTraceHidden]
        public void Log(Exception exception)
        {
            lock (_lock)
            {
                _logs.Add(new LogMessage(LogLevel.Error, exception.Message, exception.StackTrace));
            }
        }

        [StackTraceHidden]
        public void Assert(bool condition, string message)
        {
            if (!condition)
            {
                Log(message, LogLevel.Error);
            }
        }

        public void Clear(bool includeFile = false)
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }
    }

    private static readonly LoggerImpl s_logger = new LoggerImpl();

    public static ILogger Impl => s_logger;
    public static LogCollection Logs => s_logger.Logs;

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(LogLevel level, object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", level);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(LogLevel level, string message)
    {
        s_logger.Log(message, level);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Log(LogLevel level, string format, params object?[] args)
    {
        s_logger.Log(string.Format(format, args), level);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", LogLevel.Info);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(string message)
    {
        s_logger.Log(message, LogLevel.Info);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(string format, params object?[] args)
    {
        s_logger.Log(string.Format(format, args), LogLevel.Info);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warning(object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", LogLevel.Warning);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warning(string message)
    {
        s_logger.Log(message, LogLevel.Warning);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warning(string format, params object?[] args)
    {
        s_logger.Log(string.Format(format, args), LogLevel.Warning);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", LogLevel.Error);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(string message)
    {
        s_logger.Log(message, LogLevel.Error);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(string format, params object?[] args)
    {
        s_logger.Log(string.Format(format, args), LogLevel.Error);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(Exception ex)
    {
        s_logger.Log(ex);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Assert(bool condition, [CallerArgumentExpression(nameof(condition))] string? message = null)
    {
        s_logger.Assert(condition, message ?? "null");
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("DEBUG")]
    [Conditional("GHOST_EDITOR")]
    public static void Debug(object? message)
    {
        s_logger.Log(message?.ToString() ?? "null", LogLevel.Debug);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("DEBUG")]
    [Conditional("GHOST_EDITOR")]
    public static void Debug(string message)
    {
        s_logger.Log(message, LogLevel.Debug);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("DEBUG")]
    [Conditional("GHOST_EDITOR")]
    public static void Debug(string format, params object?[] args)
    {
        s_logger.Log(string.Format(format, args), LogLevel.Debug);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Conditional("DEBUG")]
    [Conditional("GHOST_EDITOR")]
    public static void DebugAssert([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] string? message = null)
    {
        s_logger.Assert(condition, message?.ToString() ?? "null");
#if DEBUG
        if (!condition)
        {
            System.Diagnostics.Debug.Fail(message ?? "Assertion failed.");
        }
#endif
    }
}
