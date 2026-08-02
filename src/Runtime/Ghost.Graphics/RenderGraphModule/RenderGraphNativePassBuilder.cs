using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Builds native render passes by merging compatible consecutive raster passes.
/// Optimizes for tile-based deferred rendering (TBDR) GPUs by minimizing load/store operations.
/// </summary>
internal static class RenderGraphNativePassBuilder
{
    /// <summary>
    /// Builds native render passes by merging compatible consecutive raster passes.
    /// Uses conservative merging: only merge passes with identical attachments and no barriers between them.
    /// </summary>
    public static UnsafeList<NativeRenderPass> BuildNativeRenderPasses(
        RenderGraphResourceRegistry resourceRegistry,
        List<RenderGraphPass> passes,
        ReadOnlySpan<int> compiledPasses,
        AliasingPlan aliasingPlan,
        AllocationHandle allocationHandle)
    {
        var initialCapacity = Math.Max(1, (compiledPasses.Length + 1) / 2);
        var nativePasses = new UnsafeList<NativeRenderPass>(initialCapacity, allocationHandle);
        NativeRenderPass currentNativePass = default;

        for (var i = 0; i < compiledPasses.Length; i++)
        {
            var pass = passes[compiledPasses[i]];

            // Only raster passes can be merged into native render passes
            // Compute passes break the current native render pass
            if (pass.type != RenderPassType.Raster)
            {
                // Close current native pass if open
                if (currentNativePass.mergedPassIndices.IsCreated)
                {
                    nativePasses.Add(currentNativePass);
                    currentNativePass = default;
                }

                continue;  // Compute/Unsafe passes execute outside native render passes
            }

            // Check if we can merge with current native pass
            if (currentNativePass.mergedPassIndices.IsCreated
                && CanMergePasses(resourceRegistry, currentNativePass, pass, passes, compiledPasses, aliasingPlan))
            {
                // Merge into existing native pass
                currentNativePass.mergedPassIndices.Add(pass.index);
                currentNativePass.lastLogicalPass = pass.index;
            }
            else
            {
                // Start new native pass
                if (currentNativePass.mergedPassIndices.IsCreated)
                {
                    nativePasses.Add(currentNativePass);
                }

                currentNativePass = CreateNativePass(pass, allocationHandle);
            }
        }

        // Add final native pass
        if (currentNativePass.mergedPassIndices.IsCreated)
        {
            nativePasses.Add(currentNativePass);
        }

        // Infer load/store operations for all native passes
        for (var i = 0; i < nativePasses.Count; i++)
        {
            InferLoadStoreOps(resourceRegistry, nativePasses[i]);
        }

        return nativePasses;
    }

    private static NativeRenderPass CreateNativePass(RenderGraphPass pass, AllocationHandle allocationHandle)
    {
        var nativePass = new NativeRenderPass(allocationHandle)
        {
            index = 0, // Will be set by caller
            firstLogicalPass = pass.index,
            lastLogicalPass = pass.index,
            allowUAVWrites = pass.randomAccess.Count > 0
        };

        nativePass.mergedPassIndices.Add(pass.index);

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

    private static bool CanMergePasses(
        RenderGraphResourceRegistry resources,
        scoped in NativeRenderPass nativePass,
        RenderGraphPass pass,
        List<RenderGraphPass> passes,
        ReadOnlySpan<int> compiledPasses,
        AliasingPlan aliasingPlan)
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
        if (RequiresBarrierBetweenPasses(nativePass.lastLogicalPass, pass.index, passes, compiledPasses, resources, aliasingPlan))
        {
            return false;
        }

        return true;
    }

    private static bool AttachmentsMatch(scoped in NativeRenderPass nativePass, RenderGraphPass pass)
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

    private static bool RequiresBarrierBetweenPasses(
        int passA,
        int passB,
        List<RenderGraphPass> passes,
        ReadOnlySpan<int> compiledPasses,
        RenderGraphResourceRegistry resources,
        AliasingPlan aliasingPlan)
    {
        return RenderGraphCompiler.RequiresBarrierBetweenPasses(passA, passB, passes, compiledPasses, resources, aliasingPlan);
    }

    private static void InferLoadStoreOps(RenderGraphResourceRegistry resourceRegistry, NativeRenderPass nativePass)
    {
        // Infer load/store ops for color attachments
        for (var i = 0; i < nativePass.colorAttachmentCount; i++)
        {
            ref var attachment = ref nativePass.colorAttachments[i];
            var resource = resourceRegistry.GetResource(attachment.texture);
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
            var resource = resourceRegistry.GetResource(attachment.texture);
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
