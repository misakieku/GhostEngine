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
        using var scratch = new UnsafeArray<int>(resources.ResourceCount, scope.AllocationHandle);

        // Hash pass count
        writer.Write(passes.Count);

        // Hash each pass structure (excluding names)
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];

            writer.Write(pass.type);
            writer.Write(pass.allowCulling);
            writer.Write(pass.asyncCompute);
            writer.Write(WritesExternalResource(pass, resources));

            // Hash depth attachment
            ComputeTextureHash(&writer, pass.depthAccess.id, resources);

            writer.Write(pass.depthAccess.accessFlags);
            writer.Write(pass.maxColorIndex);

            for (var j = 0; j <= pass.maxColorIndex; j++)
            {
                ComputeTextureHash(&writer, pass.colorAccess[j].id, resources);
                writer.Write(pass.colorAccess[j].accessFlags);
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
    /// Computes a hash of a texture heap's structural properties.
    /// For imported textures, hashes the backing handle.
    /// For transient textures, hashes the descriptor (respecting size mode).
    /// </summary>
    private static void ComputeTextureHash(BufferWriter* writer, Identifier<RGTexture> texture, RenderGraphResourceRegistry resources)
    {
        if (texture.IsInvalid)
        {
            return;
        }

        ref readonly var resource = ref resources.GetResource(texture.AsResource());

        // For imported textures, hash structural properties (format, dimension, usage, size) rather than dynamic handle
        writer->Write(resource.isImported);
        if (resource.isImported)
        {
            writer->Write(resource.rgTextureDesc.format);
            writer->Write(resource.rgTextureDesc.dimension);
            writer->Write(resource.rgTextureDesc.usage);
            writer->Write(resource.rgTextureDesc.width);
            writer->Write(resource.rgTextureDesc.height);
            return;
        }

        writer->Write(resource.isExtracted);
        if (resource.isExtracted)
        {
            writer->Write(resource.extractionTarget.GetHashCode());
            writer->Write((byte)resource.extractionFlags);
        }

        var desc = resource.rgTextureDesc;

        writer->Write(desc.format);
        writer->Write(desc.sizeMode);

        // Hash size specification based on mode
        if (desc.sizeMode == RGTextureSizeMode.Absolute)
        {
            // Absolute mode: hash actual dimensions
            writer->Write(desc.width);
            writer->Write(desc.height);
        }
        else
        {
            // Relative mode: hash scale factors (NOT resolved dimensions)
            writer->Write(desc.scaleX);
            writer->Write(desc.scaleY);
        }

        // Hash other structural properties
        writer->Write(desc.dimension);
        writer->Write(desc.mipLevels);
        writer->Write(desc.usage);
        writer->Write(desc.clearAtFirstUse);
        writer->Write(desc.discardAtLastUse);
    }
}
