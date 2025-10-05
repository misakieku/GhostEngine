using Ghost.Core;
using Ghost.Graphics.Data;

namespace Ghost.Graphics.RHI;

/// <summary>
/// Swap chain interface for presentation
/// </summary>
public interface ISwapChain : IDisposable
{
    /// <summary>
    /// Width of the swap chain back buffers
    /// </summary>
    public uint Width
    {
        get;
    }

    /// <summary>
    /// Height of the swap chain back buffers
    /// </summary>
    public uint Height
    {
        get;
    }

    /// <summary>
    /// Number of back buffers
    /// </summary>
    public uint BufferCount
    {
        get;
    }

    /// <summary>
    /// Gets the current back buffer texture
    /// </summary>
    /// <returns>Current back buffer texture</returns>
    public Handle<Texture> GetCurrentBackBuffer();

    /// <summary>
    /// Presents the rendered frame
    /// </summary>
    /// <param name="vsync">Enable vertical synchronization</param>
    public void Present(bool vsync = true);

    /// <summary>
    /// Resizes the swap chain back buffers
    /// </summary>
    /// <param name="width">New width</param>
    /// <param name="height">New height</param>
    public void Resize(uint width, uint height);
}

/// <summary>
/// Swap chain description
/// </summary>
public struct SwapChainDesc
{
    /// <summary>
    /// Width of the swap chain
    /// </summary>
    public uint width;

    /// <summary>
    /// Height of the swap chain
    /// </summary>
    public uint height;

    /// <summary>
    /// Back buffer format
    /// </summary>
    public TextureFormat format;

    /// <summary>
    /// Target for presentation (window handle or composition target)
    /// </summary>
    public SwapChainTarget target;

    public SwapChainDesc(uint width, uint height, SwapChainTarget target, TextureFormat format = TextureFormat.B8G8R8A8_UNorm, uint bufferCount = 2)
    {
        this.width = width;
        this.height = height;
        this.format = format;
        this.target = target;
    }
}

/// <summary>
/// Swap chain target (window handle or composition surface)
/// </summary>
public struct SwapChainTarget
{
    /// <summary>
    /// Target type
    /// </summary>
    public SwapChainTargetType type;

    /// <summary>
    /// Window handle for HWND targets
    /// </summary>
    public nint windowHandle;

    /// <summary>
    /// Composition surface for UWP/WinUI targets
    /// </summary>
    public object? compositionSurface;

    public static SwapChainTarget FromWindowHandle(nint hwnd)
    {
        return new SwapChainTarget
        {
            type = SwapChainTargetType.WindowHandle,
            windowHandle = hwnd,
            compositionSurface = null
        };
    }

    public static SwapChainTarget FromCompositionSurface(object surface)
    {
        return new SwapChainTarget
        {
            type = SwapChainTargetType.Composition,
            windowHandle = nint.Zero,
            compositionSurface = surface
        };
    }
}

/// <summary>
/// Swap chain target types
/// </summary>
public enum SwapChainTargetType
{
    WindowHandle,
    Composition
}