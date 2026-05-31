using Ghost.Entities;
using Microsoft.UI.Dispatching;
using System.Diagnostics;

namespace Ghost.Editor.Core.Services;

public sealed class EditorTickEngine : IDisposable
{
    private readonly IEditorWorldService _worldService;
    private readonly DispatcherQueueTimer _timer;
    private bool _isStarted;

    // Time data
    private TimeData _timeData;
    private long _startTimestamp;
    private long _lastFrameTimestamp;

    public event Action? OnSafeZone;
    public event Action? OnSystemUpdate;
    public event Action? OnInspectorSync;
    public event Action? OnFireEvents;

    public EditorTickEngine(IEditorWorldService worldService)
    {
        _worldService = worldService;

        _timer = EditorApplication.DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16); // ~60Hz
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        _startTimestamp = Stopwatch.GetTimestamp();
        _lastFrameTimestamp = _startTimestamp;
        _timeData = new TimeData();

        _timer.Start();
        _isStarted = true;
    }

    private void OnTick(DispatcherQueueTimer sender, object args)
    {
        var now = Stopwatch.GetTimestamp();
        var dt = (float)(now - _lastFrameTimestamp) / Stopwatch.Frequency;
        var elapsed = (double)(now - _startTimestamp) / Stopwatch.Frequency;

        _timeData = new TimeData
        {
            FrameCount = _timeData.FrameCount + 1,
            DeltaTime = dt,
            ElapsedTime = elapsed
        };

        _lastFrameTimestamp = now;

        // Safe Zone (Drain Commands & ECB)
        _worldService.FlushCommands();
        OnSafeZone?.Invoke();

        // Editor Systems
        _worldService.EditorWorld.SystemManager.UpdateAll(_timeData);
        OnSystemUpdate?.Invoke();

        // Inspector Sync
        OnInspectorSync?.Invoke();

        // Fire Events
        _worldService.FirePendingEvents();
        OnFireEvents?.Invoke();

        _worldService.EditorWorld.AdvanceVersion();
    }

    public void Dispose()
    {
        if (_isStarted && _timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _isStarted = false;
        }
    }
}
