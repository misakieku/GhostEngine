using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ghost.Graphics;

internal readonly struct RenderEngineDesc
{
    public required IGraphicsEngine GraphicsEngine
    {
        get; init;
    }

    public required uint FrameBufferCount
    {
        get; init;
    }

    public required IRenderPipelineSettings InitialRenderPipelineSettings
    {
        get; init;
    }

    public required IResourceStreamingProcessor ResourceStreamingProcessor
    {
        get; init;
    }

    public required string ShaderCacheDirectory
    {
        get; init;
    }

    public IShaderCompilationBridge? ShaderCompilationBridge
    {
        get; init;
    }
}

/// <summary>
/// Application-level render system that orchestrates multiple renderers
/// and handles frame synchronization
/// </summary>
public class RenderEngine : IDisposable
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

        public required ICommandAllocator GraphicsCommandAllocator
        {
            get; init;
        }

        public required ICommandAllocator ComputeCommandAllocator
        {
            get; init;
        }

        public required ICommandAllocator CopyCommandAllocator
        {
            get; init;
        }

        public FrameCompletionInfo Completion
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
            GraphicsCommandAllocator.Dispose();
            ComputeCommandAllocator.Dispose();
            CopyCommandAllocator.Dispose();
            RenderPayload.Dispose();
        }
    }

    private readonly IResourceStreamingProcessor _streamingProcessor;

    private IRenderPipelineSettings _renderPipelineSettings;
    private IRenderPipeline _renderPipeline;

    private readonly IGraphicsEngine _graphicsEngine;
    private readonly ResourceManager _resourceManager;
    private readonly SwapChainManager _swapChainManager;
    private readonly ShaderLibrary _shaderLibrary;
    private readonly IFrameScheduler _frameScheduler;
    private readonly FrameResource[] _frameResources;
    private readonly Thread _renderThread;
    private readonly AutoResetEvent _shutdownEvent;

    private readonly ConcurrentDictionary<ISwapChain, uint2> _resizeRequest;

    private ulong _submittedFrame;

    private bool _isRunning;
    private bool _disposed;

    internal ShaderLibrary ShaderLibrary => _shaderLibrary;

    public IGraphicsEngine GraphicsEngine => _graphicsEngine;
    public ResourceManager ResourceManager => _resourceManager;
    public SwapChainManager SwapChainManager => _swapChainManager;
    public IFrameScheduler FrameScheduler => _frameScheduler;

    public bool IsRunning => _isRunning;
    public ulong SubmittedFrame => _submittedFrame;
    public int MaxFrameLatency => _frameResources.Length;

    public IRenderPipelineSettings RenderPipelineSettings
    {
        get => _renderPipelineSettings;
        set
        {
            Logger.DebugAssert(value != null, "RenderPipelineSettings cannot be set to null.");
            Logger.DebugAssert(!_disposed, "Cannot set RenderPipelineSettings on a disposed RenderSystem.");

            if (value == _renderPipelineSettings)
            {
                return;
            }

            _renderPipeline?.Dispose();
            for (var i = 0; i < _frameResources.Length; i++)
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

    internal RenderEngine(RenderEngineDesc desc)
    {
        _graphicsEngine = desc.GraphicsEngine;
        _streamingProcessor = desc.ResourceStreamingProcessor;
        _renderPipelineSettings = desc.InitialRenderPipelineSettings;

        _frameScheduler = new FrameScheduler(_graphicsEngine);
        // Create frame resources for synchronization
        _frameResources = new FrameResource[desc.FrameBufferCount];
        for (var i = 0; i < desc.FrameBufferCount; i++)
        {
            _frameResources[i] = new FrameResource
            {
                CpuReadyEvent = new AutoResetEvent(false),
                GpuReadyEvent = new AutoResetEvent(true),
                GraphicsCommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Graphics),
                ComputeCommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Compute),
                CopyCommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Copy),
            };
        }

        _renderPipeline = _renderPipelineSettings.CreatePipeline(this);
        for (var i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i].RenderPayload = _renderPipelineSettings.CreatePayload(this, _renderPipeline);
        }

        _resourceManager = new ResourceManager(_graphicsEngine.Device, _graphicsEngine.ResourceAllocator, _graphicsEngine.ResourceDatabase);
        _swapChainManager = new SwapChainManager(_graphicsEngine);
        _shaderLibrary = new ShaderLibrary(desc.ShaderCompilationBridge, _graphicsEngine.PipelineLibrary, desc.ShaderCacheDirectory);

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

    ~RenderEngine()
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
            Logger.Error($"Render failed: {result.Message}");
        }

        var waitHandles = new WaitHandle[] { null!, _shutdownEvent };

        var renderContext = new RenderContext
        (
            _resourceManager,
            _graphicsEngine.ResourceAllocator,
            _graphicsEngine.ResourceDatabase,
            _graphicsEngine.PipelineLibrary,
            _shaderLibrary
        );

        while (_isRunning)
        {
            var frameIndex = (int)(_submittedFrame % (ulong)_frameResources.Length);
            ref var frameResource = ref _frameResources[frameIndex];

            try
            {
                // Wait for either CPU ready signal or shutdown signal
                waitHandles[0] = frameResource.CpuReadyEvent;
                var waitResult = WaitHandle.WaitAny(waitHandles);

                // If shutdown was signaled, exit the loop
                if (!_isRunning || waitResult == 1)
                {
                    break;
                }

                // Only proceed if CPU ready event was signaled
                if (waitResult != 0)
                {
                    continue;
                }

                _frameScheduler.WaitForFrame(frameResource.Completion);
                var completedFrame = frameResource.Completion.FrameNumber;

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

                if (_submittedFrame < completedFrame)
                {
                    _submittedFrame = completedFrame;
                }

                // Begin rendering for this frame.
                frameResource.GraphicsCommandAllocator.Reset();
                frameResource.ComputeCommandAllocator.Reset();
                frameResource.CopyCommandAllocator.Reset();

                _resourceManager.BeginFrame(_submittedFrame);
                _graphicsEngine.BeginFrame(_submittedFrame);

                var cmd = _graphicsEngine.GetPooledCommandBuffer(CommandBufferType.Graphics);
                var submitted = false;

                try
                {
                    cmd.Begin(frameResource.GraphicsCommandAllocator);

                    var streamingContext = new ResourceStreamingContext
                    {
                        FrameScheduler = _frameScheduler,
                        GraphicsEngine = _graphicsEngine,
                        CopyCommandAllocator = frameResource.CopyCommandAllocator,
                        ResourceManager = _resourceManager,
                        ResourceDatabase = _graphicsEngine.ResourceDatabase,
                        ResourceAllocator = _graphicsEngine.ResourceAllocator,
                        CommandBuffer = cmd,
                    };

                    _streamingProcessor.ProcessPendingUploads(streamingContext);

                    renderContext.CommandBuffer = cmd;

                    _renderPipeline.Render(renderContext, frameIndex, frameResource.RenderPayload);
                    _swapChainManager.TransitionAllToPresent(cmd);

                    var result = cmd.End();
                    if (result.IsFailure)
                    {
                        StopRenderLoop(result);
                        break;
                    }

                    _frameScheduler.Submit(cmd);
                    submitted = true;
                    frameResource.Completion = _frameScheduler.Flush();
                    _submittedFrame = frameResource.Completion.FrameNumber;

                    _swapChainManager.PresentAll();
                }
                finally
                {
                    if (!submitted)
                    {
                        _graphicsEngine.ReturnPooledCommandBuffer(cmd);
                    }
                }

                frameResource.GpuReadyEvent.Set();

                // End the frame and retire resources based on the oldest completed frame slot.
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
        Logger.DebugAssert(!_disposed, "Cannot start a disposed RenderSystem.");

        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _renderThread.Start();
    }

    internal void Stop()
    {
        Logger.DebugAssert(!_disposed, "Cannot stop a disposed RenderSystem.");

        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _shutdownEvent.Set();
        _renderThread.Join();
    }

    internal void SignalCPUReady(int frameIndex)
    {
        Logger.DebugAssert(!_disposed, "Cannot signal CPU ready on a disposed RenderSystem.");

        var eventIndex = frameIndex % _frameResources.Length;
        ref var frameResource = ref _frameResources[eventIndex];

        frameResource.CpuReadyEvent.Set();
    }

    internal void RequestSwapChainResize(ISwapChain swapChain, uint2 newSize)
    {
        Logger.DebugAssert(!_disposed, "Cannot request swap chain resize on a disposed RenderSystem.");
        _resizeRequest.AddOrUpdate(swapChain, newSize, (_, _) => newSize);
    }

    public bool WaitForGPUReady(int frameIndex, int timeOut = -1)
    {
        Logger.DebugAssert(!_disposed, "Cannot wait for GPU ready on a disposed RenderSystem.");

        var eventIndex = frameIndex % _frameResources.Length;
        return _frameResources[eventIndex].GpuReadyEvent.WaitOne(timeOut);
    }

    public void WaitIdle()
    {
        Logger.DebugAssert(!_disposed, "Cannot wait idle on a disposed RenderSystem.");
        _frameScheduler.WaitIdle();
    }

    public IRenderPayload GetCurrentFramePayload(int frameIndex)
    {
        Logger.DebugAssert(!_disposed, "Cannot get current frame payload from a disposed RenderSystem.");

        var eventIndex = frameIndex % _frameResources.Length;
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
        _frameScheduler.Dispose();

        for (var i = 0; i < _frameResources.Length; i++)
        {
            ref var frameResource = ref _frameResources[i];
            frameResource.Dispose();
        }

        _renderPipeline.Dispose();

        _shaderLibrary.Dispose();
        _resourceManager.Dispose();
        _swapChainManager.Dispose();

        _graphicsEngine.Dispose();

        _shutdownEvent.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
