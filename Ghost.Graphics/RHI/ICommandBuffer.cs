using Ghost.Graphics.D3D12;
using Ghost.Graphics.Data;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Drawing;

namespace Ghost.Graphics.RHI;

/// <summary>
/// D3D12-style command buffer interface for recording rendering commands
/// </summary>
public interface ICommandBuffer : IDisposable
{
    public CommandBufferType Type
    {
        get;
    }

    /// <summary>
    /// Begins recording commands into this command buffer
    /// </summary>
    public void Begin();

    /// <summary>
    /// Ends recording commands and prepares for submission
    /// </summary>
    public void End();

    /// <summary>
    /// Begins a render pass with the specified render target
    /// </summary>
    /// <param name="renderTarget">Render target to render into</param>
    /// <param name="clearColor">Color to clear the render target with</param>
    public void BeginRenderPass(IRenderTarget renderTarget, Color128 clearColor);

    /// <summary>
    /// Ends the current render pass
    /// </summary>
    public void EndRenderPass();

    /// <summary>
    /// Sets the viewport for rendering
    /// </summary>
    /// <param name="viewport">Viewport to set</param>
    public void SetViewport(ViewportDesc viewport);

    /// <summary>
    /// Sets the scissor rectangle
    /// </summary>
    /// <param name="rect">Scissor rectangle to set</param>
    public void SetScissorRect(RectDesc rect);

    /// <summary>
    /// Inserts a resource barrier for state transitions
    /// </summary>
    /// <param name="resource">Resource to transition</param>
    /// <param name="before">Current resource state</param>
    /// <param name="after">Target resource state</param>
    public void ResourceBarrier(IResource resource, ResourceState before, ResourceState after);

    /// <summary>
    /// Sets the graphics root signature
    /// </summary>
    /// <param name="rootSignature">Root signature to set</param>
    public void SetGraphicsRootSignature(IRootSignature rootSignature);

    /// <summary>
    /// Sets the pipeline state object
    /// </summary>
    /// <param name="pipelineState">Pipeline state to set</param>
    public void SetPipelineState(IPipelineStateController pipelineState);

    /// <summary>
    /// Renders the specified mesh using the provided material.
    /// </summary>
    /// <param name="mesh">The mesh to be drawn. Must not be null.</param>
    /// <param name="material">The material to use for rendering the mesh. Must not be null.</param>
    public void DrawMesh(MeshClass mesh, MaterialClass material);

    /// <summary>
    /// Dispatches compute threads
    /// </summary>
    /// <param name="threadGroupCountX">Thread groups in X dimension</param>
    /// <param name="threadGroupCountY">Thread groups in Y dimension</param>
    /// <param name="threadGroupCountZ">Thread groups in Z dimension</param>
    public void Dispatch(uint threadGroupCountX, uint threadGroupCountY = 1, uint threadGroupCountZ = 1);

    /// <summary>
    /// Uploads the specified data to the buffer represented by the given handle.
    /// </summary>
    /// <typeparam name="T">The unmanaged value type of the elements to upload to the buffer.</typeparam>
    /// <param name="buffer">A handle to the buffer that will receive the uploaded data.</param>
    /// <param name="data">A read-only span containing the data to upload to the buffer. The span must contain elements of type
    /// <typeparamref name="T"/>.</param>
    public void Upload<T>(BufferHandle buffer, ReadOnlySpan<T> data)
        where T : unmanaged;

    /// <summary>
    /// Uploads texture data to the specified texture resource starting at the given subresource index.
    /// </summary>
    /// <param name="texture">The texture resource to which the subresource data will be uploaded. Must be a valid, initialized texture handle.</param>
    /// <param name="firstSubresource">The index of the first subresource in the texture to receive data. Must be less than the total number of subresources in the texture.</param>
    /// <param name="subresources">A reference to the structure containing the subresource data to upload. The data must match the format and layout expected by the texture.</param>
    /// <param name="numSubresources">The number of subresources to upload, starting from <paramref name="firstSubresource"/>.
    /// Must be greater than zero and not exceed the remaining subresources in the texture.</param>
    public void Upload(TextureHandle texture, uint firstSubresource, ref SubResourceData subresources, uint numSubresources);
}

/// <summary>
/// Viewport description
/// </summary>
public struct ViewportDesc
{
    public float x;
    public float y;
    public float width;
    public float height;
    public float minDepth;
    public float maxDepth;

    public ViewportDesc(float width, float height)
    {
        x = 0;
        y = 0;
        this.width = width;
        this.height = height;
        minDepth = 0.0f;
        maxDepth = 1.0f;
    }
}

/// <summary>
/// Rectangle description
/// </summary>
public struct RectDesc
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public RectDesc(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}

/// <summary>
/// Resource states
/// </summary>
[Flags]
public enum ResourceState
{
    Common = 0,
    VertexAndConstantBuffer = 1 << 0,
    IndexBuffer = 1 << 1,
    RenderTarget = 1 << 2,
    UnorderedAccess = 1 << 3,
    DepthWrite = 1 << 4,
    DepthRead = 1 << 5,
    PixelShaderResource = 1 << 6,
    CopyDest = 1 << 7,
    CopySource = 1 << 8,
    GenericRead = 1 << 9,
    IndirectArgument = 1 << 10,
    Present = 0,
}


public struct SubResourceData
{
    public unsafe void* pData;
    public nint rowPitch;
    public nint slicePitch;
}