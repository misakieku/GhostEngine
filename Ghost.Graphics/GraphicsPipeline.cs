using Ghost.Graphics.Contracts;
using Ghost.Graphics.Data;

namespace Ghost.Graphics;

public static class GraphicsPipeline
{
    internal const int FRAME_COUNT = 2;

    private static IGraphicsDevice? _graphicsDevice;
    private static Thread? _renderThread;

    private static bool _isRunning;

    public static IGraphicsDevice GraphicsDevice
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

    internal static void Initialize(GraphicsAPI api)
    {
        _graphicsDevice = api switch
        {
            GraphicsAPI.DX12 => DX12.DX12GraphicsDevice.Create(),
            _ => throw new NotSupportedException($"Graphics API {api} is not supported.")
        };

        _renderThread = new Thread(RenderLoop);
    }

    private static void RenderLoop()
    {
        while (_isRunning)
        {
            GraphicsDevice.OnRender();
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

        _graphicsDevice = null;
        _renderThread = null;
    }
}