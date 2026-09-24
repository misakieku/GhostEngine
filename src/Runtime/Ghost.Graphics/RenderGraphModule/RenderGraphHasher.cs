using Ghost.Core;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.IO.Hashing;

namespace Ghost.Graphics.RenderGraphModule;

internal static unsafe class RenderGraphHasher
{
    private static void WriteResourceSet(BufferWriter* writer, RenderGraphResourceSet resources, Span<int> scratch)
    {
        writer->Write(resources.Count);

        var resourceCount = 0;
        foreach (var resource in resources)
        {
            scratch[resourceCount++] = resource.Value;
        }

        var resourceIds = scratch[..resourceCount];
        resourceIds.Sort();
        for (var resourceIndex = 0; resourceIndex < resourceIds.Length; resourceIndex++)
        {
            writer->Write(resourceIds[resourceIndex]);
        }
    }

    private static bool WritesExternalResource(RenderGraphPass pass, RenderGraphResourceRegistry resources)
    {
        for (var resourceType = 0; resourceType < (int)RGResourceType.Count; resourceType++)
        {
            foreach (var resourceId in pass.resourceWrites[resourceType])
            {
                ref readonly var resource = ref resources.GetResource(resourceId);
                if (resource.isImported || resource.isExtracted)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Computes a hash of the entire render graph structure.
    /// Used for cache invalidation - same hash means same compilation result.
    /// </summary>
    public static ulong ComputeGraphHash(List<RenderGraphPass> passes, RenderGraphResourceRegistry resources)
    {
        using var scope = AllocationManager.CreateStackScope();
        using var writer = new BufferWriter(2048, scope.AllocationHandle);
        using var scratch = new UnsafeArray<int>(Math.Max(1, resources.ResourceCount), scope.AllocationHandle);

        // Hash resource definitions
        writer.Write(resources.ResourceCount);
        for (var i = 0; i < resources.ResourceCount; i++)
        {
            ref readonly var resource = ref resources.Resources[i];
            ComputeResourceHash(&writer, in resource);
        }

        // Hash pass count
        writer.Write(passes.Count);

        // Hash each pass structure (excluding names)
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];

            writer.Write(pass.type);
            writer.Write(pass.allowCulling);
            writer.Write(pass.asyncCompute);
            writer.Write(pass.allowAsyncComputeOverlap);
            writer.Write(WritesExternalResource(pass, resources));

            // Hash depth attachment
            writer.Write(pass.depthAccess.id.Value);
            writer.Write(pass.depthAccess.accessFlags);
            writer.Write(pass.depthAccess.usage);

            // Hash color attachments
            writer.Write(pass.maxColorIndex);
            for (var j = 0; j <= pass.maxColorIndex; j++)
            {
                writer.Write(pass.colorAccess[j].id.Value);
                writer.Write(pass.colorAccess[j].accessFlags);
                writer.Write(pass.colorAccess[j].usage);
            }

            for (var j = 0; j < (int)RGResourceType.Count; j++)
            {
                var readList = pass.resourceReads[j];
                var writeList = pass.resourceWrites[j];
                var createList = pass.resourceCreates[j];

                WriteResourceSet(&writer, readList, scratch.AsSpan());
                WriteResourceSet(&writer, writeList, scratch.AsSpan());
                WriteResourceSet(&writer, createList, scratch.AsSpan());
            }

            WriteResourceSet(&writer, pass.randomAccess, scratch.AsSpan());
            WriteResourceSet(&writer, pass.renderTargetWrites, scratch.AsSpan());

            writer.Write(pass.GetRenderFuncHashCode());
        }

        return XxHash64.HashToUInt64(writer.AsSpan());
    }

    /// <summary>
    /// Computes a hash of a resource's structural properties.
    /// For imported resources, hashes format, dimension, usage, size rather than dynamic handles.
    /// For transient textures, hashes the descriptor respecting size mode (Absolute vs Relative).
    /// For transient buffers, hashes Size, Stride, Usage, HeapType.
    /// </summary>
    private static void ComputeResourceHash(BufferWriter* writer, scoped ref readonly RenderGraphResource resource)
    {
        writer->Write(resource.type);
        writer->Write(resource.isImported);
        writer->Write(resource.isExtracted);

        if (resource.isExtracted)
        {
            writer->Write(resource.extractionTarget.GetHashCode());
            writer->Write((byte)resource.extractionFlags);
        }

        writer->Write(resource.hasInitialBarrierState);
        if (resource.hasInitialBarrierState)
        {
            writer->Write(resource.initialBarrierState);
        }

        writer->Write(resource.hasFinalBarrierState);
        if (resource.hasFinalBarrierState)
        {
            writer->Write(resource.finalBarrierState);
        }

        if (resource.type == RGResourceType.Texture)
        {
            if (resource.isImported)
            {
                writer->Write(resource.rgTextureDesc.format);
                writer->Write(resource.rgTextureDesc.dimension);
                writer->Write(resource.rgTextureDesc.usage);
                writer->Write(resource.rgTextureDesc.width);
                writer->Write(resource.rgTextureDesc.height);
                writer->Write(resource.rgTextureDesc.mipLevels);
                writer->Write(resource.rgTextureDesc.slice);
                return;
            }

            var desc = resource.rgTextureDesc;
            writer->Write(desc.format);
            writer->Write(desc.sizeMode);

            if (desc.sizeMode == RGTextureSizeMode.Absolute)
            {
                writer->Write(desc.width);
                writer->Write(desc.height);
            }
            else
            {
                writer->Write(desc.scaleX);
                writer->Write(desc.scaleY);
            }

            writer->Write(desc.dimension);
            writer->Write(desc.mipLevels);
            writer->Write(desc.slice);
            writer->Write(desc.usage);
            writer->Write(desc.clearAtFirstUse);
            writer->Write(desc.discardAtLastUse);
            writer->Write(desc.clearColor);
            writer->Write(desc.clearDepth);
            writer->Write(desc.clearStencil);
        }
        else if (resource.type == RGResourceType.Buffer)
        {
            var desc = resource.bufferDesc;
            writer->Write(desc.Size);
            writer->Write(desc.Stride);
            writer->Write(desc.Usage);
            writer->Write(desc.HeapType);
        }
    }
}
