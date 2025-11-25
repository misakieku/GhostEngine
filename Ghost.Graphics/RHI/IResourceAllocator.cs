using Ghost.Core;
using Ghost.Core.Graphics;
using Misaki.HighPerformance.LowLevel.Collections;
using Ghost.Graphics.Core;

namespace Ghost.Graphics.RHI;

public interface IResourceAllocator : IDisposable
{
    /// <summary>
    /// Creates a texture resource
    /// </summary>
    /// <param name="desc">Texture description</param>
    /// <returns>A new texture handle point to the resource</returns>
    public Handle<Texture> CreateTexture(ref readonly TextureDesc desc, bool tempResource = false);

    /// <summary>
    /// Creates a render target for off-screen rendering
    /// </summary>
    /// <param name="desc">Render target description</param>
    /// <returns>A new texture handle point to the resource</returns>
    public Handle<Texture> CreateRenderTarget(ref readonly RenderTargetDesc desc, bool tempResource = false);

    /// <summary>
    /// Creates a buffer resource
    /// </summary>
    /// <param name="desc">Buffer description</param>
    /// <returns>A new buffer handle point to the resource</returns>
    public Handle<GraphicsBuffer> CreateBuffer(ref readonly BufferDesc desc, bool tempResource = false);

    /// <summary>
    /// Creates a new mesh from the specified vertex and index data.
    /// </summary>
    /// <param name="vertices">A UnsafeList containing the vertices that define the geometry of the mesh. Must contain at least one vertex.</param>
    /// <param name="indices">A UnsafeList containing the indices that specify how vertices are connected to form primitives. Must contain at least one index.</param>
    /// <returns>An <see cref="Identifier{Mesh}"/> representing the newly created mesh.</returns>
    public Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices);

    /// <summary>
    /// Creates a new material instance using the specified shader.
    /// </summary>
    /// <param name="shader">The identifier of the shader to associate with the new material. Cannot be null.</param>
    /// <returns>An <see cref="Identifier{Material}"/> representing the newly created material.</returns>
    public Handle<Material> CreateMaterial(Identifier<Shader> shader);

    /// <summary>
    /// Creates a new shader and returns its unique identifier.
    /// </summary>
    /// <returns>An <see cref="Identifier{Shader}"/> representing the newly created shader.</returns>
    /// <param name="descriptor">The viewGroup containing the shader's properties and passes.</param>
    public Identifier<Shader> CreateGraphicsShader(ShaderDescriptor descriptor);
}
