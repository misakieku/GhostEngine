using Ghost.Graphics.RHI;
using System.Collections.Immutable;

namespace Ghost.Graphics;

public enum GraphicsAPI
{
    Direct3D12
}

/// <summary>
/// Application-level render system that orchestrates multiple renderers
/// and handles frame synchronization
/// </summary>
internal class RenderSystem
{
    private readonly struct FrameResource : IDisposable
    {
        public readonly AutoResetEvent cpuReadyEvent;
        public readonly AutoResetEvent gpuReadyEvent;

        public FrameResource()
        {
            cpuReadyEvent = new(false);
            gpuReadyEvent = new(true);
        }

        public void Dispose()
        {
            cpuReadyEvent?.Dispose();
            gpuReadyEvent?.Dispose();
        }
    }

    private const uint _FRAME_COUNT = 2;

    private readonly IGraphicsEngine _engine = null!;
    private readonly FrameResource[] _frameResources = null!;
    private readonly Thread _renderThread = null!;
    private readonly AutoResetEvent _shutdownEvent = null!;
    private ImmutableArray<IRenderer> _renderers;

    private uint _frameIndex;
    private uint _cpuFenceValue;
    private uint _gpuFenceValue;

    private bool _isRunning;
    private bool _disposed;

    public IGraphicsEngine GraphicsEngine => _engine;
    public uint CPUFenceValue => _cpuFenceValue;
    public uint GPUFenceValue => _gpuFenceValue;
    public bool IsRunning => _isRunning;

    public RenderSystem(GraphicsAPI api)
    {
        _engine = api switch
        {
            GraphicsAPI.Direct3D12 => new D3D12.D3D12GraphicsEngine(this),
            _ => throw new NotSupportedException($"Graphics API {api} is not supported.")
        };

        _renderers = ImmutableArray<IRenderer>.Empty;
        _shutdownEvent = new(false);

        // Create frame resources for synchronization
        _frameResources = new FrameResource[_FRAME_COUNT];
        for (var i = 0; i < _FRAME_COUNT; i++)
        {
            _frameResources[i] = new();
        }

        _renderThread = new(RenderLoop)
        {
            IsBackground = true,
            Name = "Graphics Render Thread",
            Priority = ThreadPriority.Normal
        };

        _disposed = true;
    }

    public IRenderer CreateRenderer()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var renderer = _engine.CreateRenderer();
        ImmutableInterlocked.Update(ref _renderers, renderers => renderers.Add(renderer));
        return renderer;
    }

    public void RemoveRenderer(IRenderer renderer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ImmutableInterlocked.Update(ref _renderers, renderers => renderers.Remove(renderer));
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _renderThread.Start();
    }

    public void Stop()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _shutdownEvent.Set();

        if (_renderThread.IsAlive)
        {
            _renderThread.Join();
        }
    }

    public bool WaitForGPUReady(int timeOut = -1)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var eventIndex = (int)(_cpuFenceValue % _FRAME_COUNT);
        return _frameResources[eventIndex].gpuReadyEvent.WaitOne(timeOut);
    }

    public void SignalCPUReady()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var eventIndex = (int)(_cpuFenceValue % _FRAME_COUNT);
        _frameResources[eventIndex].cpuReadyEvent.Set();
        _cpuFenceValue++;
    }

    private void RenderLoop()
    {
        var waitHandles = new WaitHandle[] { null!, _shutdownEvent };

        while (_isRunning)
        {
            _frameIndex = _gpuFenceValue % _FRAME_COUNT;
            var frameResource = _frameResources[_frameIndex];

            // Wait for either CPU ready signal or shutdown signal
            waitHandles[0] = frameResource.cpuReadyEvent;
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
                frameResource.gpuReadyEvent.Set();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();

        foreach (var frameResource in _frameResources)
        {
            frameResource.Dispose();
        }

        _shutdownEvent.Dispose();
        _disposed = true;
    }
}
