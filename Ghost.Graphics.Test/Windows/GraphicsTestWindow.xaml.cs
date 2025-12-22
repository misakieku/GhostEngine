using Ghost.Graphics.RHI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;
using System.Text.Json;

namespace Ghost.Graphics.Test.Windows;

public sealed partial class GraphicsTestWindow : Window
{
    private DispatcherTimer _resizeTimer;
    private IRenderSystem? _renderSystem;
    private IRenderer? _renderer;
    private ISwapChain? _swapChain;

    private bool _isFirstActivationHandled;
    private bool _isResizing;

    public GraphicsTestWindow()
    {
        InitializeComponent();

        Activated += GraphicsTestWindow_Activated;
        Closed += GraphicsTestWindow_Closed;

        _resizeTimer = new DispatcherTimer();
        _resizeTimer.Interval = TimeSpan.FromMilliseconds(200);
        _resizeTimer.Tick += OnResizeTimerTick;

        Panel.SizeChanged += SwapChainPanel_SizeChanged;
    }

    private void GraphicsTestWindow_Activated(object sender, WindowActivatedEventArgs e)
    {
        if (_isFirstActivationHandled)
        {
            return;
        }

#if DEBUG
        AllocationManager.EnableDebugLayer();
#endif

        _renderSystem = new RenderSystem(new RenderingConfig()
        {
            FrameBufferCount = 2,
            GraphicsAPI = GraphicsAPI.Direct3D12
        });
        _renderer = _renderSystem.GraphicsEngine.CreateRenderer();
        _swapChain = _renderSystem.GraphicsEngine.CreateSwapChain(new SwapChainDesc
        {
            Width = (uint)AppWindow.Size.Width,
            Height = (uint)AppWindow.Size.Height,
            Format = TextureFormat.B8G8R8A8_UNorm,
            Target = SwapChainTarget.FromCompositionSurface(Panel)
        });

        _renderer.SetRenderTarget(_swapChain.GetCurrentBackBuffer());

        _renderSystem.Start();
        CompositionTarget.Rendering += OnRendering;

        e.Handled = true;
        _isFirstActivationHandled = true;
    }

    private void GraphicsTestWindow_Closed(object sender, WindowEventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        _renderSystem?.Stop();

        _renderer?.Dispose();
        _swapChain?.Dispose();
        _renderSystem?.Dispose();

#if DEBUG
        AllocationManager.Dispose();
#endif
    }

    private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        //if (e.NewSize.Width > 8.0 && e.NewSize.Height > 8.0)
        //{
        //    _renderer?.RequestResize(new((uint)e.NewSize.Width, (uint)e.NewSize.Height));
        //}

        _resizeTimer.Stop();
        _resizeTimer.Start();

        _isResizing = true;
    }

    private void OnResizeTimerTick(object? sender, object e)
    {
        _resizeTimer.Stop();
        _isResizing = false;

        var newWidth = (uint)(Panel.ActualWidth * Panel.CompositionScaleX);
        var newHeight = (uint)(Panel.ActualHeight * Panel.CompositionScaleY);

        if (newWidth < 8 || newHeight < 8)
        {
            return;
        }

        _renderSystem?.WaitIdle();
        _swapChain?.Resize(newWidth, newHeight);
    }

    private void OnRendering(object? sender, object e)
    {
        if (_renderSystem == null)
        {
            return;
        }

        if (_renderSystem.CPUFenceValue < _renderSystem.GPUFenceValue + _renderSystem.MaxFrameLatency)
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
            {
                _renderSystem.SignalCPUReady();
            });
        }
    }
}
