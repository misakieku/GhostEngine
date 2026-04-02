using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Builds native render passes by merging compatible consecutive raster passes.
/// Optimizes for tile-based deferred rendering (TBDR) GPUs by minimizing load/store operations.
/// </summary>
internal sealed class RenderGraphNativePassBuilder
{
    private readonly RenderGraphObjectPool _objectPool;
    private readonly RenderGraphResourceRegistry _resources;

    public RenderGraphNativePassBuilder(RenderGraphObjectPool objectPool, RenderGraphResourceRegistry resources)
    {
        _objectPool = objectPool;
        _resources = resources;
    }

    /// <summary>
    /// Builds native render passes by merging compatible consecutive raster passes.
    /// Uses conservative merging: only merge passes with identical attachments and no barriers between them.
    /// </summary>
    public void BuildNativeRenderPasses(
        List<RenderGraphPassBase> compiledPasses,
        List<NativeRenderPass> nativePasses,
        List<CompiledBarrier> compiledBarriers)
    {
        // Clear previous native passes
        for (var i = 0; i < nativePasses.Count; i++)
        {
            _objectPool.Return(nativePasses[i]);
        }
        nativePasses.Clear();

        NativeRenderPass? currentNativePass = null;

        for (var i = 0; i < compiledPasses.Count; i++)
        {
            var pass = compiledPasses[i];

            // Only raster passes can be merged into native render passes
            // Compute passes break the current native render pass
            if (pass.type != RenderPassType.Raster)
            {
                // Close current native pass if open
                if (currentNativePass != null)
                {
                    nativePasses.Add(currentNativePass);
                    currentNativePass = null;
                }
                continue;  // Compute/Unsafe passes execute outside native render passes
            }


            // Check if we can merge with current native pass
            if (currentNativePass != null && CanMergePasses(currentNativePass, pass, i, compiledPasses, compiledBarriers))
            {
                // Merge into existing native pass
                currentNativePass.mergedPassIndices.Add(i);
                currentNativePass.lastLogicalPass = i;
            }
            else
            {
                // Start new native pass
                if (currentNativePass != null)
                {
                    nativePasses.Add(currentNativePass);
                }

                currentNativePass = CreateNativePass(pass, i);
            }
        }

        // Add final native pass
        if (currentNativePass != null)
        {
            nativePasses.Add(currentNativePass);
        }

        // Infer load/store operations for all native passes
        for (var i = 0; i < nativePasses.Count; i++)
        {
            InferLoadStoreOps(nativePasses[i]);
        }
    }

    /// <summary>
    /// Creates a new native render pass from a logical pass.
    /// </summary>
    private NativeRenderPass CreateNativePass(RenderGraphPassBase pass, int passIndex)
    {
        var nativePass = _objectPool.Rent<NativeRenderPass>();
        nativePass.Reset();

        nativePass.index = 0; // Will be set by caller
        nativePass.mergedPassIndices.Add(passIndex);
        nativePass.firstLogicalPass = passIndex;
        nativePass.lastLogicalPass = passIndex;
        nativePass.allowUAVWrites = pass.randomAccess.Count > 0;

        // Copy color attachments
        nativePass.colorAttachmentCount = pass.maxColorIndex + 1;
        for (var i = 0; i <= pass.maxColorIndex; i++)
        {
            var access = pass.colorAccess[i];
            nativePass.colorAttachments[i] = new RenderTargetInfo
            {
                texture = access.id,
                access = access.accessFlags
            };
        }

        // Copy depth attachment
        if (!pass.depthAccess.id.IsInvalid)
        {
            nativePass.hasDepthAttachment = true;
            nativePass.depthAttachment = new DepthStencilInfo
            {
                texture = pass.depthAccess.id,
                access = pass.depthAccess.accessFlags
            };
        }

        return nativePass;
    }

