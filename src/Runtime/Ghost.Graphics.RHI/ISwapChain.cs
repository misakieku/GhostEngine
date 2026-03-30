using Ghost.Core;

namespace Ghost.Graphics.RHI;

/// <summary>
/// Swap chain interface for presentation
/// </summary>
public interface ISwapChain : IDisposable
{
    /// <summary>
    /// Width of the swap chain back buffers
    /// </summary>
    uint Width
    {
        get;
    }

    /// <summary>
    /// Height of the swap chain back buffers
    /// </summary>
    uint Height
    {
        get;
    }

    /// <summary>
    /// Gets the horizontal scaling factor applied to the object. This is used for DPI scaling.
    /// </summary>
    float ScaleX
    {
        get;
    }

    /// <summary>
    /// Gets the vertical scale factor applied to the object. This is used for DPI scaling.
    /// </summary>
    float ScaleY
    {
        get;
    }

    /// <summary>
    /// Gets the current back buffer texture
    /// </summary>
    /// <returns>Current back buffer texture</returns>
    Handle<GPUTexture> GetCurrentBackBuffer();

    /// <summary>
    /// Gets all back buffer textures
    /// </summary>
    /// <returns>AlowBufferAndTexture back buffer textures</returns>
    ReadOnlySpan<Handle<GPUTexture>> GetBackBuffers();

    /// <summary>
    /// Presents the rendered frame
    /// </summary>
    /// <param name="vsync">Enable vertical synchronization</param>
    void Present(bool vsync = true);

    /// <summary>
    /// Resizes the swap chain back buffers
    /// </summary>
    /// <param name="width">New Width</param>
    /// <param name="height">New Height</param>
    void Resize(uint width, uint height);

    /// <summary>
    /// Sets the horizontal and vertical scaling factors for the object.
    /// </summary>
    /// <param name="scaleX">The factor by which to scale the object along the X-axis.</param>
    /// <param name="scaleY">The factor by which to scale the object along the Y-axis.</param>
    void SetScale(float scaleX, float scaleY);
}
