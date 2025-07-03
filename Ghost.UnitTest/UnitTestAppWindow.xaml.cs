using Ghost.Graphics;
using Ghost.Graphics.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Misaki.HighPerformance.Unsafe.Buffer;
using WinRT;

namespace Ghost.UnitTest;

public sealed partial class UnitTestAppWindow : Window
{
    private IRenderer? _renderView;
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
            _renderView?.RequestResize((uint)e.NewSize.Width, (uint)e.NewSize.Height);
        }
    }

    private void UnitTestAppWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        AllocationManager.Initialize();
        GraphicsPipeline.Initialize(Graphics.Data.GraphicsAPI.D3D12);
        GraphicsPipeline.Start();

        var guid = typeof(ISwapChainPanelNative.Interface).GUID;
        ((IWinRTObject)Panel).NativeObject.TryAs(guid, out var swapChainPanelNativeHandle);
        _swapChainPanelNative = new ISwapChainPanelNative(swapChainPanelNativeHandle);

        _renderView = GraphicsPipeline.GraphicsDevice.CreateRenderer(new(_swapChainPanelNative, (uint)AppWindow.Size.Width, (uint)AppWindow.Size.Height));

        CompositionTarget.Rendering += OnRendering;
    }

    private void UnitTestAppWindow_Closed(object sender, WindowEventArgs args)
    {
        GraphicsPipeline.SignalCPUReady();
        GraphicsPipeline.Shutdown();
        AllocationManager.Dispose();
        CompositionTarget.Rendering -= OnRendering;
        _swapChainPanelNative.Dispose();
        _renderView?.Dispose();
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
