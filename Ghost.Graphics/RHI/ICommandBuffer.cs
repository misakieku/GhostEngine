using Ghost.Graphics.Data;

namespace Ghost.Graphics.RHI;

/// <summary>
/// D3D12-style command buffer interface for recording rendering commands
/// </summary>
public interface ICommandBuffer : IDisposable
{
    /// <summary>
    /// Begins recording commands into this command buffer
    /// </summary>
    void Begin();

    /// <summary>
    /// Ends recording commands and prepares for submission
    /// </summary>
    void End();

    /// <summary>
    /// Begins a render pass with the specified render target
    /// </summary>
    /// <param name="renderTarget">Render target to render into</param>
    /// <param name="clearColor">Color to clear the render target with</param>
    void BeginRenderPass(IRenderTarget renderTarget, Color128 clearColor);

    /// <summary>
    /// Ends the current render pass
    /// </summary>
    void EndRenderPass();

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
    /// Inserts a resource barrier for state transitions
    /// </summary>
    /// <param name="resource">Resource to transition</param>
    /// <param name="before">Current resource state</param>
    /// <param name="after">Target resource state</param>
    void ResourceBarrier(IResource resource, ResourceState before, ResourceState after);

    /// <summary>
    /// Sets the graphics root signature
    /// </summary>
    /// <param name="rootSignature">Root signature to set</param>
    void SetGraphicsRootSignature(IRootSignature rootSignature);

    /// <summary>
    /// Sets the pipeline state object
    /// </summary>
    /// <param name="pipelineState">Pipeline state to set</param>
    void SetPipelineState(IPipelineState pipelineState);

    /// <summary>
    /// Sets descriptor heaps for bindless rendering
    /// </summary>
    /// <param name="heaps">Descriptor heaps to set</param>
    void SetDescriptorHeaps(IDescriptorHeap[] heaps);

    /// <summary>
    /// Draws indexed geometry
    /// </summary>
    /// <param name="indexCount">Number of indices to draw</param>
    /// <param name="instanceCount">Number of instances to draw</param>
    /// <param name="startIndex">Starting index location</param>
    /// <param name="baseVertex">Base vertex location</param>
    /// <param name="startInstance">Starting instance location</param>
    void DrawIndexedInstanced(uint indexCount, uint instanceCount = 1, uint startIndex = 0, int baseVertex = 0, uint startInstance = 0);

    /// <summary>
    /// Dispatches compute threads
    /// </summary>
    /// <param name="threadGroupCountX">Thread groups in X dimension</param>
    /// <param name="threadGroupCountY">Thread groups in Y dimension</param>
    /// <param name="threadGroupCountZ">Thread groups in Z dimension</param>
    void Dispatch(uint threadGroupCountX, uint threadGroupCountY = 1, uint threadGroupCountZ = 1);
}

/// <summary>
/// Viewport description
/// </summary>
public struct ViewportDesc
{
    public float X;
    public float Y;
    public float Width;
    public float Height;
    public float MinDepth;
    public float MaxDepth;

    public ViewportDesc(float width, float height)
    {
        X = 0;
        Y = 0;
        Width = width;
        Height = height;
        MinDepth = 0.0f;
        MaxDepth = 1.0f;
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
/// D3D12-style resource states
/// </summary>
public enum ResourceState
{
    Common = 0,
    VertexAndConstantBuffer = 0x1,
    IndexBuffer = 0x2,
    RenderTarget = 0x4,
    UnorderedAccess = 0x8,
    DepthWrite = 0x10,
    DepthRead = 0x20,
    PixelShaderResource = 0x80,
    CopyDest = 0x400,
    CopySource = 0x800,
    Present = 0
}
