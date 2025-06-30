using Ghost.Core;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.Data;
using Ghost.Graphics.DX12;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics;

public static class GraphicsPipeline
{
    internal const int _FRAME_COUNT = 2;

    private static IGraphicsDevice? _graphicsDevice;
    private static IResourceAllocator? _resourceAllocator;

    private static Thread? _renderThread;

    private static bool _isRunning;

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
            case GraphicsAPI.DX12:
                _graphicsDevice = new DX12GraphicsDevice();
                _resourceAllocator = new DX12ResourceAllocator();
                break;
            default:
                throw new NotSupportedException($"Graphics API {api} is not supported.");
        }

        _renderThread = new Thread(RenderLoop);

        CurrentAPI = api;
    }

    private static void RenderLoop()
    {
        while (_isRunning)
        {
            if (_graphicsDevice == null)
            {
                throw new ArgumentException("Renderer has been disposed or is not initialized.");
            }

            foreach (var renderer in _graphicsDevice.Renderers)
            {
                renderer.ExecutePendingResize();
                renderer.Render();
            }
        }
    }

    internal static void Start()
    {
        if (_isRunning)
        {
            return;
        }

        if (_graphicsDevice == null || _renderThread == null)
        {
            throw new InvalidOperationException("Graphics pipeline is not initialized.");
        }

        _isRunning = true;
        _renderThread.Start();
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T GetRenderer<T>()
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