namespace Ghost.Graphics.Data;

internal readonly struct SwapChainPresenter
{
    public enum TargetType
    {
        Composition,
        Hwnd
    }

    public readonly TargetType Type
    {
        get;
    }

    public readonly Vortice.WinUI.ISwapChainPanelNative? SwapChainPanelNative
    {
        get;
    }

    public readonly IntPtr Hwnd
    {
        get;
    }

    public readonly uint Width
    {
        get;
    }

    public readonly uint Height
    {
        get;
    }

    public SwapChainPresenter(Vortice.WinUI.ISwapChainPanelNative swapChainPanelNative, uint width, uint height)
    {
        Type = TargetType.Composition;
        SwapChainPanelNative = swapChainPanelNative;
        Hwnd = IntPtr.Zero;
        Width = width;
        Height = height;
    }

    public SwapChainPresenter(IntPtr hwnd, uint width, uint height)
    {
        Type = TargetType.Hwnd;
        SwapChainPanelNative = null;
        Hwnd = hwnd;
        Width = width;
        Height = height;
    }
}