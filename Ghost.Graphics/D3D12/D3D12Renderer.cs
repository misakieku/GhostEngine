using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of the renderer interface using RHI abstractions
/// </summary>
public unsafe class D3D12Renderer : IRenderer
{
    private struct FrameResource : IDisposable
    {
        public ICommandBuffer commandBuffer;
        public ulong fenceValue;

        public FrameResource(IGraphicsEngine graphicsEngine)
        {
            commandBuffer = graphicsEngine.CreateCommandBuffer();
            fenceValue = 0;
        }

        public readonly void Dispose()
        {
            commandBuffer?.Dispose();
        }
    }

    private readonly ICommandQueue _commandQueue;
    private readonly FrameResource[] _frameResources;
    private uint _frameIndex;

    private readonly IResourceAllocator _resourceAllocator;

    private IRenderTarget? _customRenderTarget;     // User-provided render target
    private IRenderTarget? _offScreenRenderTarget;  // Off-screen target for swap chain
    private ISwapChain? _swapChain;

    private readonly Lock _lock = new();
    private uint _pendingWidth;
    private uint _pendingHeight;
    private bool _resizeRequested;
    private bool _disposed;

    // TODO: Add render passes support
    // private ImmutableArray<IRenderPass> _renderPasses;

    public D3D12Renderer(IGraphicsEngine graphicsEngine, IResourceAllocator resourceAllocator)
    {
        _resourceAllocator = resourceAllocator;
        _commandQueue = graphicsEngine.Device.GraphicsQueue;

        // Create frame resources for double buffering
        _frameResources = new FrameResource[D3D12PipelineResource.BACK_BUFFER_COUNT];
        for (var i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i] = new FrameResource(graphicsEngine);
        }
    }

    ~D3D12Renderer()
    {
        Dispose();
    }

    public void SetRenderTarget(IRenderTarget? renderTarget)
    {
        _customRenderTarget = renderTarget;
        _swapChain = null;

        // Clean up off-screen target when switching to render target mode
        _offScreenRenderTarget?.Dispose();
        _offScreenRenderTarget = null;
    }

    public void SetSwapChain(ISwapChain? swapChain)
    {
        _swapChain = swapChain;
        _customRenderTarget = null;

        if (_swapChain != null)
        {
            CreateOrUpdateOffScreenRenderTarget(_swapChain.Width, _swapChain.Height);
        }
        else
        {
            _offScreenRenderTarget?.Dispose();
            _offScreenRenderTarget = null;
        }
    }

    public void RequestResize(uint width, uint height)
    {
        lock (_lock)
        {
            if (_pendingWidth == width && _pendingHeight == height)
                return;

            _resizeRequested = true;
            _pendingWidth = width;
            _pendingHeight = height;
        }
    }

    public void ExecutePendingResize()
    {
        if (!_resizeRequested)
        {
            return;
        }

        uint newWidth, newHeight;
        lock (_lock)
        {
            newWidth = _pendingWidth;
            newHeight = _pendingHeight;
            _resizeRequested = false;
        }

        // Wait for GPU to complete
        WaitIdle();

        // Resize swap chain if present
        _swapChain?.Resize(newWidth, newHeight);

        // Update off-screen render target size
        if (_swapChain != null)
        {
            CreateOrUpdateOffScreenRenderTarget(newWidth, newHeight);
        }
    }

    public void Render()
    {
        ExecutePendingResize();

        var frameIndex = _frameIndex % (uint)_frameResources.Length;
        ref var frame = ref _frameResources[frameIndex];

        if (frame.fenceValue > 0)
        {
            _commandQueue.WaitForValue(frame.fenceValue);
        }

        frame.commandBuffer.Begin();

        if (_customRenderTarget != null)
        {
            // Render target mode: render directly to custom target
            RenderScene(_customRenderTarget, frame.commandBuffer);
        }
        else if (_swapChain != null && _offScreenRenderTarget != null)
        {
            // Swap chain mode: render to off-screen, then blit to back buffer
            var backBufferRT = _swapChain.GetCurrentBackBuffer();

            // For testing, we render directly to the back buffer
            RenderScene(backBufferRT, frame.commandBuffer);
            //BlitToDestination(_offScreenRenderTarget, backBufferRT, frame.CommandBuffer);
        }

        frame.commandBuffer.End();

        _commandQueue.Submit(frame.commandBuffer);
        _swapChain?.Present();

        frame.fenceValue = _commandQueue.Signal(++_frameIndex);
    }

    private void RenderScene(IRenderTarget target, ICommandBuffer cmd)
    {
        var clearColor = new Color16 { r = 1.0f, g = 0.0f, b = 1.0f, a = 1.0f };

        cmd.BeginRenderPass(target, clearColor);

        var viewport = new ViewportDesc(target.Width, target.Height);
        var scissor = new RectDesc(0, 0, (int)target.Width, (int)target.Height);

        cmd.SetViewport(viewport);
        cmd.SetScissorRect(scissor);

        // TODO: Execute render passes
        // foreach (var pass in _renderPasses)
        // {
        //     pass.Execute(cmd);
        // }

        cmd.EndRenderPass();
    }

    private void BlitToDestination(IRenderTarget source, IRenderTarget destination, ICommandBuffer cmd)
    {
        // Handle swap chain back buffer transitions if needed
        if (_swapChain != null)
        {
            // Transition back buffer to render target
            cmd.ResourceBarrier(destination, ResourceState.Present, ResourceState.RenderTarget);
        }

        // For now, we'll do a simple copy operation
        // In a real implementation, you would use a blit shader for post-processing

        // TODO: Implement proper blit operation with shader
        // This is a placeholder - in D3D12, you would typically:
        // 1. Set render target to the destination
        // 2. Use a full-screen quad/triangle with a shader that samples from the source
        // 3. Apply post-processing effects (tone mapping, gamma correction, etc.)

        // For now, just clear the destination (this should be replaced with actual blit)
        var clearColor = new Color16 { r = 0.0f, g = 0.0f, b = 0.0f, a = 1.0f };
        cmd.BeginRenderPass(destination, clearColor);
        cmd.EndRenderPass();

        // Handle swap chain back buffer transitions if needed
        if (_swapChain != null)
        {
            // Transition back buffer to present
            cmd.ResourceBarrier(destination, ResourceState.RenderTarget, ResourceState.Present);
        }
    }

    private void CreateOrUpdateOffScreenRenderTarget(uint width, uint height)
    {
        // Check if we need to recreate the off-screen render target
        if (_offScreenRenderTarget == null ||
            _offScreenRenderTarget.Width != width ||
            _offScreenRenderTarget.Height != height)
        {
            _offScreenRenderTarget?.Dispose();

            var desc = RenderTargetDesc.Color(width, height, 1, TextureFormat.R8G8B8A8_UNorm);
            _offScreenRenderTarget = _resourceAllocator.CreateRenderTarget(in desc);
        }
    }

    public void WaitIdle()
    {
        // Wait for all frame resources to complete
        foreach (ref var frame in _frameResources.AsSpan())
        {
            if (frame.fenceValue > 0)
            {
                _commandQueue.WaitForValue(frame.fenceValue);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        WaitIdle();

        foreach (ref var frame in _frameResources.AsSpan())
        {
            frame.Dispose();
        }

        _offScreenRenderTarget?.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
