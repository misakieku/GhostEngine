using Ghost.Engine.Models;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Ghost.Engine.Services;

internal enum LogChangeType
{
    LogAdded,
    LogRemoved,
    LogsCleared
}

internal readonly struct LogChangeContext(LogChangeType type, int index)
{
    public readonly LogChangeType changeType = type;
    public readonly int index = index;
}

public static class Logger
{

    private const int _MAX_LOGS = 4096;

    private static readonly List<LogMessage> _logs = new();
    internal static IReadOnlyList<LogMessage> Logs => _logs;

    internal static event Action<LogChangeContext>? OnLogsUpdate;

    internal static bool HasStackTrace
    {
        get; set;
    }

    private static StackTrace? CaptureStackTrace()
    {
        return HasStackTrace ? new StackTrace(1, true) : null;
    }

    private static void LogInternal(LogLevel level, object? message, StackTrace? stackTrace)
    {
        if (_logs.Count >= _MAX_LOGS)
        {
            _logs.RemoveAt(0);
            OnLogsUpdate?.Invoke(new(LogChangeType.LogRemoved, 0));
        }

        var logMessage = new LogMessage(level, message?.ToString(), stackTrace?.ToString());
        _logs.Add(logMessage);

        OnLogsUpdate?.Invoke(new(LogChangeType.LogAdded, _logs.Count - 1));
    }

    private static void LogExceptionInternal(Exception ex)
    {
        if (_logs.Count >= _MAX_LOGS)
        {
            _logs.RemoveAt(0);
            OnLogsUpdate?.Invoke(new(LogChangeType.LogRemoved, 0));
        }

        var logMessage = new LogMessage(LogLevel.Error, ex.Message, ex.StackTrace);
        _logs.Add(logMessage);

        OnLogsUpdate?.Invoke(new(LogChangeType.LogAdded, _logs.Count - 1));
    }

    public static void Log(LogLevel level, object? message)
    {
        LogInternal(level, message, CaptureStackTrace());
    }

    public static void LogInfo(object? message)
    {
        LogInternal(LogLevel.Info, message, CaptureStackTrace());
    }

    public static void LogWarning(object? message)
    {
        LogInternal(LogLevel.Warning, message, CaptureStackTrace());
    }

    public static void LogError(object? message)
    {
        LogInternal(LogLevel.Error, message, CaptureStackTrace());
    }

    public static void LogError(Exception ex)
    {
        LogExceptionInternal(ex);
    }

    public static void Assert([DoesNotReturnIf(false)] bool condition, object? message = null)
    {
        if (!condition)
        {
            LogInternal(LogLevel.Error, message ?? "Assertion failed", CaptureStackTrace());
        }
    }

    internal static void Clear()
    {
        _logs.Clear();
        OnLogsUpdate?.Invoke(new(LogChangeType.LogsCleared, -1));
    }
}