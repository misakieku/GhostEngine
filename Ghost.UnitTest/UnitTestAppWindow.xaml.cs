using Ghost.Graphics;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.D3D12;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Misaki.HighPerformance.LowLevel.Buffer;
using WinRT;

namespace Ghost.UnitTest;

public sealed partial class UnitTestAppWindow : Window
{
    private Renderer? _renderer;
    private ISwapChainPanelNative _swapChainPanelNative;

    public UnitTestAppWindow()
    {
        InitializeComponent();

        Activated += UnitTestAppWindow_Activated;
        Closed += UnitTestAppWindow_Closed;

        Panel.SizeChanged += SwapChainPanel_SizeChanged;
    }

    private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 8.0 && e.NewSize.Height > 8.0)
        {
            _renderer?.RequestResize((uint)e.NewSize.Width, (uint)e.NewSize.Height);
        }
    }

    private void UnitTestAppWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
#if DEBUG
        AllocationManager.EnableDebugLayer();
#endif
        GraphicsPipeline.Initialize();
        GraphicsPipeline.Start();

        var guid = typeof(ISwapChainPanelNative.Interface).GUID;
        ((IWinRTObject)Panel).NativeObject.TryAs(guid, out var swapChainPanelNativeHandle);
        _swapChainPanelNative = new ISwapChainPanelNative(swapChainPanelNativeHandle);

        _renderer = GraphicsPipeline.GraphicsDevice.CreateRenderer(new(_swapChainPanelNative, (uint)AppWindow.Size.Width, (uint)AppWindow.Size.Height));

        CompositionTarget.Rendering += OnRendering;
    }

    private void UnitTestAppWindow_Closed(object sender, WindowEventArgs args)
    {
        CompositionTarget.Rendering -= OnRendering;

        GraphicsPipeline.SignalCPUReady();
        GraphicsPipeline.Shutdown();

        _swapChainPanelNative.Dispose();
        _renderer?.Dispose();
    }

    private void OnRendering(object? sender, object e)
    {
        if (GraphicsPipeline.WaitForGPUReady(0))
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
            {
                GraphicsPipeline.SignalCPUReady();
            });
        }
    }
}
