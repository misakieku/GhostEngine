using Ghost.Graphics.RHI;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.Data;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of the renderer interface using RHI abstractions
/// </summary>
public unsafe class D3D12Renderer : IRenderer
{
    private struct FrameResource : IDisposable
    {
        public ICommandBuffer CommandBuffer;
        public ulong FenceValue;

        public FrameResource(IRenderDevice device)
        {
            CommandBuffer = device.CreateCommandBuffer();
            FenceValue = 0;
        }

        public void Dispose()
        {
            CommandBuffer?.Dispose();
        }
    }

    private readonly ICommandQueue _commandQueue;
    private readonly FrameResource[] _frameResources;
    private uint _frameIndex;

    private IRenderTarget? _destinationTarget; // Final destination (custom render target or swap chain back buffer)
    private ISwapChain? _swapChain;
    private IRenderTarget? _offScreenRenderTarget; // Always render to off-screen first

    private readonly Lock _lock = new();
    private uint _pendingWidth;
    private uint _pendingHeight;
    private bool _resizeRequested;
    private bool _disposed;

    // TODO: Add render passes support
    // private ImmutableArray<IRenderPass> _renderPasses;

    public D3D12Renderer(IRenderDevice device)
    {
        _commandQueue = device.GraphicsQueue;

        // Create frame resources for double buffering
        _frameResources = new FrameResource[2];
        for (int i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i] = new FrameResource(device);
        }
    }

    public void SetRenderTarget(IRenderTarget? renderTarget)
    {
        _destinationTarget = renderTarget;
        _swapChain = null; // Clear swap chain when using custom render target

        // Create or update off-screen render target to match destination size
        if (_destinationTarget != null)
        {
            CreateOrUpdateOffScreenRenderTarget(_destinationTarget.Width, _destinationTarget.Height);
        }
        else
        {
            _offScreenRenderTarget?.Dispose();
            _offScreenRenderTarget = null;
        }
    }

    public void SetSwapChain(ISwapChain? swapChain)
    {
        _swapChain = swapChain;
        _destinationTarget = null; // Clear custom render target when using swap chain

        // Create or update off-screen render target to match swap chain size
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
            return;

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

        // Get current frame resource
        var frameIndex = _frameIndex % (uint)_frameResources.Length;
        ref var frame = ref _frameResources[frameIndex];

        // Wait for this frame resource to be available
        if (frame.FenceValue > 0)
        {
            _commandQueue.WaitForValue(frame.FenceValue);
        }

        // Begin command recording
        frame.CommandBuffer.Begin();

        // Determine the final destination target
        IRenderTarget? finalDestination = null;
        ITexture? swapChainBackBuffer = null;

        if (_destinationTarget != null)
        {
            // Rendering to custom render target
            finalDestination = _destinationTarget;
        }
        else if (_swapChain != null)
        {
            // Rendering to swap chain - get back buffer as render target
            finalDestination = _swapChain.GetCurrentBackBufferRenderTarget();
            swapChainBackBuffer = _swapChain.GetCurrentBackBuffer();
        }

        if (finalDestination != null && _offScreenRenderTarget != null)
        {
            // Always render to off-screen first, then blit to final destination
            RenderScene(_offScreenRenderTarget, frame.CommandBuffer);
            BlitToDestination(_offScreenRenderTarget, finalDestination, swapChainBackBuffer, frame.CommandBuffer);
        }
        else
        {
            // No destination - skip rendering
            frame.CommandBuffer.End();
            return;
        }

        // End command recording
        frame.CommandBuffer.End();

        // Submit commands
        _commandQueue.Submit(frame.CommandBuffer);

        // Present if using swap chain
        _swapChain?.Present();

        // Signal fence for this frame
        frame.FenceValue = _commandQueue.Signal(++_frameIndex);
    }

    private void RenderScene(IRenderTarget target, ICommandBuffer cmd)
    {
        var clearColor = new Color128 { r = 1.0f, g = 0.0f, b = 1.0f, a = 1.0f };

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

    private void BlitToDestination(IRenderTarget source, IRenderTarget destination, ITexture? swapChainBackBuffer, ICommandBuffer cmd)
    {
        // Handle swap chain back buffer transitions if needed
        if (swapChainBackBuffer != null)
        {
            // Transition back buffer to render target
            cmd.ResourceBarrier(swapChainBackBuffer, ResourceState.Present, ResourceState.RenderTarget);
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
        cmd.BeginRenderPass(destination, clearColor);
        cmd.EndRenderPass();

        // Handle swap chain back buffer transitions if needed
        if (swapChainBackBuffer != null)
        {
            // Transition back buffer to present
            cmd.ResourceBarrier(swapChainBackBuffer, ResourceState.RenderTarget, ResourceState.Present);
        }
    }

    private void CreateOrUpdateOffScreenRenderTarget(uint width, uint height)
    {
        // Check if we need to recreate the off-screen render target
        if (_offScreenRenderTarget == null || _offScreenRenderTarget.Width != width || _offScreenRenderTarget.Height != height)
        {
            _offScreenRenderTarget?.Dispose();

            var desc = RenderTargetDesc.Color(width, height, TextureFormat.B8G8R8A8_UNorm);
            _offScreenRenderTarget = _device.CreateRenderTarget(desc);
        }
    }

    public void WaitIdle()
    {
        // Wait for all frame resources to complete
        foreach (ref var frame in _frameResources.AsSpan())
        {
            if (frame.FenceValue > 0)
            {
                _commandQueue.WaitForValue(frame.FenceValue);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        WaitIdle();

        foreach (ref var frame in _frameResources.AsSpan())
        {
            frame.Dispose();
        }

        _offScreenRenderTarget?.Dispose();

        _disposed = true;
    }
}
