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
internal class D3D12Renderer : IRenderer
{
    private struct FrameResource : IDisposable
    {
        public ICommandBuffer commandBuffer;
        public ulong fenceValue;

        public FrameResource(D3D12GraphicsEngine graphicsEngine, int index)
        {
            commandBuffer = graphicsEngine.CreateCommandBuffer();
            commandBuffer.Name = $"Frame Command Buffer {index}";
            fenceValue = 0;
        }

        public readonly void Dispose()
        {
            commandBuffer?.Dispose();
        }
    }

    private readonly D3D12GraphicsEngine _graphicsEngine;
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
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;

        // Create frame resources for double buffering
        _frameResources = new FrameResource[D3D12PipelineResource.BACK_BUFFER_COUNT];
        for (var i = 0; i < _frameResources.Length; i++)
        {
            _frameResources[i] = new FrameResource(graphicsEngine, i);
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

        var newSize = swapChain != null ? new uint2(swapChain.Width, swapChain.Height) : _currentSize;
        if (!math.all(newSize == _currentSize))
        {
            RequestResize(newSize);
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

        // Update off-screen render Target size
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
            _graphicsEngine.Device.GraphicsQueue.WaitForValue(frame.fenceValue);
        }

        if (_renderTarget.IsValid)
        {
            frame.commandBuffer.Begin();

            // NOTE: Temperary solution: render directly to the swap chain back buffer if available.
            // HACK: This is hard coded for testing purposes only.

            var rt = _swapChain?.GetCurrentBackBuffer() ?? _renderTarget;

            if(_swapChain != null)
            {
                frame.commandBuffer.ResourceBarrier(rt.AsResource(), ResourceState.Present, ResourceState.RenderTarget);
            }

            RenderScene(rt, frame.commandBuffer);

            if (_swapChain != null)
            {
                frame.commandBuffer.ResourceBarrier(rt.AsResource(), ResourceState.RenderTarget, ResourceState.Present);
            }

            // if (_swapChain != null)
            // {
            //     var backBufferRT = _swapChain.GetCurrentBackBuffer();
            //     BlitToDestination(_renderTarget, backBufferRT, frame.commandBuffer);
            // }

            frame.commandBuffer.End();

            _graphicsEngine.Device.GraphicsQueue.Submit(frame.commandBuffer);
            _swapChain?.Present();

        }

        frame.fenceValue = _graphicsEngine.Device.GraphicsQueue.Signal(_frameIndex);
        _frameIndex++;
    }

    // TODO: A proper render graph integration.
    private void RenderScene(Handle<Texture> target, ICommandBuffer cmd)
    {
        var clearColor = new Color128 { r = 1.0f, g = 0.0f, b = 1.0f, a = 1.0f };

        Span<PassRenderTargetDesc> rtDesc =
        [
            new PassRenderTargetDesc
            {
                Texture = target,
                ClearColor = clearColor,
            },
        ];

        var depthDesc = new PassDepthStencilDesc
        {
            Texture = Handle<Texture>.Invalid,
            ClearDepth = 1.0f,
            ClearStencil = 0,
        };

        // NOTE: Testing only.
        var ctx = new RenderingContext(_graphicsEngine, cmd, _graphicsEngine.CopyCommandBuffer, null!);
        if (_frameIndex == 0)
        {
            _pass.Initialize(ref ctx);
        }

        var viewport = new ViewportDesc { Width = _currentSize.x, Height = _currentSize.y, MinDepth = 0, MaxDepth = 1 };
        var scissor = new RectDesc { Right = _currentSize.x, Bottom = _currentSize.y };

        cmd.BeginRenderPass(rtDesc, depthDesc, false);
        cmd.SetViewport(viewport);
        cmd.SetScissorRect(scissor);

        // NOTE: Testing only.
        _pass.Execute(ref ctx);

        cmd.EndRenderPass();
    }

    private void BlitToSwapChain(Handle<Texture> source, Handle<Texture> destination, ICommandBuffer cmd)
    {
        // Handle swap chain back buffer transitions if needed
        if (_swapChain != null)
        {
            // Transition back buffer to render Target
            cmd.ResourceBarrier(destination.AsResource(), ResourceState.Present, ResourceState.RenderTarget);
        }

        // For now, we'll do a simple copy operation
        // In a real implementation, you would use a blit shader for post-processing

        // FIX: Implement proper blit operation with shader
        // This is a placeholder - in D3D12, you would typically:
        // 1. Set render Target to the destination
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
                _graphicsEngine.Device.GraphicsQueue.WaitForValue(frame.fenceValue);
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

        // If using a swap chain, release the off-screen render Target.
        // Otherwise, the render Target is managed externally.
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
