using Ghost.Editor.Controls.Internal;
using Ghost.Graphics;
using Ghost.Graphics.Contracts;
using Microsoft.UI.Xaml;
using Vortice.WinUI;
using WinRT;

namespace Ghost.Editor.View.Pages.EngineEditor;

internal sealed partial class ScenePage : NavigationTabPage
{
    private IRenderView? _renderer;
    private ISwapChainPanelNative2? _swapChainPanelNative;

    public ScenePage()
    {
        InitializeComponent();

        SwapChainPanel.Loaded += SwapChainPanel_Loaded;
        SwapChainPanel.Unloaded += SwapChainPanel_Unloaded;
        SwapChainPanel.SizeChanged += SwapChainPanel_SizeChanged;
    }

    private void OnRendering(object? sender, object e)
    {
        _renderer?.Render();
    }

    private void SwapChainPanel_Loaded(object sender, RoutedEventArgs e)
    {
        var guid = typeof(ISwapChainPanelNative2).GUID;
        ((IWinRTObject)SwapChainPanel).NativeObject.TryAs(guid, out var swapChainPanelNativeHandle);

        _swapChainPanelNative = new ISwapChainPanelNative2(swapChainPanelNativeHandle);
        _renderer = GraphicsPipeline.GraphicsDevice.CreateRenderView(new(_swapChainPanelNative, (uint)SwapChainPanel.ActualWidth, (uint)SwapChainPanel.ActualHeight));

        //CompositionTarget.Rendering += OnRendering;
    }

    private void SwapChainPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        //CompositionTarget.Rendering -= OnRendering;
        _swapChainPanelNative?.Dispose();
        _renderer?.Dispose();
    }

    private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 8.0 && e.NewSize.Height > 8.0)
        {
            _renderer?.RequestResize((uint)e.NewSize.Width, (uint)e.NewSize.Height);
        }
    }
}