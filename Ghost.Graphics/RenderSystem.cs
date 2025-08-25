using System.Collections.Immutable;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics;

/// <summary>
/// Application-level render system that orchestrates multiple renderers
/// and handles frame synchronization
/// </summary>
public class RenderSystem : IDisposable
{
    private readonly struct FrameResource : IDisposable
    {
        public readonly AutoResetEvent CpuReadyEvent;
        public readonly AutoResetEvent GpuReadyEvent;

        public FrameResource()
        {
            CpuReadyEvent = new(false);
            GpuReadyEvent = new(true);
        }

        public void Dispose()
        {
            CpuReadyEvent?.Dispose();
            GpuReadyEvent?.Dispose();
        }
    }

    private const uint FRAME_COUNT = 2;

    private readonly IRenderDevice _device;
    private readonly FrameResource[] _frameResources;
    private readonly Thread _renderThread;
    private readonly AutoResetEvent _shutdownEvent;
    private ImmutableArray<IRenderer> _renderers;

    private uint _frameIndex;
    private uint _cpuFenceValue;
    private uint _gpuFenceValue;
    private bool _isRunning;
    private bool _disposed;

    public uint CPUFenceValue => _cpuFenceValue;
    public uint GPUFenceValue => _gpuFenceValue;
    public bool IsRunning => _isRunning;

    public RenderSystem(IRenderDevice device)
    {
        _device = device;
        _renderers = new();
        _shutdownEvent = new(false);

        // Create frame resources for synchronization
        _frameResources = new FrameResource[FRAME_COUNT];
        for (var i = 0; i < FRAME_COUNT; i++)
        {
            _frameResources[i] = new();
        }

        _renderThread = new(RenderLoop)
        {
            IsBackground = true,
            Name = "Graphics Render Thread",
            Priority = ThreadPriority.Normal
        };
    }

    ~RenderSystem()
    {
        Dispose();
    }

    public void AddRenderer(IRenderer renderer)
    {
        ImmutableInterlocked.Update(ref _renderers, renderers => renderers.Add(renderer));
    }

    public void RemoveRenderer(IRenderer renderer)
    {
        ImmutableInterlocked.Update(ref _renderers, renderers => renderers.Remove(renderer));
    }

    public void Start()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _renderThread.Start();
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _shutdownEvent.Set();

        if (_renderThread.IsAlive)
        {
            _renderThread.Join();
        }
    }

    public bool WaitForGPUReady(int timeOut = -1)
    {
        var eventIndex = (int)(_cpuFenceValue % FRAME_COUNT);
        return _frameResources[eventIndex].GpuReadyEvent.WaitOne(timeOut);
    }

    public void SignalCPUReady()
    {
        var eventIndex = (int)(_cpuFenceValue % FRAME_COUNT);
        _frameResources[eventIndex].CpuReadyEvent.Set();
        _cpuFenceValue++;
    }

    private void RenderLoop()
    {
        var waitHandles = new WaitHandle[] { null!, _shutdownEvent };

        while (_isRunning)
        {
            _frameIndex = _gpuFenceValue % FRAME_COUNT;
            var frameResource = _frameResources[_frameIndex];

            // Wait for either CPU ready signal or shutdown signal
            waitHandles[0] = frameResource.CpuReadyEvent;
            var waitResult = WaitHandle.WaitAny(waitHandles);

            // If shutdown was signaled or timeout occurred, exit the loop
            if (!_isRunning || waitResult == 1 || waitResult == WaitHandle.WaitTimeout)
            {
                break;
            }

            // Only proceed if CPU ready event was signaled
            if (waitResult == 0)
            {
                foreach (var renderer in _renderers)
                {
                    renderer.ExecutePendingResize();
                    renderer.Render();
                }

                _gpuFenceValue++;
                frameResource.GpuReadyEvent.Set();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();

        foreach (var frameResource in _frameResources)
        {
            frameResource.Dispose();
        }

        _shutdownEvent.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
