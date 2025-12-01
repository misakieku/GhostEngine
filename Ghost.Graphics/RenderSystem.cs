using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics;

public enum GraphicsAPI
{
    Direct3D12
}

public struct RenderingConfig
{
    public GraphicsAPI GraphicsAPI
    {
        get; set;
    }

    public uint FrameBufferCount
    {
        get; set;
    }
}

public interface IFenceSynchronizer
{
    uint CPUFenceValue
    {
        get;
    }

    uint GPUFenceValue
    {
        get;
    }

    uint FrameIndex
    {
        get;
    }

    uint MaxFrameLatency
    {
        get;
    }

    bool WaitForGPUReady(int timeOut = -1);
    void SignalCPUReady();
}

public interface IRenderSystem : IFenceSynchronizer, IDisposable
{
    IGraphicsEngine GraphicsEngine
    {
        get;
    }

    bool IsRunning
    {
        get;
    }

    void Start();
    void Stop();
}

/// <summary>
/// Application-level render system that orchestrates multiple renderers
/// and handles frame synchronization
/// </summary>
internal class RenderSystem : IRenderSystem
{
    private struct FrameResource : IDisposable
    {
        public readonly AutoResetEvent cpuReadyEvent;
        public readonly AutoResetEvent gpuReadyEvent;

        public FrameResource(ICommandBuffer cmd)
        {
            cpuReadyEvent = new AutoResetEvent(false);
            gpuReadyEvent = new AutoResetEvent(true);
        }

        public readonly void Dispose()
        {
            cpuReadyEvent.Dispose();
            gpuReadyEvent.Dispose();
        }
    }

    private readonly RenderingConfig _config;
    private readonly IGraphicsEngine _graphicsEngine;

    private readonly FrameResource[] _frameResources;
    private readonly Thread _renderThread;
    private readonly AutoResetEvent _shutdownEvent;

    private uint _frameIndex;
    private uint _cpuFenceValue;
    private uint _gpuFenceValue;

    private bool _isRunning;
    private bool _disposed;

    public IGraphicsEngine GraphicsEngine => _graphicsEngine;
    public bool IsRunning => _isRunning;

    public uint CPUFenceValue => _cpuFenceValue;
    public uint GPUFenceValue => _gpuFenceValue;
    public uint FrameIndex => _frameIndex;
    public uint MaxFrameLatency => _config.FrameBufferCount;

    public RenderSystem(RenderingConfig config)
    {
        _config = config;
        _graphicsEngine = config.GraphicsAPI switch
        {
            GraphicsAPI.Direct3D12 => new D3D12.D3D12GraphicsEngine(this),
            _ => throw new NotSupportedException($"Graphics API {config.GraphicsAPI} is not supported.")
        };

        _shutdownEvent = new AutoResetEvent(false);

        // Create frame resources for synchronization
        _frameResources = new FrameResource[config.FrameBufferCount];
        for (var i = 0; i < config.FrameBufferCount; i++)
        {
            _frameResources[i] = new FrameResource();
        }

        _renderThread = new Thread(RenderLoop)
        {
            IsBackground = true,
            Name = "Graphics Render Thread",
            Priority = ThreadPriority.Normal
        };

        _isRunning = false;
        _disposed = false;
    }

    ~RenderSystem()
    {
        Dispose();
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
        _renderThread.Join();
    }

    public bool WaitForGPUReady(int timeOut = -1)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var eventIndex = (int)(_cpuFenceValue % _config.FrameBufferCount);
        return _frameResources[eventIndex].gpuReadyEvent.WaitOne(timeOut);
    }

    public void SignalCPUReady()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var eventIndex = (int)(_cpuFenceValue % _config.FrameBufferCount);
        _frameResources[eventIndex].cpuReadyEvent.Set();
        _cpuFenceValue++;
    }

    private void RenderLoop()
    {
        var waitHandles = new WaitHandle[] { null!, _shutdownEvent };

        while (_isRunning)
        {
            _frameIndex = _gpuFenceValue % _config.FrameBufferCount;
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
                _graphicsEngine.RenderFrame();
//                if (result.IsFailure)
//                {
//                    // Terminate the render loop on failure
//                    _isRunning = false;
//#if DEBUG
//                    throw new InvalidOperationException($"RenderFrame failed: {result.Message}");
//#else
//                    Logger.LogError($"RenderFrame failed: {result.Message}");
//                    break;
//#endif
//                }

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

        _graphicsEngine.Dispose();
        _shutdownEvent.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
