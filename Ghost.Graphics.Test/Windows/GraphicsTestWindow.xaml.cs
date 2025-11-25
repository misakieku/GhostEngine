using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Misaki.HighPerformance.LowLevel.Buffer;
using TerraFX.Interop.WinRT;
using WinRT;

namespace Ghost.Graphics.Test.Windows;

public sealed partial class GraphicsTestWindow : Window
{
    private IRenderSystem? _renderSystem;
    private IRenderer? _renderer;
    private ISwapChain? _swapChain;

    public GraphicsTestWindow()
    {
        InitializeComponent();

        Panel.Loaded += SwapChainPanel_Loaded;
        Panel.Unloaded += SwapChainPanel_Unloaded;

        Panel.SizeChanged += SwapChainPanel_SizeChanged;
    }

    private void SwapChainPanel_Loaded(object sender, RoutedEventArgs e)
    {
#if DEBUG
        AllocationManager.EnableDebugLayer();
#endif

        _renderSystem = new RenderSystem(new()
        {
            FrameBufferCount = 2,
            GraphicsAPI = GraphicsAPI.Direct3D12
        });
        _renderer = _renderSystem.GraphicsEngine.CreateRenderer();
        
        _swapChain = _renderSystem.GraphicsEngine.CreateSwapChain(new SwapChainDesc((uint)AppWindow.Size.Width, (uint)AppWindow.Size.Height, SwapChainTarget.FromCompositionSurface(Panel)));
        _renderer.SetSwapChain(_swapChain);

        _renderSystem.Start();
        CompositionTarget.Rendering += OnRendering;
    }

    private void SwapChainPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        _renderSystem?.Stop();

        _renderer?.Dispose();
        _swapChain?.Dispose();
        _renderSystem?.Dispose();
    }

    private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 8.0 && e.NewSize.Height > 8.0)
        {
            _renderer?.RequestResize(new((uint)e.NewSize.Width, (uint)e.NewSize.Height));
        }
    }

    private void OnRendering(object? sender, object e)
    {
        if (_renderSystem == null)
        {
            return;
        }

        if (_renderSystem.CPUFenceValue < _renderSystem.GPUFenceValue + _renderSystem.Config.FrameBufferCount)
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
            {
                _renderSystem.SignalCPUReady();
            });
        }
    }
}
