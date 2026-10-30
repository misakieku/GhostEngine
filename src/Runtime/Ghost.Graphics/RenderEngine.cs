using Ghost.Core;
using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Collections.Concurrent;
using System.Collections.Immutable;

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

    public string? ShaderCacheDirectory
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
public sealed class RenderEngine : IDisposable
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

    private IRenderPipeline? _renderPipeline;

    private readonly IGraphicsEngine _graphicsEngine;

    private readonly ResourceManager _resourceManager;
    private readonly SwapChainManager _swapChainManager;
    private readonly ShaderLibrary _shaderLibrary;
    private readonly FrameScheduler _frameScheduler;
    private readonly FrameResource[] _frameResources;
    private readonly Thread _renderThread;
    private readonly CancellationTokenSource _shutdownCts;

    private readonly ConcurrentDictionary<ISwapChain, uint2> _resizeRequest;

    private ImmutableArray<IRenderLoopCommand> _preRenderLoopCommands;
    private ImmutableArray<IRenderLoopCommand> _postRenderLoopCommands;

    private ulong _submittedFrame;
    private bool _isRunning;

    internal ShaderLibrary ShaderLibrary => _shaderLibrary;

    public IGraphicsEngine GraphicsEngine => _graphicsEngine;
    public ResourceManager ResourceManager => _resourceManager;
    public SwapChainManager SwapChainManager => _swapChainManager;
    public FrameScheduler FrameScheduler => _frameScheduler;

    public bool IsRunning => _isRunning;
    public ulong SubmittedFrame => _submittedFrame;
    public int MaxFrameLatency => _frameResources.Length;

    public IRenderPipeline? RenderPipeline => _renderPipeline;

    internal RenderEngine(RenderEngineDesc desc)
    {
        _graphicsEngine = desc.GraphicsEngine;

        _resourceManager = new ResourceManager(_graphicsEngine.Device, _graphicsEngine.ResourceAllocator, _graphicsEngine.ResourceDatabase);
        _swapChainManager = new SwapChainManager(_graphicsEngine, desc.FrameBufferCount);
        _frameScheduler = new FrameScheduler(_graphicsEngine);
        _shaderLibrary = new ShaderLibrary(desc.ShaderCompilationBridge, _graphicsEngine.PipelineLibrary, desc.ShaderCacheDirectory);

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

        _renderThread = new Thread(RenderLoop)
        {
            IsBackground = true,
            Name = "Graphics Render Thread",
            Priority = ThreadPriority.Normal
        };

        _shutdownCts = new CancellationTokenSource();
        _resizeRequest = new ConcurrentDictionary<ISwapChain, uint2>();
        _preRenderLoopCommands = ImmutableArray<IRenderLoopCommand>.Empty;
        _postRenderLoopCommands = ImmutableArray<IRenderLoopCommand>.Empty;

        _isRunning = false;
    }

    private void StopRenderLoop(Result result)
    {
        Logger.Fatal($"Render failed: {result.Message}");

        _isRunning = false;
        _shutdownCts.Cancel();
    }

    private void RenderLoop()
    {
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

                ResizePendingSwapChain();

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

                var renderContext = new RenderContext
                {
                    FrameScheduler = _frameScheduler,
                    CopyCommandAllocator = frameResource.CopyCommandAllocator,
                    GraphicsCommandAllocator = frameResource.GraphicsCommandAllocator,
                    ResourceManager = _resourceManager,
                    ResourceDatabase = _graphicsEngine.ResourceDatabase,
                    ResourceAllocator = _graphicsEngine.ResourceAllocator,
                    PipelineLibrary = _graphicsEngine.PipelineLibrary,
                    FrameBufferCount = _frameResources.Length,
                    ShaderLibrary = _shaderLibrary,
                    FrameIndex = frameIndex,
                };

                var result = ExecutePrelude(frameIndex, frameResource, renderContext);
                if (result.IsFailure)
                {
                    StopRenderLoop(result);
                    break;
                }

                result = ExecuteRenderPipeline(frameIndex, frameResource, renderContext);
                if (result.IsFailure)
                {
                    StopRenderLoop(result);
                    break;
                }

                result = ExecutePostlude(frameResource, renderContext);
                if (result.IsFailure)
                {
                    StopRenderLoop(result);
                    break;
                }

                frameResource.Completion = _frameScheduler.Flush();
                _submittedFrame = frameResource.Completion.FrameNumber;
                _swapChainManager.PresentAll();

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
            finally
            {
                RenderThreadTempAllocatorManager.Reset();
            }
        }
    }

    private void ResizePendingSwapChain()
    {
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
    }

    private Result ExecutePrelude(int frameIndex, FrameResource frameResource, RenderContext renderContext)
    {
        if (_preRenderLoopCommands.IsEmpty && _renderPipeline == null)
        {
            return Result.Success();
        }

        var preludeCmd = _frameScheduler.GetPooledCommandBuffer(CommandBufferType.Graphics);
        preludeCmd.Begin(frameResource.GraphicsCommandAllocator);

        if (!_preRenderLoopCommands.IsEmpty)
        {
            foreach (var command in _preRenderLoopCommands)
            {
                command.Execute(renderContext, preludeCmd);
            }
        }

        if (_renderPipeline != null && frameResource.RenderPayload != null)
        {
            _renderPipeline.RecordPrelude(renderContext, preludeCmd, frameIndex, frameResource.RenderPayload);
        }

        var result = preludeCmd.End();
        if (result.IsFailure)
        {
            return result;
        }

        _frameScheduler.Submit(preludeCmd);
        return Result.Success();
    }

    private Result ExecuteRenderPipeline(int frameIndex, FrameResource frameResource, RenderContext renderContext)
    {
        if (_renderPipeline != null && frameResource.RenderPayload != null)
        {
            var executionContext = new RenderGraphExecutionContext(
                _graphicsEngine,
                _frameScheduler,
                frameResource.GraphicsCommandAllocator,
                frameResource.ComputeCommandAllocator);

            var result = _renderPipeline.ExecuteGraph(renderContext.ResourceContext, frameIndex, frameResource.RenderPayload, executionContext);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Result.Success();
    }

    private Result ExecutePostlude(FrameResource frameResource, RenderContext renderContext)
    {
        if (!_postRenderLoopCommands.IsEmpty)
        {
            var postRenderCmd = _frameScheduler.GetPooledCommandBuffer(CommandBufferType.Graphics);
            postRenderCmd.Begin(frameResource.GraphicsCommandAllocator);
            foreach (var command in _postRenderLoopCommands)
            {
                command.Execute(renderContext, postRenderCmd);
            }

            var result = postRenderCmd.End();
            if (result.IsFailure)
            {
                return result;
            }

            _frameScheduler.Submit(postRenderCmd);
        }

        return Result.Success();
    }

    internal void Start()
    {
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
        var eventIndex = frameIndex % _frameResources.Length;
        ref var frameResource = ref _frameResources[eventIndex];

        frameResource.RenderPayload?.EndRecord();
        frameResource.CpuReadyEvent.Set();
    }

    public void AddPreRenderLoopCommand(params ReadOnlySpan<IRenderLoopCommand> commands)
    {
        _preRenderLoopCommands = _preRenderLoopCommands.AddRange(commands);
    }

    public void AddPreRenderLoopCommand(params IEnumerable<IRenderLoopCommand> commands)
    {
        _preRenderLoopCommands = _preRenderLoopCommands.AddRange(commands);
    }

    public void AddPostRenderLoopCommand(params ReadOnlySpan<IRenderLoopCommand> commands)
    {
        _postRenderLoopCommands = _postRenderLoopCommands.AddRange(commands);
    }

    public void AddPostRenderLoopCommand(params IEnumerable<IRenderLoopCommand> commands)
    {
        _postRenderLoopCommands = _postRenderLoopCommands.AddRange(commands);
    }

    public void SetRenderPipeline(IRenderPipeline renderPipeline)
    {
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
        _resizeRequest.AddOrUpdate(swapChain, newSize, (_, _) => newSize);
    }

    public bool WaitForGPUReady(int frameIndex, int timeOut = -1)
    {
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
        _frameScheduler.WaitIdle();
    }

    public IRenderPayload GetCurrentFramePayload(int frameIndex)
    {
        var eventIndex = frameIndex % _frameResources.Length;
        ref var frameResource = ref _frameResources[eventIndex];

        return frameResource.RenderPayload!;
    }

    public void Dispose()
    {
        Stop();

        if (!_preRenderLoopCommands.IsEmpty)
        {
            foreach (var command in _preRenderLoopCommands)
            {
                command.Dispose();
            }
        }

        if (!_postRenderLoopCommands.IsEmpty)
        {
            foreach (var command in _postRenderLoopCommands)
            {
                command.Dispose();
            }
        }

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

        RenderThreadTempAllocatorManager.Dispose();
    }
}
