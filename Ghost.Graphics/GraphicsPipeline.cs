using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;

namespace Ghost.Graphics;

/// <summary>
/// Legacy graphics pipeline - DEPRECATED
/// Use RenderSystem and D3D12RenderDevice for new code
/// This class remains for compatibility during migration
/// </summary>
[Obsolete("Use RenderSystem and D3D12RenderDevice instead")]
public static class GraphicsPipeline
{
    internal const uint _FRAME_COUNT = 2;

#if DEBUG
    private static DebugLayer? s_debugLayer;
#endif

    private static GraphicsDevice? s_graphicsDevice;
    private static DescriptorAllocator? s_descriptorAllocator;
    private static ResourceAllocator? s_resourceAllocator;
    private static ResourceUploadBatch? s_uploadBatch;

    // New RHI-based device for modern usage
    private static IRenderDevice? s_renderDevice;
    private static RenderSystem? s_renderSystem;

    private static bool s_initialized;

    internal static GraphicsDevice GraphicsDevice => s_graphicsDevice ?? throw new InvalidOperationException("Graphics device is not initialized.");
    internal static ResourceAllocator ResourceAllocator => s_resourceAllocator ?? throw new InvalidOperationException("Resource allocator is not initialized.");
    internal static DescriptorAllocator DescriptorAllocator => s_descriptorAllocator ?? throw new InvalidOperationException("Descriptor allocator is not initialized.");

    /// <summary>
    /// Gets the modern RHI render device - prefer this over legacy GraphicsDevice
    /// </summary>
    public static IRenderDevice RenderDevice => s_renderDevice ?? throw new InvalidOperationException("Render device is not initialized.");

    /// <summary>
    /// Gets the render system for managing renderers and frame synchronization
    /// </summary>
    public static RenderSystem RenderSystem => s_renderSystem ?? throw new InvalidOperationException("Render system is not initialized.");

    internal static ResourceUploadBatch UploadBatch
    {
        get
        {
            if (s_uploadBatch == null)
            {
                s_uploadBatch = new();
                s_uploadBatch.Begin();
            }

            return s_uploadBatch;
        }
    }

    internal static unsafe void Initialize()
    {
#if DEBUG
        s_debugLayer = new DebugLayer();
#endif
        // Initialize legacy components for compatibility
        s_graphicsDevice = new GraphicsDevice();
        s_descriptorAllocator = new DescriptorAllocator();
        s_resourceAllocator = new ResourceAllocator((IDXGIAdapter*)s_graphicsDevice.Adapter.Ptr, (ID3D12Device*)s_graphicsDevice.NativeDevice.Ptr);

        // Initialize modern RHI components
        s_renderDevice = new D3D12.D3D12RenderDevice();
        s_renderSystem = new RenderSystem(s_renderDevice);

        s_initialized = true;
    }

    /// <summary>
    /// Legacy method - use RenderSystem.Start() instead
    /// </summary>
    [Obsolete("Use RenderSystem.Start() instead")]
    internal static void Start()
    {
        s_renderSystem?.Start();
    }

    /// <summary>
    /// Legacy method - use RenderSystem.Stop() instead
    /// </summary>
    [Obsolete("Use RenderSystem.Stop() instead")]
    internal static void Stop()
    {
        s_renderSystem?.Stop();
    }

    /// <summary>
    /// Legacy method - use RenderSystem.WaitForGPUReady() instead
    /// </summary>
    [Obsolete("Use RenderSystem.WaitForGPUReady() instead")]
    internal static bool WaitForGPUReady(int timeOut = -1)
    {
        return s_renderSystem?.WaitForGPUReady(timeOut) ?? false;
    }

    /// <summary>
    /// Legacy method - use RenderSystem.SignalCPUReady() instead
    /// </summary>
    [Obsolete("Use RenderSystem.SignalCPUReady() instead")]
    internal static void SignalCPUReady()
    {
        s_renderSystem?.SignalCPUReady();
    }

    internal static void Shutdown()
    {
        s_renderSystem?.Dispose();
        s_renderDevice?.Dispose();

        s_resourceAllocator?.Dispose();
        s_descriptorAllocator?.Dispose();
        s_graphicsDevice?.Dispose();

#if DEBUG
        s_debugLayer?.Dispose();
#endif

        s_initialized = false;
    }
}