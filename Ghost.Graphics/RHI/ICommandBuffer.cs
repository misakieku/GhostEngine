using Ghost.Core;
using Ghost.Graphics.Data;
using TerraFX.Interop.DirectX;

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

    public bool IsEmpty
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
    /// Sets the optional render targets and optional depth target for subsequent rendering operations.
    /// </summary>
    /// <remarks>
    /// To specify no render targets, provide an empty span for <paramref name="renderTargets"/>.
    /// Use <see cref="Handle{Texture}.Invalid"/> for <paramref name="depthTarget"/> if no depth target is required.
    /// </remarks>
    /// <param name="renderTargets">A read-only span of handles to textures that will be used as render targets.
    /// The order of handles determines the order in which render targets are bound.</param>
    /// <param name="depthTarget">A handle to the texture to be used as the depth target. Specify a invalid handle if no depth target is required.</param>
    public void SetRenderTargets(ReadOnlySpan<Handle<Texture>> renderTargets, Handle<Texture> depthTarget);

    /// <summary>
    /// Begins a render pass with the specified render target
    /// </summary>
    /// <param name="renderTarget">Render target to render into (can be invalid)</param>
    /// <param name="depthTarget">Depth target to use (can be invalid)</param>
    /// <param name="clearColor">Color to clear the render target with</param>
    public void BeginRenderPass(Handle<Texture> renderTarget, Handle<Texture> depthTarget, Color128 clearColor);

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
    public void ResourceBarrier(Handle<GPUResource> resource, ResourceState before, ResourceState after);

    /// <summary>
    /// Sets the graphics root signature
    /// </summary>
    /// <param name="rootSignature">Root signature to set</param>
    public void SetRootSignature(IRootSignature rootSignature);

    /// <summary>
    /// Sets the pipeline state object
    /// </summary>
    /// <param name="pipelineState">Pipeline state to set</param>
    public void SetPipelineState(IShaderPipeline pipelineState);

    public void SetVertexBuffer(uint slot, Handle<GraphicsBuffer> buffer, ulong offset = 0);
    public void SetIndexBuffer(Handle<GraphicsBuffer> buffer, IndexType type, ulong offset = 0);

    public void Draw(uint vertexCount, uint instanceCount = 1, uint startVertex = 0, uint startInstance = 0);
    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint startIndex = 0, int baseVertex = 0, uint startInstance = 0);

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
    public void Upload<T>(Handle<GraphicsBuffer> buffer, ReadOnlySpan<T> data)
        where T : unmanaged;

    /// <summary>
    /// Uploads texture data to the specified texture resource starting at the given subresource index.
    /// </summary>
    /// <param name="texture">The texture resource to which the subresource data will be uploaded. Must be a valid, initialized texture handle.</param>
    /// <param name="firstSubresource">The index of the first subresource in the texture to receive data. Must be less than the total number of subresources in the texture.</param>
    /// <param name="subresources">A reference to the structure containing the subresource data to upload. The data must match the format and layout expected by the texture.</param>
    /// <param name="numSubresources">The number of subresources to upload, starting from <paramref name="firstSubresource"/>.
    /// Must be greater than zero and not exceed the remaining subresources in the texture.</param>
    public void Upload(Handle<Texture> texture, params ReadOnlySpan<SubResourceData> subresources);

    /// <summary>
    /// Copies a specified number of bytes from the source graphics buffer to the destination graphics buffer.
    /// </summary>
    /// <param name="dest">The handle to the destination graphics buffer where data will be written. Cannot be null.</param>
    /// <param name="src">The handle to the source graphics buffer from which data will be read. Cannot be null.</param>
    /// <param name="destOffset">The byte offset in the destination buffer at which to begin writing. Must be zero or greater.</param>
    /// <param name="srcOffset">The byte offset in the source buffer at which to begin reading. Must be zero or greater.</param>
    /// <param name="numBytes">The number of bytes to copy. If zero, copies the remaining bytes from the source buffer starting at <paramref name="srcOffset"/>.</param>
    public void CopyBuffer(Handle<GraphicsBuffer> dest, Handle<GraphicsBuffer> src, ulong destOffset = 0, ulong srcOffset = 0, ulong numBytes = 0);
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
}

/// <summary>
/// Rectangle description
/// </summary>
public struct RectDesc
{
    public uint left;
    public uint top;
    public uint right;
    public uint bottom;
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

internal static class ResourceStateExtensions
{
    public static D3D12_RESOURCE_STATES ToD3D12States(this ResourceState state)
    {
        return state switch
        {
            ResourceState.Common or ResourceState.Present => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
            ResourceState.VertexAndConstantBuffer => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,
            ResourceState.IndexBuffer => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_INDEX_BUFFER,
            ResourceState.RenderTarget => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RENDER_TARGET,
            ResourceState.UnorderedAccess => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_UNORDERED_ACCESS,
            ResourceState.DepthWrite => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_DEPTH_WRITE,
            ResourceState.DepthRead => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_DEPTH_READ,
            ResourceState.PixelShaderResource => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE,
            ResourceState.CopyDest => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST,
            ResourceState.CopySource => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_SOURCE,
            _ => throw new ArgumentException($"Unknown resource state: {state}")
        };
    }
}


public struct SubResourceData
{
    public unsafe void* pData;
    public nint rowPitch;
    public nint slicePitch;
}