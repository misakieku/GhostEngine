using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Misaki.HighPerformance.Mathematics;
using static Ghost.Graphics.D3D12.D3D12ResourceDatabase;

namespace Ghost.Graphics.Test.Windows;

public sealed partial class GraphicsTestWindow : Window
{
    private IRenderSystem? _renderSystem;
    private IRenderer? _renderer;
    private ISwapChain? _swapChain;

    private bool _isFirstActivationHandled;

    public unsafe GraphicsTestWindow()
    {
        InitializeComponent();

        Activated += GraphicsTestWindow_Activated;
        Closed += GraphicsTestWindow_Closed;

        Panel.SizeChanged += SwapChainPanel_SizeChanged;
        Panel.CompositionScaleChanged += SwapChainPanel_CompositionScaleChanged;
    }

    private void GraphicsTestWindow_Activated(object sender, WindowActivatedEventArgs e)
    {
        if (_isFirstActivationHandled)
        {
            return;
        }

#if DEBUG
        Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.EnableDebugLayer();
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
            ScaleX = Panel.CompositionScaleX,
            ScaleY = Panel.CompositionScaleY,
            Format = TextureFormat.B8G8R8A8_UNorm,
            Target = SwapChainTarget.FromCompositionSurface(Panel)
        });

        _renderer.RenderOutput = new SwapChainRenderOutput(_swapChain);

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

        Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.Dispose();
    }

    private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_renderSystem == null || _swapChain == null || _renderer == null)
        {
            return;
        }

        var newWidth = (uint)(Panel.ActualWidth * Panel.CompositionScaleX);
        var newHeight = (uint)(Panel.ActualHeight * Panel.CompositionScaleY);

        if (newWidth < 8 || newHeight < 8)
        {
            return;
        }

        _renderSystem.RequestSwapChainResize(_swapChain, new uint2(newWidth, newHeight));
        _renderer.RenderOutput!.Viewport = new ViewportDesc { Width = newWidth, Height = newHeight, MinDepth = 0.0f, MaxDepth = 1.0f };
        _renderer.RenderOutput!.Scissor = new RectDesc { Right = newWidth, Bottom = newHeight };
    }

    private void SwapChainPanel_CompositionScaleChanged(SwapChainPanel sender, object args)
    {
        _swapChain?.SetScale(sender.CompositionScaleX, sender.CompositionScaleY);
    }

    private void OnRendering(object? sender, object e)
    {
        if (_renderSystem == null)
        {
            return;
        }

        if (_renderSystem.CPUFenceValue < _renderSystem.GPUFenceValue + _renderSystem.MaxFrameLatency)
        {
            _renderSystem.SignalCPUReady();
        }
    }
}
