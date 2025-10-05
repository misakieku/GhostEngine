using Ghost.UnitTest.Models;
using System.Diagnostics;

namespace Ghost.UnitTest.Services;

internal class LoggingService
{
    private const int MAX_LOGS = 4096;
    private static readonly Lazy<LoggingService> _instance = new(() => new LoggingService());

    private readonly List<LogItem> _logs = [];
    private readonly object _lockObject = new();

    public static LoggingService Instance => _instance.Value;

    public IReadOnlyList<LogItem> Logs
    {
        get
        {
            lock (_lockObject)
            {
                return _logs.AsReadOnly();
            }
        }
    }

    public bool CaptureStackTrace { get; set; } = false;

    public event Action<LogItem>? LogAdded;
    public event Action? LogsCleared;

    private LoggingService()
    {
    }

    private void AddLog(LogItem logItem)
    {
        lock (_lockObject)
        {
            if (_logs.Count >= MAX_LOGS)
            {
                _logs.RemoveAt(0);
            }

            _logs.Add(logItem);
        }

        // Invoke event outside of lock to prevent deadlock
        LogAdded?.Invoke(logItem);
    }

    private string? CaptureCurrentStackTrace()
    {
        if (!CaptureStackTrace)
            return null;

        var stackTrace = new StackTrace(skipFrames: 2, fNeedFileInfo: true);
        return stackTrace.ToString();
    }

    public void Log(LogLevel level, object? message)
    {
        var stackTrace = CaptureCurrentStackTrace();
        var logItem = new LogItem(level, message?.ToString() ?? string.Empty, stackTrace);
        AddLog(logItem);
    }

    public void LogInfo(object? message)
    {
        Log(LogLevel.Info, message);
    }

    public void LogWarning(object? message)
    {
        Log(LogLevel.Warning, message);
    }

    public void LogError(object? message)
    {
        Log(LogLevel.Error, message);
    }

    public void LogError(Exception exception)
    {
        var logItem = new LogItem(LogLevel.Error, exception.Message, exception.StackTrace);
        AddLog(logItem);
    }

    public void LogDebug(object? message)
    {
        Log(LogLevel.Debug, message);
    }

    public void Clear()
    {
        lock (_lockObject)
        {
            _logs.Clear();
        }

        LogsCleared?.Invoke();
    }

    // Static methods for easier usage throughout the test project
    public static void Info(object? message) => Instance.LogInfo(message);
    public static void Warning(object? message) => Instance.LogWarning(message);
    public static void Error(object? message) => Instance.LogError(message);
    public static void Error(Exception exception) => Instance.LogError(exception);
    public static void Debug(object? message) => Instance.LogDebug(message);
}
