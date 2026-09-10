using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;
using System.Collections.Concurrent;

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
        public required ManualResetEventSlim CpuReadyEvent
        {
            get; init;
        }

        public required ManualResetEventSlim GpuReadyEvent
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

        public IRenderPayload? RenderPayload
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
            RenderPayload?.Dispose();
        }
    }

    private readonly IResourceStreamingProcessor _streamingProcessor;

    private IRenderPipeline? _renderPipeline;

    private readonly IGraphicsEngine _graphicsEngine;
    private readonly ResourceManager _resourceManager;
    private readonly SwapChainManager _swapChainManager;
    private readonly ShaderLibrary _shaderLibrary;
    private readonly IFrameScheduler _frameScheduler;
    private readonly FrameResource[] _frameResources;
    private readonly Thread _renderThread;
    private readonly CancellationTokenSource _shutdownCts;

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

    public IRenderPipeline? RenderPipeline => _renderPipeline;

    internal RenderEngine(RenderEngineDesc desc)
    {
        _graphicsEngine = desc.GraphicsEngine;
        _streamingProcessor = desc.ResourceStreamingProcessor;

        _resourceManager = new ResourceManager(_graphicsEngine.Device, _graphicsEngine.ResourceAllocator, _graphicsEngine.ResourceDatabase);
        _swapChainManager = new SwapChainManager(_graphicsEngine);
        _frameScheduler = new FrameScheduler(_graphicsEngine);

        // Create frame resources for synchronization
        _frameResources = new FrameResource[desc.FrameBufferCount];
        for (var i = 0; i < desc.FrameBufferCount; i++)
        {
            _frameResources[i] = new FrameResource
            {
                CpuReadyEvent = new ManualResetEventSlim(false, 64),
                GpuReadyEvent = new ManualResetEventSlim(true, 64),
                GraphicsCommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Graphics),
                ComputeCommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Compute),
                CopyCommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Copy),
            };
        }

        _shaderLibrary = new ShaderLibrary(desc.ShaderCompilationBridge, _graphicsEngine.PipelineLibrary, desc.ShaderCacheDirectory);

        _renderThread = new Thread(RenderLoop)
        {
            IsBackground = true,
            Name = "Graphics Render Thread",
            Priority = ThreadPriority.Normal
        };

        _shutdownCts = new CancellationTokenSource();
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
            _shutdownCts.Cancel();

            Logger.Error($"Render failed: {result.Message}");
        }

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
                // Wait for CPU ready signal in user space before sleeping
                try
                {
                    frameResource.CpuReadyEvent.Wait(_shutdownCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (!_isRunning)
                {
                    break;
                }

                frameResource.CpuReadyEvent.Reset();

                // Pacing: wait for DXGI frame latency waitable object
                _swapChainManager.WaitForAllFrameLatency();

                // Wait for GPU fence completion of the oldest in-flight frame slot
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
                _shaderLibrary.BeginFrame(_submittedFrame);

                var preludeCmd = _frameScheduler.GetPooledCommandBuffer(CommandBufferType.Graphics);
                var preludeSubmitted = false;

                try
                {
                    // --- Prelude: streaming uploads and pre-graph rendering commands ---
                    preludeCmd.Begin(frameResource.GraphicsCommandAllocator);

                    var streamingContext = new ResourceStreamingContext
                    {
                        FrameScheduler = _frameScheduler,
                        GraphicsEngine = _graphicsEngine,
                        CopyCommandAllocator = frameResource.CopyCommandAllocator,
                        ResourceManager = _resourceManager,
                        ResourceDatabase = _graphicsEngine.ResourceDatabase,
                        ResourceAllocator = _graphicsEngine.ResourceAllocator,
                        CommandBuffer = preludeCmd,
                        ShaderLibrary = _shaderLibrary,
                    };

                    _streamingProcessor.ProcessPendingUploads(streamingContext);
                    _streamingProcessor.ProcessPendingShaderCommits(streamingContext);

                    renderContext.CommandBuffer = preludeCmd;
                    if (_renderPipeline != null && frameResource.RenderPayload != null)
                    {
                        _renderPipeline.RecordPrelude(renderContext, frameIndex, frameResource.RenderPayload);
                    }

                    var result = preludeCmd.End();
                    if (result.IsFailure)
                    {
                        StopRenderLoop(result);
                        break;
                    }

                    if (preludeCmd.State.CommandCount > 0)
                    {
                        _frameScheduler.Submit(preludeCmd);
                        preludeSubmitted = true;
                    }

                    // --- Graph: compile and execute the render graph ---
                    if (_renderPipeline != null && frameResource.RenderPayload != null)
                    {
                        var executionContext = new RenderGraphExecutionContext(
                            _graphicsEngine,
                            _frameScheduler,
                            frameResource.GraphicsCommandAllocator,
                            frameResource.ComputeCommandAllocator);

                        var graphExecution = _renderPipeline.ExecuteGraph(
                            renderContext, frameIndex, frameResource.RenderPayload, executionContext);
                    }

                    frameResource.Completion = _frameScheduler.Flush();
                    _submittedFrame = frameResource.Completion.FrameNumber;
                    _swapChainManager.PresentAll();
                }
                finally
                {
                    if (!preludeSubmitted)
                    {
                        _frameScheduler.ReturnPooledCommandBuffer(preludeCmd);
                    }
                }


                // End the frame and retire resources based on the oldest completed frame slot.
                _resourceManager.EndFrame(completedFrame);
                _graphicsEngine.EndFrame(completedFrame);
                _shaderLibrary.EndFrame(completedFrame);

                frameResource.RenderPayload?.Reset();
                frameResource.GpuReadyEvent.Set();
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
        Logger.DebugAssert(_renderPipeline != null, "Cannot start RenderEngine without a RenderPipeline.");

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
        _shutdownCts.Cancel();
        for (var i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i].CpuReadyEvent.Set();
            _frameResources[i].GpuReadyEvent.Set();
        }
        _renderThread.Join();
        _frameScheduler.WaitIdle();
    }

    internal void SignalCPUReady(int frameIndex)
    {
        Logger.DebugAssert(!_disposed, "Cannot signal CPU ready on a disposed RenderSystem.");

        var eventIndex = frameIndex % _frameResources.Length;
        ref var frameResource = ref _frameResources[eventIndex];

        frameResource.CpuReadyEvent.Set();
    }


    public void SetRenderPipeline(IRenderPipeline renderPipeline)
    {
        ArgumentNullException.ThrowIfNull(renderPipeline);

        if (_isRunning)
        {
            WaitIdle();
        }

        _renderPipeline?.Dispose();
        _renderPipeline = renderPipeline;

        for (var i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i].RenderPayload?.Dispose();
            _frameResources[i].RenderPayload = renderPipeline.CreatePayload();
        }
    }

    public void SetRenderPipeline(IRenderPipeline renderPipeline, Func<RenderEngine, IRenderPipeline, IRenderPayload> payloadFactory)
    {
        ArgumentNullException.ThrowIfNull(renderPipeline);
        ArgumentNullException.ThrowIfNull(payloadFactory);

        if (_isRunning)
        {
            WaitIdle();
        }

        _renderPipeline?.Dispose();
        _renderPipeline = renderPipeline;

        for (var i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i].RenderPayload?.Dispose();
            _frameResources[i].RenderPayload = payloadFactory(this, renderPipeline);
        }
    }

    public void SetRenderPipeline(IRenderPipeline renderPipeline, IRenderPayload[] payloads)
    {
        ArgumentNullException.ThrowIfNull(renderPipeline);
        ArgumentNullException.ThrowIfNull(payloads);

        if (payloads.Length != _frameResources.Length)
        {
            throw new ArgumentException($"Payload count ({payloads.Length}) must match frame buffer count ({_frameResources.Length}).");
        }

        if (_isRunning)
        {
            WaitIdle();
        }

        _renderPipeline?.Dispose();
        _renderPipeline = renderPipeline;

        for (var i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i].RenderPayload?.Dispose();
            _frameResources[i].RenderPayload = payloads[i];
        }
    }

    /// <summary>
    /// Unloads the active render pipeline and releases per-frame payloads.
    /// This should be called before shutting down the AssetManager so all pipeline-held
    /// assets are released before asset registries and ResourceManager are disposed.
    /// </summary>
    public void ReleasePipeline()
    {
        if (_isRunning)
        {
            Stop();
        }

        _renderPipeline?.Dispose();
        _renderPipeline = null;

        for (var i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i].RenderPayload?.Dispose();
            _frameResources[i].RenderPayload = null;
        }
    }

    public void RequestSwapChainResize(ISwapChain swapChain, uint2 newSize)
    {
        Logger.DebugAssert(!_disposed, "Cannot request swap chain resize on a disposed RenderSystem.");
        _resizeRequest.AddOrUpdate(swapChain, newSize, (_, _) => newSize);
    }

    public bool WaitForGPUReady(int frameIndex, int timeOut = -1)
    {
        Logger.DebugAssert(!_disposed, "Cannot wait for GPU ready on a disposed RenderSystem.");

        var eventIndex = frameIndex % _frameResources.Length;
        ref var frameResource = ref _frameResources[eventIndex];
        var success = frameResource.GpuReadyEvent.Wait(timeOut);
        if (success)
        {
            frameResource.GpuReadyEvent.Reset();
        }
        return success;
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

        return frameResource.RenderPayload!;
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

        _renderPipeline?.Dispose();
        _renderPipeline = null;

        _shaderLibrary.Dispose();
        _resourceManager.Dispose();
        _swapChainManager.Dispose();

        _graphicsEngine.Dispose();

        _shutdownCts.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
