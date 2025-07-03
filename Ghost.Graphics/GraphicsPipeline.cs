using Ghost.Core;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.Data;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics;

public static class GraphicsPipeline
{
    internal const int _FRAME_COUNT = 2;

    private static IGraphicsDevice? _graphicsDevice;
    private static IResourceAllocator? _resourceAllocator;

    private static Thread? _renderThread;
    private static AutoResetEvent[]? _cpuReadyEvent;
    private static AutoResetEvent[]? _gpuReadyEvent;

    private static uint _cpuFenceValue;
    private static uint _gpuFenceValue;

    private static bool _initialized;
    private static bool _isRunning;

    internal static uint CPUFenceValue => _cpuFenceValue;
    internal static uint GPUFenceValue => _gpuFenceValue;
    internal static bool IsRunning => _isRunning;

    internal static IGraphicsDevice GraphicsDevice
    {
        get
        {
            if (_graphicsDevice == null)
            {
                throw new InvalidOperationException("Graphics pipeline is not initialized.");
            }

            return _graphicsDevice;
        }
    }

    internal static IResourceAllocator ResourceAllocator
    {
        get
        {
            if (_resourceAllocator == null)
            {
                throw new InvalidOperationException("Resource allocator is not initialized.");
            }

            return _resourceAllocator;
        }
    }

    public static GraphicsAPI CurrentAPI
    {
        get;
        private set;
    }

    internal static void Initialize(GraphicsAPI api)
    {
        switch (api)
        {
            case GraphicsAPI.D3D12:
                _graphicsDevice = new D3D12GraphicsDevice();
                _resourceAllocator = new D3D12ResourceAllocator();
                break;
            default:
                throw new NotSupportedException($"Graphics API {api} is not supported.");
        }

        _renderThread = new Thread(RenderLoop)
        {
            IsBackground = true,
            Name = "Graphics Render Thread",
            Priority = ThreadPriority.Normal
        };

        _cpuReadyEvent = new AutoResetEvent[_FRAME_COUNT];
        _gpuReadyEvent = new AutoResetEvent[_FRAME_COUNT];
        for (var i = 0; i < _FRAME_COUNT; i++)
        {
            _cpuReadyEvent[i] = new(false);
            _gpuReadyEvent[i] = new(true);
        }

        CurrentAPI = api;

        _initialized = true;
    }

    private static void RenderLoop()
    {
        while (_isRunning)
        {
            if (_graphicsDevice == null)
            {
                throw new ArgumentException("Renderer has been disposed or is not initialized.");
            }

            var eventIndex = (int)(_gpuFenceValue % _FRAME_COUNT);
            _cpuReadyEvent![eventIndex].WaitOne();

            _graphicsDevice.InitializePendingRenderers();

            foreach (var renderer in _graphicsDevice.Renderers)
            {
                renderer.ExecutePendingResize();
                renderer.Render();
            }

            _gpuFenceValue++;
            _gpuReadyEvent![eventIndex].Set();

            _resourceAllocator!.ReleaseTempResource();
        }
    }


    internal static bool WaitForGPUReady(int timeOut = -1)
    {
        if (_gpuReadyEvent == null)
        {
            throw new InvalidOperationException("Graphics pipeline is not initialized.");
        }

        var eventIndex = (int)(_cpuFenceValue % _FRAME_COUNT);
        return _gpuReadyEvent[eventIndex].WaitOne(timeOut);
    }

    internal static void SignalCPUReady()
    {
        if (_cpuReadyEvent == null)
        {
            throw new InvalidOperationException("Graphics pipeline is not initialized.");
        }

        var eventIndex = (int)(_cpuFenceValue % _FRAME_COUNT);
        _cpuFenceValue++;
        _cpuReadyEvent[eventIndex].Set();
    }

    internal static void Start()
    {
        if (_isRunning || !_initialized)
        {
            return;
        }

        _isRunning = true;
        _renderThread!.Start();
    }

    internal static void Stop()
    {
        _isRunning = false;
        _renderThread?.Join();
    }

    internal static void Shutdown()
    {
        Stop();

        _graphicsDevice?.Dispose();
        _resourceAllocator?.Dispose();

        _graphicsDevice = null;
        _renderThread = null;
        _initialized = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T GetGraphicsDevice<T>()
        where T : class, IGraphicsDevice
    {
        if (T.TargetAPI != CurrentAPI)
        {
            throw new InvalidOperationException($"No graphics device of type {typeof(T)} available for the current API.");
        }

        return Unsafe.As<T>(GraphicsDevice);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result CheckAPI(GraphicsAPI expectedAPI)
    {
        if (CurrentAPI != expectedAPI)
        {
            return Result.Failure($"Expected API {expectedAPI}, but got {CurrentAPI}.");
        }

        return Result.Success();
    }
}