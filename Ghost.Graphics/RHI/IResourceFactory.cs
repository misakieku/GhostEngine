using Ghost.Graphics.Data;

namespace Ghost.Graphics.RHI;

public interface IResourceFactory
{
    /// <summary>
    /// Creates a render target for off-screen rendering
    /// </summary>
    /// <param name="desc">Render target description</param>
    /// <returns>A new render target instance</returns>
    public IRenderTarget CreateRenderTarget(ref readonly RenderTargetDesc desc);

    /// <summary>
    /// Creates a texture resource
    /// </summary>
    /// <param name="desc">Texture description</param>
    /// <returns>A new texture handle point to the resource</returns>
    public TextureHandle CreateTextureHandle(ref readonly TextureDesc desc);

    /// <summary>
    /// Creates a texture resource
    /// </summary>
    /// <param name="desc">Texture description</param>
    /// <returns>A new texture instance</returns>
    public ITexture CreateTexture(ref readonly TextureDesc desc);

    /// <summary>
    /// Creates a buffer resource
    /// </summary>
    /// <param name="desc">Buffer description</param>
    /// <returns>A new buffer handle point to the resource</returns>
    public BufferHandle CreateBufferHandle(ref readonly BufferDesc desc);

    /// <summary>
    /// Creates a buffer resource
    /// </summary>
    /// <param name="desc">Buffer description</param>
    /// <returns>A new buffer instance</returns>
    public IBuffer CreateBuffer(ref readonly BufferDesc desc);
}