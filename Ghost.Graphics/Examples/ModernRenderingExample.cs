using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.D3D12;

namespace Ghost.Graphics.Examples;

/// <summary>
/// Example showing how to use the new RHI architecture
/// </summary>
public class ModernRenderingExample
{
    private IRenderDevice _device;
    private RenderSystem _renderSystem;
    private IRenderer _forwardRenderer;
    private ISwapChain _swapChain;

    public void Initialize()
    {
        // Create modern RHI device
        _device = new D3D12RenderDevice();

        // Create render system for frame management
        _renderSystem = new RenderSystem(_device);

        // Create a renderer using RHI abstractions
        _forwardRenderer = new D3D12Renderer(_device);

        // Create swap chain for presentation
        var swapChainDesc = new SwapChainDesc
        {
            Width = 1920,
            Height = 1080,
            BufferCount = 2,
            Format = TextureFormat.R8G8B8A8_UNorm,
            // Presenter would be set based on your window system
        };
        _swapChain = _device.CreateSwapChain(swapChainDesc);

        // Configure renderer
        _forwardRenderer.SetSwapChain(_swapChain);

        // Register renderer with render system
        _renderSystem.AddRenderer(_forwardRenderer);

        // Start rendering loop
        _renderSystem.Start();
    }

    public void RenderOffscreen()
    {
        // Example of rendering to off-screen color target
        var colorRenderTarget = RenderTargetDesc.Color(1024, 1024, TextureFormat.R8G8B8A8_UNorm);
        var offscreenColorTarget = _device.CreateRenderTarget(colorRenderTarget);

        var colorRenderer = new D3D12Renderer(_device);
        colorRenderer.SetRenderTarget(offscreenColorTarget);
        _renderSystem.AddRenderer(colorRenderer);

        // Example of rendering to depth target
        var depthRenderTarget = RenderTargetDesc.Depth(1024, 1024, TextureFormat.D24_UNorm_S8_UInt);
        var offscreenDepthTarget = _device.CreateRenderTarget(depthRenderTarget);

        var depthRenderer = new D3D12Renderer(_device);
        depthRenderer.SetRenderTarget(offscreenDepthTarget);
        _renderSystem.AddRenderer(depthRenderer);
    }

    public void Update()
    {
        // Signal that CPU work is ready
        _renderSystem.SignalCPUReady();

        // Wait for GPU to complete previous frame (optional)
        _renderSystem.WaitForGPUReady(16); // 16ms timeout for 60fps
    }

    public void Dispose()
    {
        _renderSystem?.Stop();
        _renderSystem?.Dispose();

        _forwardRenderer?.Dispose();
        _swapChain?.Dispose();
        _device?.Dispose();
    }
}

/// <summary>
/// Example showing legacy vs modern usage
/// </summary>
public static class LegacyVsModernExample
{
    public static void LegacyApproach()
    {
        // OLD WAY - tightly coupled to D3D12
        GraphicsPipeline.Initialize();

        var graphicsDevice = GraphicsPipeline.GraphicsDevice;
        // Renderer creation and management handled internally
        // Frame synchronization handled by GraphicsPipeline

        GraphicsPipeline.Start();
        GraphicsPipeline.SignalCPUReady();
        GraphicsPipeline.WaitForGPUReady();
    }

    public static void ModernApproach()
    {
        // NEW WAY - clean RHI abstractions
        var device = new D3D12RenderDevice();
        var renderSystem = new RenderSystem(device);

        var renderer = new D3D12Renderer(device);
        var swapChain = device.CreateSwapChain(new SwapChainDesc { /* ... */ });

        renderer.SetSwapChain(swapChain);
        renderSystem.AddRenderer(renderer);

        renderSystem.Start();
        renderSystem.SignalCPUReady();
        renderSystem.WaitForGPUReady();
    }
}
