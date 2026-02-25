namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Represents a native render pass that can contain multiple merged logical passes.
/// Maps to D3D12 BeginRenderPass/EndRenderPass or Vulkan vkCmdBeginRenderPass/vkCmdEndRenderPass.
/// </summary>
internal sealed class NativeRenderPass
{
    public int index;

    /// <summary>
    /// Indices of logical passes merged into this native render pass.
    /// </summary>
    public readonly List<int> mergedPassIndices = new(4);

    /// <summary>
    /// Color attachments shared across all merged passes.
    /// </summary>
    public RenderTargetInfo[] colorAttachments = new RenderTargetInfo[8];
    public int colorAttachmentCount;

    /// <summary>
    /// Depth-stencil attachment (optional).
    /// </summary>
    public DepthStencilInfo depthAttachment;
    public bool hasDepthAttachment;

    /// <summary>
    /// Range of logical passes included in this native pass.
    /// </summary>
    public int firstLogicalPass;
    public int lastLogicalPass;

    /// <summary>
    /// Whether UAV writes are allowed during this render pass.
    /// </summary>
    public bool allowUAVWrites;

    public void Reset()
    {
        index = -1;
        mergedPassIndices.Clear();
        colorAttachmentCount = 0;
        hasDepthAttachment = false;
        depthAttachment = default;
        firstLogicalPass = int.MaxValue;
        lastLogicalPass = -1;
        allowUAVWrites = false;
    }
}
