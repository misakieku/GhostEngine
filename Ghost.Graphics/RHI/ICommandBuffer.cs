using Ghost.Core;
using Ghost.Graphics.Core;

namespace Ghost.Graphics.RHI;

// TODO: Add ICommandAllocator support for thread local command buffers. We often use one allocator for multiple command buffers in a single frame.

/// <summary>
/// D3D12-style command buffer interface for recording rendering commands
/// </summary>
public interface ICommandBuffer : IDisposable
{
    /// <summary>
    /// Gets the space of the command buffer.
    /// </summary>
    CommandBufferType Type
    {
        get;
    }

    /// <summary>
    /// Indicates whether the command buffer contains any recorded commands.
    /// </summary>
    bool IsEmpty
    {
        get;
    }

    string Name
    {
        get;
        set;
    }

    /// <summary>
    /// Begins recording commands into this command buffer
    /// </summary>
    void Begin(ICommandAllocator allocator);

    /// <summary>
    /// Ends recording commands and prepares for submission
    /// </summary>
    Result End();

    /// <summary>
    /// Sets the viewport for rendering
    /// </summary>
    /// <param name="viewport">Viewport to set</param>
    void SetViewport(ViewportDesc viewport);

    /// <summary>
    /// Sets the scissor rectangle
    /// </summary>
    /// <param name="rect">Scissor rectangle to set</param>
    void SetScissorRect(RectDesc rect);

    /// <summary>
    /// Sets the optional render targets and optional depth Target for subsequent rendering operations.
    /// </summary>
    /// <remarks>
    /// To specify no render targets, provide an empty span for <paramref name="renderTargets"/>.
    /// Use <see cref="Handle{Texture}.Invalid"/> for <paramref name="depthTarget"/> if no depth Target is required.
    /// </remarks>
    /// <param name="renderTargets">A read-only span of handles to textures that will be used as render targets.
    /// The order of handles determines the order in which render targets are bound.</param>
    /// <param name="depthTarget">A handle to the texture to be used as the depth Target. Specify a invalid handle if no depth Target is required.</param>
    void SetRenderTargets(ReadOnlySpan<Handle<Texture>> renderTargets, Handle<Texture> depthTarget);

    void ClearRenderTargetView(Handle<Texture> renderTarget, Color128 clearColor);

    void ClearDepthStencilView(Handle<Texture> depthStencil, bool inlcudeDepth, bool includeStencil, float clearDepth = 1.0f, byte clearStencil = 0);

    /// <summary>
    /// Begins a render pass with the specified render Target
    /// </summary>
    /// <param name="rtDescs">Render Target descriptions</param>
    /// <param name="depthDesc">Depth stencil description</param>
    /// <param name="allowUAVWrites">Whether UAV writes are allowed during the render pass</param>
    void BeginRenderPass(ReadOnlySpan<PassRenderTargetDesc> rtDescs, PassDepthStencilDesc depthDesc, bool allowUAVWrites = false);

    /// <summary>
    /// Ends the current render pass
    /// </summary>
    void EndRenderPass();

    // TODO: Enhanced barriers.

    /// <summary>
    /// Inserts multiple resource barriers.
    /// </summary>
    /// <param name="barrierDescs">Resource barrier descriptions</param>
    void ResourceBarrier(ReadOnlySpan<BarrierDesc> barrierDescs);

    /// <summary>
    /// Inserts a resource barrier for state transitions.
    /// </summary>
    /// <param name="resource">A handle to the GPU resource to transition.</param>
    /// <param name="stateBefore">The current state of the resource before the transition.</param>
    /// <param name="stateAfter">The desired state of the resource after the transition.</param>
    void TransitionBarrier(Handle<GPUResource> resource, ResourceState stateBefore, ResourceState stateAfter);

    /// <summary>
    /// Inserts a resource barrier for state transitions. The current state is tracked internally.
    /// </summary>
    /// <param name="resource">A handle to the GPU resource to transition.</param>
    /// <param name="stateAfter">The desired state of the resource after the transition.</param>
    void TransitionBarrier(Handle<GPUResource> resource, ResourceState stateAfter);

    /// <summary>
    /// Inserts a barrier to ensure correct aliasing transitions between two GPU resources.
    /// </summary>
    /// <param name="resourceBefore">A handle to the GPU resource representing the state before the aliasing transition</param>
    /// <param name="resourceAfter">A handle to the GPU resource representing the state after the aliasing transition</param>
    void AliasBarrier(Handle<GPUResource> resourceBefore, Handle<GPUResource> resourceAfter);

    /// <summary>
    /// Sets the pipeline state object
    /// </summary>
    /// <param name="pipelineKey">Pipeline state to set</param>
    void SetPipelineState(Key128<GraphicsPipeline> pipelineKey);

    /// <summary>
    /// Sets the constant buffer view for the specified slot in the graphics pipeline.
    /// </summary>
    /// <param name="slot">The zero-based index of the slot to bind the constant buffer view to.</param>
    /// <param name="buffer">A graphics buffer to use as the constant buffer view.</param>
    void SetConstantBufferView(uint slot, Handle<GraphicsBuffer> buffer);

    /// <summary>
    /// Binds a vertex buffer to the specified slot for subsequent draw calls.
    /// </summary>
    /// <param name="slot">The vertex buffer slot to bind to.</param>
    /// <param name="buffer">The handle to the graphics buffer containing vertex data.</param>
    /// <param name="offset">The Offset in bytes from the start of the buffer.</param>
    void SetVertexBuffer(uint slot, Handle<GraphicsBuffer> buffer, ulong offset = 0);

