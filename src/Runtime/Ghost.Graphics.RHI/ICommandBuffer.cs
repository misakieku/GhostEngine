using Ghost.Core;
using Ghost.Core.Graphics;

namespace Ghost.Graphics.RHI;

/// <summary>
/// D3D12-style command buffer interface for recording rendering commands
/// </summary>
public interface ICommandBuffer : IRHIObject
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
    void SetScissorRect(ScissorRectDesc rect);

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
    void SetRenderTargets(ReadOnlySpan<Handle<GPUTexture>> renderTargets, Handle<GPUTexture> depthTarget);

    /// <summary>
    /// Clears the specified render target to a given color.
    /// </summary>
    /// <param name="renderTarget">A handle to the render target texture to be cleared. Must reference a valid render target.</param>
    /// <param name="clearColor">The color value used to clear the render target. Specifies the RGBA components to fill the target.</param>
    void ClearRenderTargetView(Handle<GPUTexture> renderTarget, Color128 clearColor);

    /// <summary>
    /// Clears the specified depth-stencil view by resetting its depth and/or stencil values.
    /// </summary>
    /// <param name="depthStencil">A handle to the depth-stencil texture to be cleared. Must reference a valid depth-stencil resource.</param>
    /// <param name="inlcludeDepth">A value indicating whether the depth component should be cleared.</param>
    /// <param name="includeStencil">A value indicating whether the stencil component should be cleared.</param>
    /// <param name="clearDepth">The value to which the depth buffer will be set. Typically ranges from 0.0f (nearest) to 1.0f (farthest).</param>
    /// <param name="clearStencil">The value to which the stencil buffer will be set. Must be a valid stencil value supported by the format.</param>
    void ClearDepthStencilView(Handle<GPUTexture> depthStencil, bool inlcludeDepth, bool includeStencil, float clearDepth = 1.0f, byte clearStencil = 0);

    /// <summary>
    /// Begins a render pass with the specified render Target
    /// </summary>
    /// <param name="rtDescs">Render Target descriptions</param>
    /// <param name="depthDesc">Depth stencil description</param>
    /// <param name="allowUAVWrites">Whether UAV writes are allowed during the render pass</param>
    void BeginRenderPass(ReadOnlySpan<PassRenderTargetDesc> rtDescs, ref readonly PassDepthStencilDesc depthDesc, bool allowUAVWrites = false);

    /// <summary>
    /// Ends the current render pass
    /// </summary>
    void EndRenderPass();

    /// <summary>
    /// Inserts multiple resource barriers.
    /// </summary>
    /// <param name="barrierDescs">Resource barrier descriptions</param>
    void Barrier(params ReadOnlySpan<BarrierDesc> barrierDescs);

    /// <summary>
    /// Sets the pipeline state object
    /// </summary>
    /// <param name="pipelineKey">Pipeline state to set</param>
    void SetPipelineState(Key128<PipelineState> pipelineKey);

    /// <summary>
    /// Sets the constant buffer view for the specified slot in the graphics pipeline.
    /// </summary>
    /// <param name="slot">The zero-based index of the slot to bind the constant buffer view to.</param>
    /// <param name="buffer">A graphics buffer to use as the constant buffer view.</param>
    void SetConstantBufferView(uint slot, Handle<GPUBuffer> buffer);

    /// <summary>
    /// Binds a vertex buffer to the specified slot for subsequent draw calls.
    /// </summary>
    /// <param name="slot">The vertex buffer slot to bind to.</param>
    /// <param name="buffer">The handle to the graphics buffer containing vertex data.</param>
    /// <param name="offset">The Offset in bytes from the start of the buffer.</param>
    void SetVertexBuffer(uint slot, Handle<GPUBuffer> buffer, ulong offset = 0);

    /// <summary>
    /// Binds an index buffer for indexed drawing.
    /// </summary>
    /// <param name="buffer">The handle to the graphics buffer containing index data.</param>
    /// <param name="type">The space of indices (e.g., 16-bit or 32-bit).</param>
    /// <param name="offset">The Offset in bytes from the start of the buffer.</param>
    void SetIndexBuffer(Handle<GPUBuffer> buffer, IndexType type, ulong offset = 0);

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

    void SetProgram(scoped in SetProgramDesc desc);

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
    void DispatchRay();

    void DispatchGraph(scoped in DispatchGraphDesc desc);

    /// <summary>
    /// Executes a sequence of GPU commands indirectly using the specified command signature and argument buffers.
    /// </summary>
    /// <param name="commandSignature">The command signature that defines the layout and type of commands to execute.</param>
    /// <param name="argumentBuffer">A handle to the GPU buffer containing the arguments for each command.</param>
    /// <param name="argumentOffset">The byte offset within the argument buffer at which to begin reading command arguments.</param>
    /// <param name="countBuffer">A handle to the GPU buffer that specifies the number of commands to execute.</param>
    /// <param name="countBufferOffset">The byte offset within the count buffer at which to read the command count.</param>
    void ExecuteIndirect(ICommandSignature commandSignature, Handle<GPUBuffer> argumentBuffer, ulong argumentOffset, Handle<GPUBuffer> countBuffer, ulong countBufferOffset);

    /// <summary>
    /// Copies a specified number of bytes from the source graphics buffer to the destination graphics buffer.
    /// </summary>
    /// <param name="dest">The handle to the destination graphics buffer where data will be written.</param>
    /// <param name="src">The handle to the source graphics buffer from which data will be read.</param>
    /// <param name="destOffset">The byte Offset in the destination buffer at which to begin writing. Must be zero or greater.</param>
    /// <param name="srcOffset">The byte Offset in the source buffer at which to begin reading. Must be zero or greater.</param>
    /// <param name="numBytes">The number of bytes to copy. If zero, copies the remaining bytes from the source buffer starting at <paramref name="srcOffset"/>.</param>
    void CopyBuffer(Handle<GPUBuffer> dest, Handle<GPUBuffer> src, ulong destOffset = 0, ulong srcOffset = 0, ulong numBytes = 0);

    /// <summary>
    /// Copies a region of a source texture to a destination texture. The source and destination regions can be specified to copy a subset of the textures, or the entire textures if the regions are null.
    /// </summary>
    /// <param name="dst">The handle to the destination texture where data will be written.</param>
    /// <param name="dstRegion">The region of the destination texture to copy to. If null, the entire texture will be used.</param>
    /// <param name="src">The handle to the source texture from which data will be read.</param>
    /// <param name="srcRegion">The region of the source texture to copy from. If null, the entire texture will be used.</param>
    void CopyTexture(Handle<GPUTexture> dst, TextureRegion? dstRegion, Handle<GPUTexture> src, TextureRegion? srcRegion);

    /// <summary>
    /// Updates the subresources of a GPU resource using data from the specified intermediate resource and subresource data spans.
    /// </summary>
    /// <param name="resource">A handle to the destination GPU resource whose subresources will be updated.</param>
    /// <param name="intermediate">A handle to an intermediate GPU resource used to stage the subresource data before copying to the destination resource.</param>
    /// <param name="subResources">A span containing the data for each subresource to update. Each element represents a subresource and its associated data.</param>
    void UpdateSubResources(Handle<GPUResource> resource, Handle<GPUResource> intermediate, params ReadOnlySpan<SubResourceData> subResources);
}
