using Ghost.Editor.Controls.Internal;
using Ghost.Graphics;
using Ghost.Graphics.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SharpGen.Runtime;
using WinRT;

namespace Ghost.Editor.View.Pages.EngineEditor;

internal sealed partial class ScenePage : NavigationTabPage
{
    private IRenderView? _renderer;

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
        var guid = typeof(Vortice.WinUI.ISwapChainPanelNative).GUID;
        Result result = ((IWinRTObject)SwapChainPanel).NativeObject.TryAs(guid, out var swapChainPanelNativeHandle);
        result.CheckError();

        var swapChainPanelNative = new Vortice.WinUI.ISwapChainPanelNative(swapChainPanelNativeHandle);
        _renderer = GraphicsPipeline.GraphicsDevice.CreateRenderView(new(swapChainPanelNative, (uint)SwapChainPanel.ActualWidth, (uint)SwapChainPanel.ActualHeight));

        CompositionTarget.Rendering += OnRendering;
    }

    private void SwapChainPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        _renderer?.Dispose();
    }

    private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 8.0 && e.NewSize.Height > 8.0)
        {
            _renderer?.Resize((uint)e.NewSize.Width, (uint)e.NewSize.Height);
        }
    }
}