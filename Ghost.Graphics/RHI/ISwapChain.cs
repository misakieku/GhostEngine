using Ghost.Graphics.Contracts;

namespace Ghost.Graphics.RHI;

/// <summary>
/// Swap chain interface for presentation
/// </summary>
public interface ISwapChain : IDisposable
{
    /// <summary>
    /// Width of the swap chain back buffers
    /// </summary>
    uint Width { get; }

    /// <summary>
    /// Height of the swap chain back buffers
    /// </summary>
    uint Height { get; }

    /// <summary>
    /// Number of back buffers
    /// </summary>
    uint BufferCount { get; }

    /// <summary>
    /// Gets the current back buffer texture
    /// </summary>
    /// <returns>Current back buffer texture</returns>
    ITexture GetCurrentBackBuffer();

    /// <summary>
    /// Presents the rendered frame
    /// </summary>
    /// <param name="vsync">Enable vertical synchronization</param>
    void Present(bool vsync = true);

    /// <summary>
    /// Resizes the swap chain back buffers
    /// </summary>
    /// <param name="width">New width</param>
    /// <param name="height">New height</param>
    void Resize(uint width, uint height);
}

/// <summary>
/// Swap chain description
/// </summary>
public struct SwapChainDesc
{
    /// <summary>
    /// Width of the swap chain
    /// </summary>
    public uint Width;

    /// <summary>
    /// Height of the swap chain
    /// </summary>
    public uint Height;

    /// <summary>
    /// Back buffer format
    /// </summary>
    public TextureFormat Format;

    /// <summary>
    /// Number of back buffers
    /// </summary>
    public uint BufferCount;

    /// <summary>
    /// Target for presentation (window handle or composition target)
    /// </summary>
    public SwapChainTarget Target;

    public SwapChainDesc(uint width, uint height, SwapChainTarget target, TextureFormat format = TextureFormat.B8G8R8A8_UNorm, uint bufferCount = 2)
    {
        Width = width;
        Height = height;
        Format = format;
        BufferCount = bufferCount;
        Target = target;
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
    public SwapChainTargetType Type;

    /// <summary>
    /// Window handle for HWND targets
    /// </summary>
    public nint WindowHandle;

    /// <summary>
    /// Composition surface for UWP/WinUI targets
    /// </summary>
    public object? CompositionSurface;

    public static SwapChainTarget FromWindowHandle(nint hwnd)
    {
        return new SwapChainTarget
        {
            Type = SwapChainTargetType.WindowHandle,
            WindowHandle = hwnd,
            CompositionSurface = null
        };
    }

    public static SwapChainTarget FromCompositionSurface(object surface)
    {
        return new SwapChainTarget
        {
            Type = SwapChainTargetType.Composition,
            WindowHandle = nint.Zero,
            CompositionSurface = surface
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