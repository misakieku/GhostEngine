using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.RenderPipeline;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ghost.Graphics;

internal enum GraphicsAPI
{
    Direct3D12
}

internal struct RenderSystemDesc
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

        public readonly void Dispose()
        {
            CpuReadyEvent.Dispose();
            GpuReadyEvent.Dispose();
            CommandAllocator.Dispose();
        }
    }

    private readonly RenderSystemDesc _config;

    private readonly IGraphicsEngine _graphicsEngine;
    private readonly ResourceManager _resourceManager;

    private readonly FrameResource[] _frameResources;
    private readonly Thread _renderThread;
    private readonly AutoResetEvent _shutdownEvent;

    private UnsafeArray<UnsafeList<RenderRequest>> _renderRequests;
    private readonly ConcurrentDictionary<ISwapChain, uint2> _resizeRequest;

    private IRenderPipelineSettings _renderPipelineSettings;
    private IRenderPipeline _renderPipeline;

    private uint _frameIndex;
    private ulong _cpuFenceValue;
    private ulong _gpuFenceValue;

    private bool _isRunning;
    private bool _disposed;

    public IGraphicsEngine GraphicsEngine => _graphicsEngine;
    public ResourceManager ResourceManager => _resourceManager;
    public bool IsRunning => _isRunning;

    public ulong CPUFenceValue => _cpuFenceValue;
    public ulong GPUFenceValue => _gpuFenceValue;
    public uint FrameIndex => _frameIndex;
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

            _renderPipelineSettings = value;
            _renderPipeline = _renderPipelineSettings.CreatePipeline(this);
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

        _resourceManager = new ResourceManager(_graphicsEngine.ResourceAllocator, _graphicsEngine.ResourceDatabase);

        // Create frame resources for synchronization
        _frameResources = new FrameResource[desc.FrameBufferCount];
        for (var i = 0; i < desc.FrameBufferCount; i++)
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
        _renderRequests = new UnsafeArray<UnsafeList<RenderRequest>>((int)desc.FrameBufferCount, Allocator.Persistent);
        for (var i = 0; i < desc.FrameBufferCount; i++)
        {
            _renderRequests[i] = new UnsafeList<RenderRequest>(2, Allocator.Persistent);
        }

        _renderPipelineSettings = new GhostRenderPipelineSettings();
        _renderPipeline = _renderPipelineSettings.CreatePipeline(this);

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
            Debug.Assert(result.IsFailure, "StopRenderLoop should only be called with a failure result.");

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
            _frameIndex = (uint)(_gpuFenceValue % _config.FrameBufferCount);
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
            if (waitResult != 0)
            {
                continue;
            }

            _graphicsEngine.Device.GraphicsQueue.WaitForValue(frameResource.FenceValue);

            if (!_resizeRequest.IsEmpty)
            {
                _gpuFenceValue++;
                var flushFence = _graphicsEngine.Device.GraphicsQueue.Signal(_gpuFenceValue);
                _graphicsEngine.Device.GraphicsQueue.WaitForValue(flushFence);

                // Sync the current frame resource to this new fence to keep state consistent
                frameResource.FenceValue = flushFence;

                foreach (var resource in _frameResources)
                {
                    resource.CommandAllocator.Reset();
                }

                var keys = _resizeRequest.Keys.ToArray();
                foreach (var swapChain in keys)
                {
                    if (_resizeRequest.TryRemove(swapChain, out var newSize))
                    {
                        swapChain.Resize(newSize.x, newSize.y);
                    }
                }

                frameResource.GpuReadyEvent.Set();

                continue; // Skip rendering this frame since we just resized and may have invalid render targets
            }

            // Begin rendering for this frame
            frameResource.CommandAllocator.Reset();

            _resourceManager.BeginFrame(_cpuFenceValue);
            var r = _graphicsEngine.BeginFrame(_cpuFenceValue);

            if (r.IsFailure)
            {
                StopRenderLoop(r);
                break;
            }

            // Start recording commands

            // TODO: How can we support async compute and async copy?
            var cmd = _graphicsEngine.GetPooledCommandBuffer(CommandBufferType.Graphics);
            cmd.Begin(frameResource.CommandAllocator);

            var renderCtx = new RenderContext
            {
                CommandBuffer = cmd
            };

            ref var renderRequests = ref _renderRequests[_frameIndex];
            _renderPipeline.Render(renderCtx, renderRequests.AsSpan());

            // End recording commands and submit
            r = cmd.End();
            if (r.IsFailure)
            {
                _graphicsEngine.ReturnPooledCommandBuffer(cmd);
                StopRenderLoop(r);
                break;
            }

            _graphicsEngine.Device.GraphicsQueue.Submit(cmd);
            _graphicsEngine.ReturnPooledCommandBuffer(cmd);

            // End the frame and present
            _resourceManager.EndFrame(_cpuFenceValue);
            r = _graphicsEngine.EndFrame(_gpuFenceValue);

            if (r.IsFailure)
            {
                StopRenderLoop(r);
                break;
            }

            // TODO: Present here.


            // Prepare for the next frame.
            for (var i = 0; i < renderRequests.Count; i++)
            {
                renderRequests[i].Dispose();
            }

            renderRequests.Clear();

            _gpuFenceValue++;

            frameResource.GpuReadyEvent.Set();
            frameResource.FenceValue = _graphicsEngine.Device.GraphicsQueue.Signal(_gpuFenceValue);
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
        _frameResources[eventIndex].CpuReadyEvent.Set();
        _cpuFenceValue++;
    }

    internal void RequestSwapChainResize(ISwapChain swapChain, uint2 newSize)
    {
        Debug.Assert(!_disposed, "Cannot request swap chain resize on a disposed RenderSystem.");
        _resizeRequest.AddOrUpdate(swapChain, newSize, (_, _) => newSize);
    }

    public void AddRenderRequest(in RenderRequest request)
    {
        Debug.Assert(!_disposed, "Cannot add render request to a disposed RenderSystem.");

        var frameIndex = (int)(_cpuFenceValue % _config.FrameBufferCount);
        _renderRequests[frameIndex].Add(request);
    }

    public bool WaitForGPUReady(int timeOut = -1)
    {
        Debug.Assert(!_disposed, "Cannot wait for GPU ready on a disposed RenderSystem.");

        var eventIndex = (int)(_cpuFenceValue % _config.FrameBufferCount);
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

        foreach (ref var renderRequestList in _renderRequests)
        {
            foreach (ref var request in renderRequestList)
            {
                request.Dispose();
            }

            renderRequestList.Dispose();
        }

        _graphicsEngine.Dispose();
        _shutdownEvent.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
