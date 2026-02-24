using Ghost.Editor.Controls;
//using Ghost.Graphics.Contracts;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using WinRT;

namespace Ghost.Editor.View.Pages.EngineEditor;

internal sealed partial class ScenePage : NavigationTabPage
{
    //private Renderer? _renderView;
    //private ISwapChainPanelNative _swapChainPanelNative;

    public ScenePage()
    {
        InitializeComponent();

        //SwapChainPanel.Loaded += SwapChainPanel_Loaded;
        //SwapChainPanel.Unloaded += SwapChainPanel_Unloaded;
        //SwapChainPanel.SizeChanged += SwapChainPanel_SizeChanged;
    }

    //private void SwapChainPanel_Loaded(object sender, RoutedEventArgs e)
    //{
    //    var guid = typeof(ISwapChainPanelNative.Interface).GUID;
    //    ((IWinRTObject)SwapChainPanel).NativeObject.TryAs(guid, out var swapChainPanelNativeHandle);
    //    _swapChainPanelNative = new ISwapChainPanelNative(swapChainPanelNativeHandle);

    //    _renderView = GraphicsPipeline.GraphicsDevice.CreateRenderer(new(_swapChainPanelNative, (uint)SwapChainPanel.ActualWidth, (uint)SwapChainPanel.ActualHeight));
    //}

    //private void SwapChainPanel_Unloaded(object sender, RoutedEventArgs e)
    //{
    //    _swapChainPanelNative.Dispose();
    //    _renderView?.Dispose();
    //}

    //private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    //{
    //    if (e.NewSize.ActualWidth > 8.0 && e.NewSize.ActualHeight > 8.0)
    //    {
    //        _renderView?.RequestResize((uint)e.NewSize.ActualWidth, (uint)e.NewSize.ActualHeight);
    //    }
    //}
}