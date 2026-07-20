using Ghost.Core;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.IO.Hashing;

namespace Ghost.Graphics.RenderGraphModule;

internal static unsafe class RenderGraphHasher
{
    /// <summary>
    /// Computes a hash of the entire render graph structure.
    /// Used for cache invalidation - same hash means same compilation result.
    /// </summary>
    public static ulong ComputeGraphHash(List<RenderGraphPass> passes, RenderGraphResourceRegistry resources)
    {
        using var scope = AllocationManager.CreateStackScope();
        using var writer = new BufferWriter(2048, scope.AllocationHandle);

        // Hash pass count
        writer.Write(passes.Count);

        // Hash each pass structure (excluding names)
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];

            writer.Write(pass.type);
            writer.Write(pass.allowCulling);
            writer.Write(pass.asyncCompute);

            // Hash depth attachment
            ComputeTextureHash(&writer, pass.depthAccess.id, resources);

            writer.Write(pass.depthAccess.accessFlags);
            writer.Write(pass.maxColorIndex);

            for (var j = 0; j <= pass.maxColorIndex; j++)
            {
                ComputeTextureHash(&writer, pass.colorAccess[j].id, resources);
                writer.Write(pass.colorAccess[j].accessFlags);
            }

            for (var j = 0; j < (int)RenderGraphResourceType.Count; j++)
            {
                var readList = pass.resourceReads[j];
                var writeList = pass.resourceWrites[j];
                var createList = pass.resourceCreates[j];

                writer.Write(readList.Count);
                for (var k = 0; k < readList.Count; k++)
                {
                    writer.Write(readList[k].Value);
                }

                writer.Write(writeList.Count);
                for (var k = 0; k < writeList.Count; k++)
                {
                    writer.Write(writeList[k].Value);
                }

                writer.Write(createList.Count);
                for (var k = 0; k < createList.Count; k++)
                {
                    writer.Write(createList[k].Value);
                }

                writer.Write(pass.randomAccess.Count);
                for (var k = 0; k < pass.randomAccess.Count; k++)
                {
                    writer.Write(pass.randomAccess[k].Value);
                }
            }

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

        // Hash imported flag
        writer->Write(resource.isImported);

        // For imported textures, hash the backing heap handle
        if (resource.isImported)
        {
            writer->Write(resource.backingResource.GetHashCode());
            return;
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
