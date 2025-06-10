using Ghost.Engine.Models;
using System.Diagnostics;

namespace Ghost.Engine.Services;

internal enum LogChangeType
{
    LogAdded,
    LogRemoved,
    LogsCleared
}

public static class Logger
{

    private const int _MAX_LOGS = 4096;

    private static readonly List<LogMessage> _logs = new();
    internal static IReadOnlyList<LogMessage> Logs => _logs;

    internal static event Action<LogChangeType>? OnLogsUpdate;

    internal static bool HasStackTrace
    {
        get; set;
    }

    private static void LogInternal(LogLevel level, string message, int skipFrame)
    {
        if (_logs.Count >= _MAX_LOGS)
        {
            _logs.RemoveAt(0);
            OnLogsUpdate?.Invoke(LogChangeType.LogRemoved);
        }

        StackTrace? stackTrace = null;
        if (HasStackTrace)
        {
            stackTrace = new StackTrace(skipFrame, true);
        }

        var logMessage = new LogMessage(level, message, stackTrace?.ToString());
        _logs.Add(logMessage);

        OnLogsUpdate?.Invoke(LogChangeType.LogAdded);
    }

    private static void LogExceptionInternal(Exception ex)
    {
        if (_logs.Count >= _MAX_LOGS)
        {
            _logs.RemoveAt(0);
            OnLogsUpdate?.Invoke(LogChangeType.LogRemoved);
        }

        var logMessage = new LogMessage(LogLevel.Error, ex.Message, ex.StackTrace);
        _logs.Add(logMessage);

        OnLogsUpdate?.Invoke(LogChangeType.LogAdded);
    }

    public static void Log(LogLevel level, string message)
    {
        LogInternal(level, message, 2);
    }

    public static void LogInfo(string message)
    {
        LogInternal(LogLevel.Info, message, 3);
    }

    public static void LogWarning(string message)
    {
        LogInternal(LogLevel.Warning, message, 3);
    }

    public static void LogError(string message)
    {
        LogInternal(LogLevel.Error, message, 3);
    }

    public static void LogError(Exception ex)
    {
        LogExceptionInternal(ex);
    }

    internal static void Clear()
    {
        _logs.Clear();
        OnLogsUpdate?.Invoke(LogChangeType.LogsCleared);
    }
}