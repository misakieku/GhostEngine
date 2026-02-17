using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.IO.Hashing;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Computes structural hashes of render graphs for compilation caching.
/// Hashes are based on graph topology and resource configurations, not runtime values.
/// </summary>
internal static class RenderGraphHasher
{
    /// <summary>
    /// Computes a hash of the entire render graph structure.
    /// Used for cache invalidation - same hash means same compilation result.
    /// </summary>
    public static unsafe ulong ComputeGraphHash(List<RenderGraphPassBase> passes, RenderGraphResourceRegistry resources)
    {
        using var scope = AllocationManager.CreateStackScope();
        var bufferPool = new UnsafeList<byte>(2048, scope.AllocationHandle);
        var pData = (byte*)bufferPool.GetUnsafePtr();
        var offset = 0;

        // Hash pass count
        *(int*)(pData + offset) = passes.Count;
        offset += sizeof(int);

        // Hash each pass structure (excluding names)
        for (var i = 0; i < passes.Count; i++)
        {
            var pass = passes[i];

            *(RenderPassType*)(pData + offset) = pass.type;
            offset += sizeof(RenderPassType);

            *(bool*)(pData + offset) = pass.allowCulling;
            offset += sizeof(bool);

            *(bool*)(pData + offset) = pass.asyncCompute;
            offset += sizeof(bool);

            // Hash depth attachment
            offset = ComputeTextureHash(pData, offset, pass.depthAccess.id, resources);

            pData[offset] = (byte)pass.depthAccess.accessFlags;
            offset += sizeof(AccessFlags);

            *(int*)(pData + offset) = pass.maxColorIndex;
            offset += sizeof(int);
            for (var j = 0; j <= pass.maxColorIndex; j++)
            {
                offset = ComputeTextureHash(pData, offset, pass.colorAccess[j].id, resources);

                pData[offset] = (byte)pass.colorAccess[j].accessFlags;
                offset += sizeof(AccessFlags);
            }

            for (var j = 0; j < (int)RenderGraphResourceType.Count; j++)
            {
                var readList = pass.resourceReads[j];
                var writeList = pass.resourceWrites[j];
                var createList = pass.resourceCreates[j];

                *(int*)(pData + offset) = readList.Count;
                offset += sizeof(int);
                for (var k = 0; k < readList.Count; k++)
                {
                    *(int*)(pData + offset) = readList[k].Value;
                    offset += sizeof(int);
                }

                *(int*)(pData + offset) = writeList.Count;
                offset += sizeof(int);
                for (var k = 0; k < writeList.Count; k++)
                {
                    *(int*)(pData + offset) = writeList[k].Value;
                    offset += sizeof(int);
                }

                *(int*)(pData + offset) = createList.Count;
                offset += sizeof(int);
                for (var k = 0; k < createList.Count; k++)
                {
                    *(int*)(pData + offset) = createList[k].Value;
                    offset += sizeof(int);
                }

                *(int*)(pData + offset) = pass.randomAccess.Count;
                offset += sizeof(int);
                for (var k = 0; k < pass.randomAccess.Count; k++)
                {
                    *(int*)(pData + offset) = pass.randomAccess[k].Value;
                    offset += sizeof(int);
                }
            }

            *(int*)(pData + offset) = pass.GetRenderFuncHashCode();
            offset += sizeof(int);
        }

        var span = new Span<byte>(pData, offset);
        return XxHash64.HashToUInt64(span);
    }

    /// <summary>
    /// Computes a hash of a texture resource's structural properties.
    /// For imported textures, hashes the backing handle.
    /// For transient textures, hashes the descriptor (respecting size mode).
    /// </summary>
    private static unsafe int ComputeTextureHash(byte* pData, int offset, Identifier<RGTexture> texture, RenderGraphResourceRegistry resources)
    {
        if (texture.IsInvalid)
        {
            return offset;
        }

        var resource = resources.GetResource(texture.AsResource());

        // Hash imported flag
        *(pData + offset) = resource.isImported ? (byte)1 : (byte)0;
        offset += sizeof(byte);

        // For imported textures, hash the backing resource handle
        if (resource.isImported)
        {
            *(int*)(pData + offset) = resource.backingResource.GetHashCode();
            offset += sizeof(int);
            return offset;
        }

        var desc = resource.rgTextureDesc;

        // Hash format (structural)
        *(TextureFormat*)(pData + offset) = desc.format;
        offset += sizeof(TextureFormat);

        // Hash size mode (structural)
        *(RGTextureSizeMode*)(pData + offset) = desc.sizeMode;
        offset += sizeof(RGTextureSizeMode);

        // Hash size specification based on mode
        if (desc.sizeMode == RGTextureSizeMode.Absolute)
        {
            // Absolute mode: hash actual dimensions
            *(uint*)(pData + offset) = desc.width;
            offset += sizeof(uint);
            *(uint*)(pData + offset) = desc.height;
            offset += sizeof(uint);
        }
        else
        {
            // Relative mode: hash scale factors (NOT resolved dimensions)
            *(float*)(pData + offset) = desc.scaleX;
            offset += sizeof(float);
            *(float*)(pData + offset) = desc.scaleY;
            offset += sizeof(float);
        }

        // Hash other structural properties
        *(TextureDimension*)(pData + offset) = desc.dimension;
        offset += sizeof(TextureDimension);
        *(uint*)(pData + offset) = desc.mipLevels;
        offset += sizeof(uint);
        *(TextureUsage*)(pData + offset) = desc.usage;
        offset += sizeof(TextureUsage);

        *(bool*)(pData + offset) = desc.clearAtFirstUse;
        offset += sizeof(bool);
        *(bool*)(pData + offset) = desc.discardAtLastUse;
        offset += sizeof(bool);

        return offset;
    }
}
