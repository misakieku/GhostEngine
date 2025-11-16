using Ghost.Core;
using Ghost.Graphics.Core;

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
    /// <param name="width">New Width</param>
    /// <param name="height">New Height</param>
    public void Resize(uint width, uint height);
}