    /// <summary>
    /// Binds an index buffer for indexed drawing.
    /// </summary>
    /// <param name="buffer">The handle to the graphics buffer containing index data.</param>
    /// <param name="type">The space of indices (e.g., 16-bit or 32-bit).</param>
    /// <param name="offset">The Offset in bytes from the start of the buffer.</param>
    void SetIndexBuffer(Handle<GraphicsBuffer> buffer, IndexType type, ulong offset = 0);

    /// <summary>
    /// Sets the primitive topology to be used for subsequent drawing operations.
    /// </summary>
    /// <param name="topology">The primitive topology that determines how the input vertices are interpreted during rendering.</param>
    void SetPrimitiveTopology(PrimitiveTopology topology);

    /// <summary>
    /// Sets a 32-bit constant value in the graphics root signature at the specified index.
    /// </summary>
    /// <param name="rootIndex">The zero-based index of the root parameter in the graphics root signature to set the constant for.</param>
    /// <param name="constantBuffer">A read-only span containing the 32-bit constant values to set.</param>
    /// <param name="offsetIn32Bits">The Offset, in 32-bit values, from the start of the root parameter where the constants will be set.</param>
    void SetGraphicsRoot32Constants(uint rootIndex, ReadOnlySpan<uint> constantBuffer, uint offsetIn32Bits = 0);

    /// <summary>
    /// Issues a non-indexed draw call.
    /// </summary>
    /// <param name="vertexCount">Number of vertices to draw.</param>
    /// <param name="instanceCount">Number of instances to draw.</param>
    /// <param name="startVertex">Index of the first vertex to draw.</param>
    /// <param name="startInstance">Index of the first instance to draw.</param>
    void Draw(uint vertexCount, uint instanceCount = 1, uint startVertex = 0, uint startInstance = 0);

    /// <summary>
    /// Issues an indexed draw call.
    /// </summary>
    /// <param name="indexCount">Number of indices to draw.</param>
    /// <param name="instanceCount">Number of instances to draw.</param>
    /// <param name="startIndex">Index of the first index to draw.</param>
    /// <param name="baseVertex">Value added to each index before indexing the vertex buffer.</param>
    /// <param name="startInstance">Index of the first instance to draw.</param>
    void DrawIndexed(uint indexCount, uint instanceCount = 1, uint startIndex = 0, int baseVertex = 0, uint startInstance = 0);

    /// <summary>
    /// Dispatches compute threads
    /// </summary>
    /// <param name="threadGroupCountX">Thread groups in X dimension</param>
    /// <param name="threadGroupCountY">Thread groups in Y dimension</param>
    /// <param name="threadGroupCountZ">Thread groups in Z dimension</param>
    void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ);

    /// <summary>
    /// Dispatches mesh shader threads
    /// </summary>
    /// <param name="threadGroupCountX">Thread groups in X dimension</param>
    /// <param name="threadGroupCountY">Thread groups in Y dimension</param>
    /// <param name="threadGroupCountZ">Thread groups in Z dimension</param>
    void DispatchMesh(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ);

    /// <summary>
    /// Dispatches ray tracing threads
    /// </summary>
    // TODO: This method is not supported yet.
    void DispatchRay();

    /// <summary>
    /// Uploads the specified data to the buffer represented by the given handle.
    /// </summary>
    /// <typeparam name="T">The unmanaged Value space of the elements to upload to the buffer.</typeparam>
    /// <param name="buffer">A handle to the buffer that will receive the uploaded data.</param>
    /// <param name="data">A read-only span containing the data to upload to the buffer. The span must contain elements of space
    /// <typeparamref name="T"/>.</param>
    void UploadBuffer<T>(Handle<GraphicsBuffer> buffer, ReadOnlySpan<T> data)
        where T : unmanaged;

    /// <summary>
    /// Uploads texture data to the specified texture resource starting at the given subresource index.
    /// </summary>
    /// <param name="texture">The texture resource to which the subresource data will be uploaded. Must be a valid, initialized texture handle.</param>
    /// <param name="firstSubresource">The index of the first subresource in the texture to receive data. Must be less than the total number of subresources in the texture.</param>
    /// <param name="subresources">A reference to the structure containing the subresource data to upload. The data must match the Format and layout expected by the texture.</param>
    /// <param name="numSubresources">The number of subresources to upload, starting from <paramref name="firstSubresource"/>.
    /// Must be greater than zero and not exceed the remaining subresources in the texture.</param>
    void UploadTexture(Handle<Texture> texture, ReadOnlySpan<SubResourceData> subresources);

    /// <summary>
    /// Copies a specified number of bytes from the source graphics buffer to the destination graphics buffer.
    /// </summary>
    /// <param name="dest">The handle to the destination graphics buffer where data will be written.</param>
    /// <param name="src">The handle to the source graphics buffer from which data will be read.</param>
    /// <param name="destOffset">The byte Offset in the destination buffer at which to begin writing. Must be zero or greater.</param>
    /// <param name="srcOffset">The byte Offset in the source buffer at which to begin reading. Must be zero or greater.</param>
    /// <param name="numBytes">The number of bytes to copy. If zero, copies the remaining bytes from the source buffer starting at <paramref name="srcOffset"/>.</param>
    void CopyBuffer(Handle<GraphicsBuffer> dest, Handle<GraphicsBuffer> src, ulong destOffset = 0, ulong srcOffset = 0, ulong numBytes = 0);
}
