using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderPasses;

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

        public FrameResource(D3D12GraphicsEngine graphicsEngine)
        {
            commandBuffer = graphicsEngine.CreateCommandBuffer();
            fenceValue = 0;
        }

        public readonly void Dispose()
        {
            commandBuffer?.Dispose();
        }
    }

    private readonly D3D12GraphicsEngine _graphicsEngine;
    private readonly D3D12CommandQueue _commandQueue;
    private readonly FrameResource[] _frameResources;
    private uint _frameIndex;

    private readonly D3D12ResourceAllocator _resourceAllocator;
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private Handle<Texture> _renderTarget;
    private ISwapChain? _swapChain;

    private readonly Lock _lock = new();

    private uint2 _currentSize;
    private uint2 _pendingSize;
    private bool _resizeRequested;
    private bool _disposed;


    // NOTE: Testing only.
    private readonly MeshRenderPass _pass;

    public uint2 Size => _currentSize;

    // TODO: Add render passes support
    // private ImmutableArray<IRenderPass> _renderPasses;

    public D3D12Renderer(D3D12GraphicsEngine graphicsEngine, D3D12ResourceAllocator resourceAllocator, D3D12ResourceDatabase resourceDatabase)
    {
        _graphicsEngine = graphicsEngine;
        _commandQueue = (D3D12CommandQueue)graphicsEngine.Device.GraphicsQueue;
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;

        // Create frame resources for double buffering
        _frameResources = new FrameResource[D3D12PipelineResource.BACK_BUFFER_COUNT];
        for (var i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i] = new FrameResource(graphicsEngine);
        }

        _renderTarget = Handle<Texture>.Invalid;


        // NOTE: Testing only.
        _pass = new();
    }

    ~D3D12Renderer()
    {
        Dispose();
    }

    private void CreateOffScreenRenderTarget(uint width, uint height)
    {
        var desc = RenderTargetDesc.Color(width, height, 1, TextureFormat.R8G8B8A8_UNorm);
        _renderTarget = _resourceAllocator.CreateRenderTarget(in desc);
    }

    public void SetRenderTarget(Handle<Texture> renderTarget)
    {
        _swapChain = null;

        _resourceDatabase.ReleaseResource(_renderTarget.AsResource());
        _renderTarget = renderTarget;
    }

    public void SetSwapChain(ISwapChain? swapChain)
    {
        if (_swapChain != null)
        {
            _resourceDatabase.ReleaseResource(_renderTarget.AsResource());
        }

        if (swapChain != null)
        {
            CreateOffScreenRenderTarget(swapChain.Width, swapChain.Height);
        }

        _swapChain = swapChain;
    }

    public void RequestResize(uint2 newSize )
    {
        lock (_lock)
        {
            if (math.all(_pendingSize == newSize))
            {
                return;
            }

            _resizeRequested = true;
            _pendingSize = newSize;
        }
    }

    public void ExecutePendingResize()
    {
        if (!_resizeRequested)
        {
            return;
        }

        uint2 newSize;
        lock (_lock)
        {
            newSize = _pendingSize;
            _resizeRequested = false;
        }

        // Wait for GPU to complete
        WaitIdle();

        // Resize swap chain if present
        _swapChain?.Resize(newSize.x, newSize.y);
        _currentSize = newSize;

        // Update off-screen render target size
        if (_swapChain != null)
        {
            _resourceDatabase.ReleaseResource(_renderTarget.AsResource());
            CreateOffScreenRenderTarget(newSize.x, newSize.y);
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

        if (_renderTarget.IsValid)
        {
            frame.commandBuffer.Begin();

            // NOTE: Temperary solution: render directly to the swap chain back buffer if available.
            var rt = _swapChain?.GetCurrentBackBuffer() ?? _renderTarget;
            RenderScene(rt, frame.commandBuffer);

            // if (_swapChain != null)
            // {
            //     var backBufferRT = _swapChain.GetCurrentBackBuffer();
            //     BlitToDestination(_renderTarget, backBufferRT, frame.commandBuffer);
            // }

            frame.commandBuffer.End();

            _commandQueue.Submit(frame.commandBuffer);
            _swapChain?.Present();

        }

        frame.fenceValue = _commandQueue.Signal(_frameIndex);
        _frameIndex++;
    }

    // TODO: A proper render graph integration.
    private void RenderScene(Handle<Texture> target, ICommandBuffer cmd)
    {
        var clearColor = new Color128 { r = 1.0f, g = 0.0f, b = 1.0f, a = 1.0f };

        Span<PassRenderTargetDesc> rtDesc = stackalloc PassRenderTargetDesc[1];
        rtDesc[0] = new PassRenderTargetDesc
        {
            texture = target,
            clearColor = clearColor,
        };

        var depthDesc = new PassDepthStencilDesc
        {
            texture = Handle<Texture>.Invalid,
            clearDepth = 1.0f,
            clearStencil = 0,
        };

        cmd.BeginRenderPass(rtDesc, depthDesc, false);

        var viewport = new ViewportDesc { width = _currentSize.x, height = _currentSize.y, minDepth = 0, maxDepth = 1 };
        var scissor = new RectDesc { right = _currentSize.x, bottom = _currentSize.y };

        cmd.SetViewport(viewport);
        cmd.SetScissorRect(scissor);

        // NOTE: Testing only.
        var ctx = new RenderingContext(_graphicsEngine, cmd, _graphicsEngine.CopyCommandBuffer, null!);
        if (_frameIndex == 0)
        {
            _pass.Initialize(ref ctx);
        }

        _pass.Execute(ref ctx);

        cmd.EndRenderPass();
    }

    private void BlitToSwapChain(Handle<Texture> source, Handle<Texture> destination, ICommandBuffer cmd)
    {
        // Handle swap chain back buffer transitions if needed
        if (_swapChain != null)
        {
            // Transition back buffer to render target
            cmd.ResourceBarrier(destination.AsResource(), ResourceState.Present, ResourceState.RenderTarget);
        }

        // For now, we'll do a simple copy operation
        // In a real implementation, you would use a blit shader for post-processing

        // FIX: Implement proper blit operation with shader
        // This is a placeholder - in D3D12, you would typically:
        // 1. Set render target to the destination
        // 2. Use a full-screen quad/triangle with a shader that samples from the source

        // Handle swap chain back buffer transitions if needed
        if (_swapChain != null)
        {
            // Transition back buffer to present
            cmd.ResourceBarrier(destination.AsResource(), ResourceState.RenderTarget, ResourceState.Present);
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

        // NOTE: Testing only.
        _pass.Cleanup(_resourceDatabase);

        // If using a swap chain, release the off-screen render target.
        // Otherwise, the render target is managed externally.
        if (_swapChain != null)
        {
            _resourceDatabase.ReleaseResource(_renderTarget.AsResource());
        }

        foreach (ref var frame in _frameResources.AsSpan())
        {
            frame.Dispose();
        }

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
