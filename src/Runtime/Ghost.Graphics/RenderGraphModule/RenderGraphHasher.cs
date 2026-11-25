using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RenderGraphModule;

internal static unsafe class RenderGraphHasher
{
    private const int STACK_BUFFER_SIZE = 4096;

    [ThreadStatic]
    private static XxHash64? t_hasher;

    private ref struct StackHashWriter
    {
        private readonly XxHash64 _hasher;
        private readonly Span<byte> _buffer;
        private int _position;

        public StackHashWriter(XxHash64 hasher, Span<byte> buffer)
        {
            _hasher = hasher;
            _buffer = buffer;
            _position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T value) where T : unmanaged
        {
            var size = sizeof(T);
            if (_position + size > _buffer.Length)
            {
                Flush();
            }

            Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(_buffer), _position), value);
            _position += size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flush()
        {
            if (_position > 0)
            {
                _hasher.Append(_buffer.Slice(0, _position));
                _position = 0;
            }
        }
    }

    private static void WriteResourceSet(ref StackHashWriter writer, RenderGraphResourceSet resources)
    {
        var count = resources.Count;
        writer.Write(count);
        if (count == 0)
        {
            return;
        }

        if (count == 1)
        {
            foreach (var resource in resources)
            {
                writer.Write(resource.Value);
            }
            return;
        }

        if (count <= 16)
        {
            Span<int> stackIds = stackalloc int[16];
            var i = 0;
            foreach (var resource in resources)
            {
                stackIds[i++] = resource.Value;
            }

            var slice = stackIds[..count];
            slice.Sort();
            for (var resourceIndex = 0; resourceIndex < count; resourceIndex++)
            {
                writer.Write(slice[resourceIndex]);
            }
            return;
        }

        using var scope = AllocationManager.CreateStackScope();
        using var heapIds = new UnsafeArray<int>(count, scope.AllocationHandle);
        var idx = 0;
        foreach (var resource in resources)
        {
            heapIds[idx++] = resource.Value;
        }

        var span = heapIds.AsSpan();
        span.Sort();
        for (var resourceIndex = 0; resourceIndex < count; resourceIndex++)
        {
            writer.Write(span[resourceIndex]);
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
    public static ulong ComputeGraphHash(List<RenderGraphPass> passes, RenderGraphResourceRegistry resources, RGFlags flags)
    {
        var hasher = t_hasher ??= new XxHash64();
        hasher.Reset();

        Span<byte> stackBuffer = stackalloc byte[STACK_BUFFER_SIZE];
        var writer = new StackHashWriter(hasher, stackBuffer);

        writer.Write((int)flags);

        // Hash resource definitions
        writer.Write(resources.ResourceCount);
        for (var i = 0; i < resources.ResourceCount; i++)
        {
            ref readonly var resource = ref resources.Resources[i];
            ComputeResourceHash(ref writer, in resource);
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
                WriteResourceSet(ref writer, pass.resourceReads[j]);
                WriteResourceSet(ref writer, pass.resourceWrites[j]);
                WriteResourceSet(ref writer, pass.resourceCreates[j]);
            }

            WriteResourceSet(ref writer, pass.randomAccess);
            WriteResourceSet(ref writer, pass.renderTargetWrites);
            WriteResourceSet(ref writer, pass.depthTargetWrites);
            WriteResourceSet(ref writer, pass.depthTargetReads);

            writer.Write(pass.GetRenderFuncHashCode());
        }

        writer.Flush();
        return hasher.GetCurrentHashAsUInt64();
    }

    /// <summary>
    /// Computes a hash of a resource's structural properties.
    /// For imported resources, hashes format, dimension, usage, size rather than dynamic handles.
    /// For transient textures, hashes the descriptor respecting size mode (Absolute vs Relative).
    /// For transient buffers, hashes Size, Stride, Usage, HeapType.
    /// </summary>
    private static void ComputeResourceHash(ref StackHashWriter writer, scoped ref readonly RenderGraphResource resource)
    {
        writer.Write(resource.type);
        writer.Write(resource.isImported);
        writer.Write(resource.isAllocated);
        writer.Write(resource.isExtracted);

        if (resource.isExtracted)
        {
            writer.Write(resource.extractionTarget.GetHashCode());
            writer.Write((byte)resource.extractionFlags);
        }

        writer.Write(resource.hasInitialBarrierState);
        if (resource.hasInitialBarrierState)
        {
            writer.Write(resource.initialBarrierState);
        }

        writer.Write(resource.hasFinalBarrierState);
        if (resource.hasFinalBarrierState)
        {
            writer.Write(resource.finalBarrierState);
        }

        if (resource.type == RGResourceType.Texture)
        {
            var desc = resource.rgTextureDesc;
            writer.Write(desc.format);

            // Hash the resolved size for aliasable resources. Non-aliasable resources never break the graph topology no matter what their size is, so we don't need to hash it.
            if (resource.CanBeAliased)
            {
                writer.Write(desc.sizeMode);
                if (desc.sizeMode == RGTextureSizeMode.Absolute)
                {
                    writer.Write(desc.width);
                    writer.Write(desc.height);
                }
                else
                {
                    writer.Write(desc.scaleX);
                    writer.Write(desc.scaleY);
                }
            }

            writer.Write(desc.dimension);
            writer.Write(desc.mipLevels);
            writer.Write(desc.slice);
            writer.Write(desc.usage);
            writer.Write(desc.clearAtFirstUse);
            writer.Write(desc.discardAtLastUse);
            writer.Write(desc.clearColor);
            writer.Write(desc.clearDepth);
            writer.Write(desc.clearStencil);
        }
        else if (resource.type == RGResourceType.Buffer)
        {
            var desc = resource.bufferDesc;
            writer.Write(desc.Size);
            writer.Write(desc.Stride);
            writer.Write(desc.Usage);
            writer.Write(desc.HeapType);
        }
    }
}
