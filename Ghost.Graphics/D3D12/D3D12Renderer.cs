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

    private readonly IRenderDevice _device;
    private readonly ICommandQueue _commandQueue;
    private readonly FrameResource[] _frameResources;
    private uint _frameIndex;

    private IRenderTarget? _renderTarget;
    private ISwapChain? _swapChain;

    private readonly Lock _lock = new();
    private uint _pendingWidth;
    private uint _pendingHeight;
    private bool _resizeRequested;
    private bool _disposed;

    // TODO: Add render passes support
    // private ImmutableArray<IRenderPass> _renderPasses;

    public D3D12Renderer(IRenderDevice device)
    {
        _device = device;
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
        _renderTarget = renderTarget;
        _swapChain = null; // Clear swap chain when using render target
    }

    public void SetSwapChain(ISwapChain? swapChain)
    {
        _swapChain = swapChain;
        _renderTarget = null; // Clear render target when using swap chain
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

        if (_renderTarget != null)
        {
            RenderToTarget(_renderTarget, frame.CommandBuffer);
        }
        else if (_swapChain != null)
        {
            RenderToSwapChain(_swapChain, frame.CommandBuffer);
        }
        else
        {
            // No render target - skip rendering
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

    private void RenderToTarget(IRenderTarget target, ICommandBuffer cmd)
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

    private void RenderToSwapChain(ISwapChain swapChain, ICommandBuffer cmd)
    {
        var backBuffer = swapChain.GetCurrentBackBuffer();

        // Transition back buffer to render target
        cmd.ResourceBarrier(backBuffer, ResourceState.Present, ResourceState.RenderTarget);

        // Create temporary render target for back buffer
        // TODO: This should be cached/reused
        var renderTarget = CreateBackBufferRenderTarget(backBuffer);

        RenderToTarget(renderTarget, cmd);

        // Transition back buffer to present
        cmd.ResourceBarrier(backBuffer, ResourceState.RenderTarget, ResourceState.Present);

        renderTarget.Dispose();
    }

    private IRenderTarget CreateBackBufferRenderTarget(ITexture backBuffer)
    {
        // TODO: Create render target from back buffer texture
        // This is a simplified implementation
        var desc = RenderTargetDesc.Color(backBuffer.Width, backBuffer.Height, backBuffer.Format);
        return _device.CreateRenderTarget(desc);
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

        _disposed = true;
    }
}
