using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using Ghost.Graphics.Core;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of the renderer interface using RHI abstractions
/// </summary>
internal unsafe class D3D12Renderer : IRenderer
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
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private Handle<Texture> _customRenderTarget;     // User-provided render target
    private Handle<Texture> _offScreenRenderTarget;  // Off-screen target for swap chain
    private ISwapChain? _swapChain;

    private readonly Lock _lock = new();

    private uint2 _currentSize;
    private uint _pendingWidth;
    private uint _pendingHeight;
    private bool _resizeRequested;
    private bool _disposed;

    public uint2 Size => _currentSize;

    // TODO: Add render passes support
    // private ImmutableArray<IRenderPass> _renderPasses;

    public D3D12Renderer(IGraphicsEngine graphicsEngine, IResourceAllocator resourceAllocator, D3D12ResourceDatabase resourceDatabase)
    {
        _commandQueue = graphicsEngine.Device.GraphicsQueue;
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;

        // Create frame resources for double buffering
        _frameResources = new FrameResource[D3D12PipelineResource.BACK_BUFFER_COUNT];
        for (var i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i] = new FrameResource(graphicsEngine);
        }

        _customRenderTarget = Handle<Texture>.Invalid;
        _offScreenRenderTarget = Handle<Texture>.Invalid;
    }

    ~D3D12Renderer()
    {
        Dispose();
    }

    public void SetRenderTarget(Handle<Texture> renderTarget)
    {
        _customRenderTarget = renderTarget;
        _swapChain = null;

        // Clean up off-screen target when switching to render target mode
        _resourceDatabase.ReleaseResource(_offScreenRenderTarget.AsResource());
        _offScreenRenderTarget = Handle<Texture>.Invalid;
    }

    public void SetSwapChain(ISwapChain? swapChain)
    {
        _swapChain = swapChain;
        _customRenderTarget = Handle<Texture>.Invalid;

        if (_swapChain != null)
        {
            CreateOrUpdateOffScreenRenderTarget(_swapChain.Width, _swapChain.Height);
        }
        else
        {
            _resourceDatabase.ReleaseResource(_offScreenRenderTarget.AsResource());
            _offScreenRenderTarget = Handle<Texture>.Invalid;
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
        _currentSize = new uint2(newWidth, newHeight);

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

        if (_customRenderTarget.IsValid)
        {
            // Render target mode: render directly to custom target
            RenderScene(_customRenderTarget, frame.commandBuffer);
        }
        else if (_swapChain != null && _offScreenRenderTarget.IsValid)
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

        frame.fenceValue = _commandQueue.Signal(_frameIndex);
        _frameIndex++;
    }

    private void RenderScene(Handle<Texture> target, ICommandBuffer cmd)
    {
        var clearColor = new Color128 { r = 1.0f, g = 0.0f, b = 1.0f, a = 1.0f };

        cmd.BeginRenderPass(target, Handle<Texture>.Invalid, clearColor);

        var viewport = new ViewportDesc { width = _currentSize.x, height = _currentSize.y, minDepth = 0, maxDepth = 1 };
        var scissor = new RectDesc { right = _currentSize.x, bottom = _currentSize.y };

        cmd.SetViewport(viewport);
        cmd.SetScissorRect(scissor);

        // TODO: Execute render passes
        // foreach (var pass in _renderPasses)
        // {
        //     pass.Execute(cmd);
        // }

        cmd.EndRenderPass();
    }

    private void BlitToDestination(Handle<Texture> source, Handle<Texture> destination, ICommandBuffer cmd)
    {
        // Handle swap chain back buffer transitions if needed
        if (_swapChain != null)
        {
            // Transition back buffer to render target
            cmd.ResourceBarrier(destination.AsResource(), ResourceState.Present, ResourceState.RenderTarget);
        }

        // For now, we'll do a simple copy operation
        // In a real implementation, you would use a blit shader for post-processing

        // TODO: Implement proper blit operation with shader
        // This is a placeholder - in D3D12, you would typically:
        // 1. Set render target to the destination
        // 2. Use a full-screen quad/triangle with a shader that samples from the source
        // 3. Apply post-processing effects (tone mapping, gamma correction, etc.)

        // For now, just clear the destination (this should be replaced with actual blit)
        var clearColor = new Color128 { r = 0.0f, g = 0.0f, b = 0.0f, a = 1.0f };
        cmd.BeginRenderPass(destination, Handle<Texture>.Invalid, clearColor);
        cmd.EndRenderPass();

        // Handle swap chain back buffer transitions if needed
        if (_swapChain != null)
        {
            // Transition back buffer to present
            cmd.ResourceBarrier(destination.AsResource(), ResourceState.RenderTarget, ResourceState.Present);
        }
    }

    private void CreateOrUpdateOffScreenRenderTarget(uint width, uint height)
    {
        if (_offScreenRenderTarget.IsValid)
        {
            _resourceAllocator.ReleaseResource(_offScreenRenderTarget.AsResource());
        }

        var desc = RenderTargetDesc.Color(width, height, 1, TextureFormat.R8G8B8A8_UNorm);
        _offScreenRenderTarget = _resourceAllocator.CreateRenderTarget(in desc);
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

        _resourceDatabase.ReleaseResource(_customRenderTarget.AsResource());
        _resourceDatabase.ReleaseResource(_offScreenRenderTarget.AsResource());

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