    /// <summary>
    /// Checks if a logical pass can be merged into an existing native render pass.
    /// Conservative merging: only merge if attachments match and no barriers needed.
    /// </summary>
    private bool CanMergePasses(
        NativeRenderPass nativePass,
        RenderGraphPassBase pass,
        int passIndex,
        List<RenderGraphPassBase> compiledPasses,
        List<CompiledBarrier> compiledBarriers)
    {
        // Don't merge if UAVs are involved (conservative)
        if (pass.randomAccess.Count > 0 || nativePass.allowUAVWrites)
        {
            return false;
        }

        // Check if attachment configuration matches
        if (!AttachmentsMatch(nativePass, pass))
        {
            return false;
        }

        // Check if barriers are needed between last merged pass and this pass
        if (RequiresBarrierBetweenPasses(nativePass.lastLogicalPass, passIndex, compiledPasses, compiledBarriers))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the attachment configuration of a pass matches the native pass.
    /// </summary>
    private static bool AttachmentsMatch(NativeRenderPass nativePass, RenderGraphPassBase pass)
    {
        // Check color attachment count
        if (nativePass.colorAttachmentCount != pass.maxColorIndex + 1)
        {
            return false;
        }

        // Check each color attachment
        for (var i = 0; i < nativePass.colorAttachmentCount; i++)
        {
            if (nativePass.colorAttachments[i].texture != pass.colorAccess[i].id)
            {
                return false;
            }
        }

        // Check depth attachment
        if (nativePass.hasDepthAttachment != !pass.depthAccess.id.IsInvalid)
        {
            return false;
        }

        if (nativePass.hasDepthAttachment && nativePass.depthAttachment.texture != pass.depthAccess.id)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if any barriers are required between two passes that would prevent merging.
    /// Only barriers affecting render targets prevent merging; SRV barriers are fine.
    /// </summary>
    private bool RequiresBarrierBetweenPasses(
        int passA,
        int passB,
        List<RenderGraphPassBase> compiledPasses,
        List<CompiledBarrier> compiledBarriers)
    {
        var laterPass = compiledPasses[passB];

        // Build a set of render target heap IDs (color + depth)
        var renderTargets = new HashSet<Identifier<RGResource>>();
        for (var i = 0; i <= laterPass.maxColorIndex; i++)
        {
            if (!laterPass.colorAccess[i].id.IsInvalid)
            {
                renderTargets.Add(laterPass.colorAccess[i].id.AsResource());
            }
        }
        if (!laterPass.depthAccess.id.IsInvalid)
        {
            renderTargets.Add(laterPass.depthAccess.id.AsResource());
        }

        // Check if any compiled barriers for passB affect render targets
        for (var i = 0; i < compiledBarriers.Count; i++)
        {
            if (compiledBarriers[i].PassIndex == passB)
            {
                // Only prevent merge if barrier affects a render target
                if (renderTargets.Contains(compiledBarriers[i].Resource))
                {
                    return true;  // Barrier affects render target, cannot merge
                }
            }

            if (compiledBarriers[i].PassIndex > passB)
            {
                break;  // No more barriers for this pass
            }
        }

        return false;
    }

    /// <summary>
    /// Infers optimal load/store operations for all attachments in a native render pass.
    /// Uses heap lifetime information to minimize memory bandwidth (critical for TBDR GPUs).
    /// </summary>
    private void InferLoadStoreOps(NativeRenderPass nativePass)
    {
        // Infer load/store ops for color attachments
        for (var i = 0; i < nativePass.colorAttachmentCount; i++)
        {
            ref var attachment = ref nativePass.colorAttachments[i];
            var resource = _resources.GetResource(attachment.texture);
            var flags = attachment.access;

            // ===== LOAD OP INFERENCE =====

            // 1. First use
            if (resource.firstUsePass == nativePass.firstLogicalPass)
            {
                // Clear at first use
                if (resource.rgTextureDesc.clearAtFirstUse)
                {
                    attachment.loadOp = AttachmentLoadOp.Clear;
                    attachment.clearColor = resource.rgTextureDesc.clearColor;
                }
                else
                {
                    attachment.loadOp = AttachmentLoadOp.DontCare;
                }
            }
            // 2. Discard flag: DontCare for performance
            else if (flags.HasFlag(AccessFlags.Discard))
            {
                attachment.loadOp = AttachmentLoadOp.DontCare;
            }
            // 3. Read flag: Must preserve existing contents
            else if (flags.HasFlag(AccessFlags.Read))
            {
                attachment.loadOp = AttachmentLoadOp.Load;
            }
            // 4. Continuation from previous pass
            else
            {
                attachment.loadOp = AttachmentLoadOp.Load;
            }

            // ===== STORE OP INFERENCE =====

            // Last use: No one needs it after this native pass
            if (resource.lastUsePass == nativePass.lastLogicalPass)
            {
                if (resource.rgTextureDesc.discardAtLastUse)
                {
                    attachment.storeOp = AttachmentStoreOp.DontCare;
                }
                else
                {
                    attachment.storeOp = AttachmentStoreOp.Store;
                }
            }
            // Intermediate: Store for future passes
            else
            {
                attachment.storeOp = AttachmentStoreOp.Store;
            }

        }

        // Infer load/store ops for depth attachment
        if (nativePass.hasDepthAttachment)
        {
            ref var attachment = ref nativePass.depthAttachment;
            var resource = _resources.GetResource(attachment.texture);
            var flags = attachment.access;

            // ===== LOAD OP INFERENCE =====

            // 1. First Use
            if (resource.firstUsePass == nativePass.firstLogicalPass)
            {
                // Clear at first use
                if (resource.rgTextureDesc.clearAtFirstUse)
                {
                    attachment.loadOp = AttachmentLoadOp.Clear;
                    attachment.clearDepth = resource.rgTextureDesc.clearDepth;
                    attachment.clearStencil = resource.rgTextureDesc.clearStencil;
                }
                else
                {
                    attachment.loadOp = AttachmentLoadOp.DontCare;
                }
            }
            // 2. Discard flag: DontCare for performance
            else if (flags.HasFlag(AccessFlags.Discard))
            {
                attachment.loadOp = AttachmentLoadOp.DontCare;
            }
            // 3. Read flag: Must preserve existing contents
            else if (flags.HasFlag(AccessFlags.Read))
            {
                attachment.loadOp = AttachmentLoadOp.Load;
            }
            // 4. Continuation from previous pass
            else
            {
                attachment.loadOp = AttachmentLoadOp.Load;
            }

            // ===== STORE OP INFERENCE =====

            // Depth is commonly discarded (depth-only passes, intermediate depth)
            if (resource.lastUsePass == nativePass.lastLogicalPass)
            {
                if (resource.rgTextureDesc.discardAtLastUse)
                {
                    attachment.storeOp = AttachmentStoreOp.DontCare;
                }
                else
                {
                    attachment.storeOp = AttachmentStoreOp.Store;
                }
            }
            else
            {
                attachment.storeOp = AttachmentStoreOp.Store;
            }

            if (resource.rgTextureDesc.format.IsStencilFormat())
            {
                attachment.stencilLoadOp = attachment.loadOp;
                attachment.stencilStoreOp = attachment.storeOp;
            }
            else
            {
                attachment.stencilLoadOp = AttachmentLoadOp.NoAccess;
                attachment.stencilStoreOp = AttachmentStoreOp.NoAccess;
            }
        }
    }
}
