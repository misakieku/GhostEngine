using Ghost.Core;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Collections.Concurrent;

namespace Ghost.Graphics;

public interface IRenderSystem : IFenceSynchronizer, IDisposable
{
    IGraphicsEngine GraphicsEngine
    {
        get;
    }

    IResourceManager ResourceManager
    {
        get;
    }

    bool IsRunning
    {
        get;
    }

    void Start();
    void Stop();
    void RequestSwapChainResize(ISwapChain swapChain, uint2 newSize);
}

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

/// <summary>
/// Application-level render system that orchestrates multiple renderers
/// and handles frame synchronization
/// </summary>
internal class RenderSystem : IRenderSystem
{
    // TODO: Thread local command buffers.
    private struct FrameResource : IDisposable
    {
        public required AutoResetEvent CpuReadyEvent
        {
            get; init;
        }

        public required AutoResetEvent GpuReadyEvent
        {
            get; init;
        }

        public required ICommandAllocator CommandAllocator
        {
            get; init;
        }

        public ulong FenceValue
        {
            get; set;
        }

        public readonly void Dispose()
        {
            CpuReadyEvent.Dispose();
            GpuReadyEvent.Dispose();
            CommandAllocator.Dispose();
        }
    }

    private readonly RenderingConfig _config;
    private readonly IGraphicsEngine _graphicsEngine;
    private readonly IResourceManager _resourceManager;

    private readonly FrameResource[] _frameResources;
    private readonly Thread _renderThread;
    private readonly AutoResetEvent _shutdownEvent;
    private readonly ConcurrentDictionary<ISwapChain, uint2> _resizeRequest;

    private uint _frameIndex;
    private uint _cpuFenceValue;
    private uint _gpuFenceValue;

    private bool _isRunning;
    private bool _disposed;

    public IGraphicsEngine GraphicsEngine => _graphicsEngine;
    public IResourceManager ResourceManager => _resourceManager;
    public bool IsRunning => _isRunning;

    public uint CPUFenceValue => _cpuFenceValue;
    public uint GPUFenceValue => _gpuFenceValue;
    public uint FrameIndex => _frameIndex;
    public uint MaxFrameLatency => _config.FrameBufferCount;

    public RenderSystem(RenderingConfig config)
    {
        _config = config;

        switch (config.GraphicsAPI)
        {
            case GraphicsAPI.Direct3D12:
                if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                {
                    _graphicsEngine = D3D12GraphicsEngineFactory.Create(this);
                }
                else
                {
                    // TODO: Fallback to Vulkan once it's implemented.
                    throw new PlatformNotSupportedException("Direct3D12 requires Windows 10 version 2004 (build 19041) or later.");
                }

                break;

            default:
                throw new NotSupportedException($"The specified graphics API '{config.GraphicsAPI}' is not supported.");
        }

        _resourceManager = new ResourceManager(_graphicsEngine.ResourceAllocator, _graphicsEngine.ResourceDatabase);

        // Create frame resources for synchronization
        _frameResources = new FrameResource[config.FrameBufferCount];
        for (var i = 0; i < config.FrameBufferCount; i++)
        {
            _frameResources[i] = new FrameResource
            {
                CpuReadyEvent = new AutoResetEvent(false),
                GpuReadyEvent = new AutoResetEvent(true),
                CommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Graphics)
            };
        }

        _renderThread = new Thread(RenderLoop)
        {
            IsBackground = true,
            Name = "Graphics Render Thread",
            Priority = ThreadPriority.Normal
        };

        _shutdownEvent = new AutoResetEvent(false);
        _resizeRequest = new ConcurrentDictionary<ISwapChain, uint2>();

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

    public void RequestSwapChainResize(ISwapChain swapChain, uint2 newSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _resizeRequest.AddOrUpdate(swapChain, newSize, (_, _) => newSize);
    }

    public bool WaitForGPUReady(int timeOut = -1)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var eventIndex = (int)(_cpuFenceValue % _config.FrameBufferCount);
        return _frameResources[eventIndex].GpuReadyEvent.WaitOne(timeOut);
    }

    public void SignalCPUReady()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var eventIndex = (int)(_cpuFenceValue % _config.FrameBufferCount);
        _frameResources[eventIndex].CpuReadyEvent.Set();
        _cpuFenceValue++;
    }

    public void WaitIdle()
    {
        foreach (var frameResource in _frameResources)
        {
            if (frameResource.FenceValue > 0)
            {
                _graphicsEngine.Device.GraphicsQueue.WaitForValue(frameResource.FenceValue);
            }
        }
    }

    private void RenderLoop()
    {
        var waitHandles = new WaitHandle[] { null!, _shutdownEvent };

        while (_isRunning)
        {
            _frameIndex = _gpuFenceValue % _config.FrameBufferCount;
            ref var frameResource = ref _frameResources[_frameIndex];

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
                if (frameResource.FenceValue > 0)
                {
                    _graphicsEngine.Device.GraphicsQueue.WaitForValue(frameResource.FenceValue);
                }

                if (!_resizeRequest.IsEmpty)
                {
                    //WaitIdle();
                    _gpuFenceValue++;
                    var flushFence = _graphicsEngine.Device.GraphicsQueue.Signal(_gpuFenceValue);
                    _graphicsEngine.Device.GraphicsQueue.WaitForValue(flushFence);

                    // Sync the current frame resource to this new fence to keep state consistent
                    frameResource.FenceValue = flushFence;

                    foreach (var resource in _frameResources)
                    {
                        resource.CommandAllocator.Reset();
                    }

                    foreach (var kvp in _resizeRequest)
                    {
                        var swapChain = kvp.Key;
                        var newSize = kvp.Value;
                        swapChain.Resize(newSize.x, newSize.y);
                    }

                    _resizeRequest.Clear();
                }

                frameResource.CommandAllocator.Reset();

                var r = _graphicsEngine.RenderFrame(frameResource.CommandAllocator);
                if (r.IsFailure)
                {
                    _isRunning = false;
#if DEBUG
                    System.Diagnostics.Debugger.Break();
#endif
                    Logger.LogError($"RenderFrame failed: {r.Message}");
                }

                _gpuFenceValue++;

                frameResource.GpuReadyEvent.Set();
                frameResource.FenceValue = _graphicsEngine.Device.GraphicsQueue.Signal(_gpuFenceValue);
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
