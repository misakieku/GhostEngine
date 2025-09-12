using Ghost.Graphics;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Misaki.HighPerformance.LowLevel.Buffer;
using WinRT;

namespace Ghost.UnitTest.Windows;

public sealed partial class GraphicsTestWindow : Window
{
    private RenderSystem? _renderSystem;
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

        _renderSystem = new (GraphicsAPI.Direct3D12);
        _renderer = _renderSystem.CreateRenderer();

        _swapChain = _renderSystem.GraphicsEngine.Device.CreateSwapChain(new SwapChainDesc((uint)AppWindow.Size.Width, (uint)AppWindow.Size.Height, SwapChainTarget.FromCompositionSurface(Panel)));
        _renderer.SetSwapChain(_swapChain);

        CompositionTarget.Rendering += OnRendering;
    }

    private void SwapChainPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;

        _swapChain?.Dispose();
        _renderer?.Dispose();
        _renderSystem?.Dispose();
    }

    private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 8.0 && e.NewSize.Height > 8.0)
        {
            _renderer?.RequestResize((uint)e.NewSize.Width, (uint)e.NewSize.Height);
        }
    }

    private void OnRendering(object? sender, object e)
    {
        //if (GraphicsPipeline.CPUFenceValue < GraphicsPipeline.GPUFenceValue + GraphicsPipeline._FRAME_COUNT)
        //{
        //    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
        //    {
        //        GraphicsPipeline.SignalCPUReady();
        //    });
        //}
    }
}
