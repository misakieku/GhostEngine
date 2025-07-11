using Ghost.Graphics.D3D12;

namespace Ghost.Graphics;

public static class GraphicsPipeline
{
    internal const uint _FRAME_COUNT = 2;

    private readonly struct FrameResource : IDisposable
    {
        public readonly AutoResetEvent cpuReadyEvent;
        public readonly AutoResetEvent gpuReadyEvent;

        public FrameResource()
        {
            cpuReadyEvent = new(false);
            gpuReadyEvent = new(true);
        }

        public void Dispose()
        {
            cpuReadyEvent?.Dispose();
            gpuReadyEvent?.Dispose();
        }
    }

    private static GraphicsDevice? s_graphicsDevice;
    private static DescriptorAllocator? s_descriptorAllocator;
    private static ResourceAllocator? s_resourceAllocator;

    private static Thread? s_renderThread;
    private static FrameResource[]? s_frameResources;

    private static uint s_frameIndex;
    private static uint s_cpuFenceValue;
    private static uint s_gpuFenceValue;

    private static bool s_initialized;
    private static bool s_isRunning;

    internal static uint CPUFenceValue => s_cpuFenceValue;
    internal static uint GPUFenceValue => s_gpuFenceValue;
    internal static bool IsRunning => s_isRunning;

    internal static GraphicsDevice GraphicsDevice => s_graphicsDevice ?? throw new InvalidOperationException("Graphics device is not initialized.");
    internal static ResourceAllocator ResourceAllocator => s_resourceAllocator ?? throw new InvalidOperationException("Resource allocator is not initialized.");
    internal static DescriptorAllocator DescriptorAllocator => s_descriptorAllocator ?? throw new InvalidOperationException("Descriptor allocator is not initialized.");

    internal static void Initialize()
    {
        s_graphicsDevice = new GraphicsDevice();
        s_descriptorAllocator = new DescriptorAllocator();
        s_resourceAllocator = new ResourceAllocator();

        s_renderThread = new Thread(RenderLoop)
        {
            IsBackground = true,
            Name = "Graphics Render Thread",
            Priority = ThreadPriority.Normal
        };

        s_frameResources = new FrameResource[_FRAME_COUNT];
        for (var i = 0; i < _FRAME_COUNT; i++)
        {
            s_frameResources[i] = new FrameResource();
        }

        s_initialized = true;
    }

    private static void RenderLoop()
    {
        while (s_isRunning)
        {
            s_frameIndex = s_gpuFenceValue % _FRAME_COUNT;
            var frameResource = s_frameResources![s_frameIndex];

            frameResource.cpuReadyEvent.WaitOne();

            s_graphicsDevice!.InitializePendingRenderers();

            foreach (var renderer in s_graphicsDevice.Renderers)
            {
                renderer.ExecutePendingResize();
                renderer.Render();
            }

            s_gpuFenceValue++;
            frameResource.gpuReadyEvent.Set();

            s_resourceAllocator!.ReleaseTempResource();
        }
    }

    internal static bool WaitForGPUReady(int timeOut = -1)
    {
        if (s_frameResources == null)
        {
            throw new InvalidOperationException("Graphics pipeline is not initialized.");
        }

        var eventIndex = (int)(s_cpuFenceValue % _FRAME_COUNT);
        return s_frameResources[eventIndex].gpuReadyEvent.WaitOne(timeOut);
    }

    internal static void SignalCPUReady()
    {
        if (s_frameResources == null)
        {
            throw new InvalidOperationException("Graphics pipeline is not initialized.");
        }

        var eventIndex = (int)(s_cpuFenceValue % _FRAME_COUNT);
        s_cpuFenceValue++;
        s_frameResources[eventIndex].cpuReadyEvent.Set();
    }

    internal static void Start()
    {
        if (s_isRunning || !s_initialized)
        {
            return;
        }

        s_isRunning = true;
        s_renderThread!.Start();
    }

    internal static void Stop()
    {
        s_isRunning = false;
        s_renderThread?.Join();
    }

    internal static void Shutdown()
    {
        Stop();

        s_resourceAllocator?.Dispose();
        s_descriptorAllocator?.Dispose();
        s_graphicsDevice?.Dispose();

        if (s_frameResources != null)
        {
            foreach (var frameResource in s_frameResources)
            {
                frameResource.Dispose();
            }
            s_frameResources = null;
        }

        s_initialized = false;
    }
}