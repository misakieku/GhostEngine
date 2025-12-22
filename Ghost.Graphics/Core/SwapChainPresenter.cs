using Ghost.Graphics.Contracts;

namespace Ghost.Graphics.Core;

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

    public readonly ISwapChainPanelNative SwapChainPanelNative
    {
        get;
    }

    public readonly nint Hwnd
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

    public SwapChainPresenter(ISwapChainPanelNative swapChainPanelNative, uint width, uint height)
    {
        Type = TargetType.Composition;
        SwapChainPanelNative = swapChainPanelNative;
        Hwnd = nint.Zero;
        Width = width;
        Height = height;
    }

    public SwapChainPresenter(nint hwnd, uint width, uint height)
    {
        Type = TargetType.Hwnd;
        Hwnd = hwnd;
        Width = width;
        Height = height;
    }
}
