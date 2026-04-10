using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ghost.Graphics;

internal enum GraphicsAPI
{
    Direct3D12
}

internal readonly struct RenderSystemDesc
{
    public GraphicsAPI GraphicsAPI
    {
        get; init;
    }

    public uint FrameBufferCount
    {
        get; init;
    }

    public required IRenderPipelineSettings InitialRenderPipelineSettings
    {
        get; init;
    }
}

/// <summary>
/// Application-level render system that orchestrates multiple renderers
/// and handles frame synchronization
/// </summary>
public class RenderSystem : IDisposable
{
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

        public IRenderPayload RenderPayload
        {
            get; set;
        }

        public readonly void Dispose()
        {
            CpuReadyEvent.Dispose();
            GpuReadyEvent.Dispose();
            CommandAllocator.Dispose();
            RenderPayload.Dispose();
        }
    }

    private readonly RenderSystemDesc _config;

    private readonly IGraphicsEngine _graphicsEngine;
    private readonly ResourceManager _resourceManager;
    private readonly SwapChainManager _swapChainManager;

    private readonly FrameResource[] _frameResources;
    private readonly Thread _renderThread;
    private readonly AutoResetEvent _shutdownEvent;

    private readonly ConcurrentDictionary<ISwapChain, uint2> _resizeRequest;

    private IRenderPipelineSettings _renderPipelineSettings;
    private IRenderPipeline _renderPipeline;

    private ulong _cpuFenceValue;
    private ulong _submittedFenceValue;

    private bool _isRunning;
    private bool _disposed;

    internal SwapChainManager SwapChainManager => _swapChainManager;

    public IGraphicsEngine GraphicsEngine => _graphicsEngine;
    public ResourceManager ResourceManager => _resourceManager;
    public bool IsRunning => _isRunning;

    public ulong CPUFenceValue => _cpuFenceValue;
    public ulong SubmittedFenceValue => _submittedFenceValue;

    public uint MaxFrameLatency => _config.FrameBufferCount;

    public IRenderPipelineSettings RenderPipelineSettings
    {
        get => _renderPipelineSettings;
        set
        {
            Debug.Assert(value != null, "RenderPipelineSettings cannot be set to null.");
            Debug.Assert(!_disposed, "Cannot set RenderPipelineSettings on a disposed RenderSystem.");

            if (value == _renderPipelineSettings)
            {
                return;
            }

            _renderPipeline?.Dispose();
            for (int i = 0; i < _frameResources.Length; i++)
            {
                _frameResources[i].RenderPayload?.Dispose();
            }

            _renderPipelineSettings = value;

            _renderPipeline = _renderPipelineSettings.CreatePipeline(this);
            for (var i = 0; i < _frameResources.Length; i++)
            {
                _frameResources[i].RenderPayload = _renderPipelineSettings.CreatePayload(this, _renderPipeline);
            }
        }
    }

    internal RenderSystem(RenderSystemDesc desc)
    {
        _config = desc;

        var engineDesc = new GraphicsEngineDesc
        {
            FrameBufferCount = desc.FrameBufferCount
        };

        switch (desc.GraphicsAPI)
        {
            case GraphicsAPI.Direct3D12:
                if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                {
                    _graphicsEngine = D3D12GraphicsEngineFactory.Create(engineDesc);
                }
                else
                {
                    // TODO: Fallback to Vulkan once it's implemented.
                    throw new PlatformNotSupportedException("Direct3D12 requires Windows 10 version 2004 (build 19041) or later.");
                }

                break;

            default:
                throw new NotSupportedException($"The specified graphics API '{desc.GraphicsAPI}' is not supported.");
        }

        _resourceManager = new ResourceManager(_graphicsEngine.Device, _graphicsEngine.ResourceAllocator, _graphicsEngine.ResourceDatabase);
        _swapChainManager = new SwapChainManager(_graphicsEngine);

        // Create frame resources for synchronization
        _frameResources = new FrameResource[desc.FrameBufferCount];
        for (var i = 0; i < desc.FrameBufferCount; i++)
        {
            _frameResources[i] = new FrameResource
            {
                CpuReadyEvent = new AutoResetEvent(false),
                GpuReadyEvent = new AutoResetEvent(true),
                CommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Graphics),
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

        _renderPipelineSettings = _config.InitialRenderPipelineSettings;

        _renderPipeline = _renderPipelineSettings.CreatePipeline(this);
        for (var i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i].RenderPayload = _renderPipelineSettings.CreatePayload(this, _renderPipeline);
        }

        _isRunning = false;
        _disposed = false;
    }

    ~RenderSystem()
    {
        Dispose();
    }

    private void RenderLoop()
    {
        void StopRenderLoop(Result result)
        {
            _isRunning = false;
            _shutdownEvent.Set();

#if DEBUG
            Debugger.Break();
#endif
            Logger.LogError($"Render failed: {result.Message}");
        }

        var waitHandles = new WaitHandle[] { null!, _shutdownEvent };

        while (_isRunning)
        {
            var frameIndex = (int)(_submittedFenceValue % _config.FrameBufferCount);
            ref var frameResource = ref _frameResources[frameIndex];

            try
            {

                // Wait for either CPU ready signal or shutdown signal
                waitHandles[0] = frameResource.CpuReadyEvent;
                var waitResult = WaitHandle.WaitAny(waitHandles);

                // If shutdown was signaled or timeout occurred, exit the loop
                if (!_isRunning || waitResult == 1 || waitResult == WaitHandle.WaitTimeout)
                {
                    break;
                }

                // Only proceed if CPU ready event was signaled
                if (waitResult != 0)
                {
                    continue;
                }

                _graphicsEngine.Device.GraphicsQueue.WaitForValue(frameResource.FenceValue);

                if (!_resizeRequest.IsEmpty)
                {
                    WaitIdle();

                    var keys = _resizeRequest.Keys.ToArray();
                    foreach (var swapChain in keys)
                    {
                        if (_resizeRequest.TryRemove(swapChain, out var newSize))
                        {
                            swapChain.Resize(newSize.x, newSize.y);
                        }
                    }
                }

                var completedFrame = _graphicsEngine.Device.GraphicsQueue.GetCompletedValue();
                if (_submittedFenceValue < completedFrame)
                {
                    _submittedFenceValue = completedFrame;
                }

                // Begin rendering for this frame
                frameResource.CommandAllocator.Reset();

                _resourceManager.BeginFrame(_submittedFenceValue);
                _graphicsEngine.BeginFrame(_submittedFenceValue);

                // Start recording commands

                // TODO: How can we support async compute and async copy?
                var cmd = _graphicsEngine.GetPooledCommandBuffer(CommandBufferType.Graphics);

                try
                {
                    cmd.Begin(frameResource.CommandAllocator);

                    var ctx = new RenderContext(_resourceManager, _graphicsEngine, cmd);

                    _renderPipeline.Render(ctx, frameIndex, frameResource.RenderPayload);
                    _swapChainManager.TransitionToPresent(cmd);

                    // End recording commands and submit
                    var r = cmd.End();
                    if (r.IsFailure)
                    {
                        StopRenderLoop(r);
                        break;
                    }

                    _graphicsEngine.Device.GraphicsQueue.Submit(cmd);
                    _swapChainManager.PresentAll(cmd);
                }
                finally
                {
                    _graphicsEngine.ReturnPooledCommandBuffer(cmd);
                }

                _submittedFenceValue++;
                frameResource.FenceValue = _graphicsEngine.Device.GraphicsQueue.Signal(_submittedFenceValue);
                frameResource.GpuReadyEvent.Set();

                completedFrame = _graphicsEngine.Device.GraphicsQueue.GetCompletedValue();

                // End the frame and retire resources based on the freshest observed GPU progress.
                _resourceManager.EndFrame(completedFrame);
                _graphicsEngine.EndFrame(completedFrame);

                frameResource.RenderPayload.Reset();
            }
            catch (Exception ex)
            {
                StopRenderLoop(Result.Failure($"An exception occurred during rendering: {ex.Message}"));
            }
        }
    }

    internal void Start()
    {
        Debug.Assert(!_disposed, "Cannot start a disposed RenderSystem.");

        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _renderThread.Start();
    }

    internal void Stop()
    {
        Debug.Assert(!_disposed, "Cannot stop a disposed RenderSystem.");

        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _shutdownEvent.Set();
        _renderThread.Join();
    }

    internal void SignalCPUReady()
    {
        Debug.Assert(!_disposed, "Cannot signal CPU ready on a disposed RenderSystem.");

        var eventIndex = (int)(_cpuFenceValue % _config.FrameBufferCount);
        ref var frameResource = ref _frameResources[eventIndex];

        frameResource.CpuReadyEvent.Set();
        _cpuFenceValue++;
    }

    internal void RequestSwapChainResize(ISwapChain swapChain, uint2 newSize)
    {
        Debug.Assert(!_disposed, "Cannot request swap chain resize on a disposed RenderSystem.");
        _resizeRequest.AddOrUpdate(swapChain, newSize, (_, _) => newSize);
    }

    internal bool TryAcquireCPUFrame()
    {
        Debug.Assert(!_disposed, "Cannot acquire CPU frame on a disposed RenderSystem.");

        var requiredGpuFence = _cpuFenceValue < _config.FrameBufferCount ? 0 : _cpuFenceValue - _config.FrameBufferCount + 1;

        if (requiredGpuFence > 0 && _graphicsEngine.Device.GraphicsQueue.GetCompletedValue() < requiredGpuFence)
        {
            return false;
        }

        var eventIndex = (int)(_cpuFenceValue % _config.FrameBufferCount);
        ref var frameResource = ref _frameResources[eventIndex];

        return true;
    }

    public bool WaitForGPUReady(int timeOut = -1)
    {
        Debug.Assert(!_disposed, "Cannot wait for GPU ready on a disposed RenderSystem.");

        var submittedFenceValue = Volatile.Read(ref _submittedFenceValue);
        if (submittedFenceValue == 0)
        {
            return true;
        }

        var eventIndex = (int)((submittedFenceValue - 1) % _config.FrameBufferCount);
        return _frameResources[eventIndex].GpuReadyEvent.WaitOne(timeOut);
    }

    public void WaitIdle()
    {
        Debug.Assert(!_disposed, "Cannot wait idle on a disposed RenderSystem.");
        foreach (var frameResource in _frameResources)
        {
            if (frameResource.FenceValue > 0)
            {
                _graphicsEngine.Device.GraphicsQueue.WaitForValue(frameResource.FenceValue);
            }
        }
    }

    public IRenderPayload GetCurrentFramePayload()
    {
        Debug.Assert(!_disposed, "Cannot get current frame payload from a disposed RenderSystem.");

        var eventIndex = (int)(_cpuFenceValue % _config.FrameBufferCount);
        ref var frameResource = ref _frameResources[eventIndex];

        return frameResource.RenderPayload;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();

        for (var i = 0; i < _frameResources.Length; i++)
        {
            ref var frameResource = ref _frameResources[i];
            frameResource.Dispose();
        }

        _renderPipeline.Dispose();

        _resourceManager.Dispose();
        _swapChainManager.Dispose();

        _graphicsEngine.Dispose();

        _shutdownEvent.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
