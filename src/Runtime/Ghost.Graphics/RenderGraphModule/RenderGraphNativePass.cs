using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.RenderGraphModule;

internal struct NativeRenderPass : IDisposable
{
    public static NativeRenderPass Invalid = new NativeRenderPass { index = -1 };

    public int index;

    // Indices of logical passes merged into this native render pass.
    public UnsafeList<int> mergedPassIndices;

    // Color attachments shared across all merged passes.
    public RenderTargetInfoArray colorAttachments;
    public int colorAttachmentCount;

    // Depth-stencil attachment (optional).
    public DepthStencilInfo depthAttachment;
    public bool hasDepthAttachment;

    // Range of logical passes included in this native pass.
    public int firstLogicalPass;
    public int lastLogicalPass;

    // Whether UAV writes are allowed during this render pass.
    public bool allowUAVWrites;

    public NativeRenderPass(AllocationHandle allocationHandle)
    {
        index = -1;
        mergedPassIndices = new UnsafeList<int>(8, allocationHandle);
        firstLogicalPass = int.MaxValue;
        lastLogicalPass = -1;
    }

    public void Dispose()
    {
        mergedPassIndices.Dispose();
    }
}
