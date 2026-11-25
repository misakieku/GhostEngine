using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.RenderGraphModule;

internal struct MemoryInfo
{
    public ulong offset;
    public ulong size;
    public bool isFree;
    public int firstUsePass;
    public int lastUsePass;
    public int logicalResourceIndex; // Which logical heap is currently using this block

    public MemoryInfo(ulong offset, ulong size)
    {
        this.offset = offset;
        this.size = size;
        isFree = true;
        firstUsePass = int.MaxValue;
        lastUsePass = -1;
        logicalResourceIndex = -1;
    }
}

internal struct PlacedResource : IDisposable
{
    public int index;
    public RGResourceType type;
    public ulong heapOffset;
    public ulong sizeInBytes;

    // Lifetime tracking
    public int firstUsePass = int.MaxValue;
    public int lastUsePass = -1;

    // Aliasing tracking
    public UnsafeList<int> aliasedLogicalResources;
    public MemoryInfo memoryInfo;

    public PlacedResource(AllocationHandle allocationHandle)
    {
        firstUsePass = int.MaxValue;
        lastUsePass = -1;
        aliasedLogicalResources = new UnsafeList<int>(4, allocationHandle);
    }

    public void UpdateLifetime(int passIndex)
    {
        firstUsePass = Math.Min(firstUsePass, passIndex);
        lastUsePass = Math.Max(lastUsePass, passIndex);
    }

    public void Dispose()
    {
        aliasedLogicalResources.Dispose();
    }
}

internal struct AliasingPlan : IDisposable
{
    public ulong totalHeapSize;
    public UnsafeList<PlacedResource> placedResources;
    public UnsafeArray<int> logicalToPlaced;

    public AliasingPlan(int resourceCount, AllocationHandle allocationHandle)
    {
        placedResources = new UnsafeList<PlacedResource>(32, allocationHandle);
        logicalToPlaced = resourceCount > 0
            ? new UnsafeArray<int>(resourceCount, allocationHandle)
            : default;

        if (logicalToPlaced.IsCreated)
        {
            logicalToPlaced.AsSpan().Fill(-1);
        }
    }

    public readonly int GetPlacedResourceIndex(int logicalIndex)
    {
        return logicalToPlaced.IsCreated && (uint)logicalIndex < (uint)logicalToPlaced.Length
            ? logicalToPlaced[logicalIndex]
            : -1;
    }

    public readonly Result<PlacedResource> GetPlacedResource(int placedIndex)
    {
        return placedIndex >= 0 && placedIndex < placedResources.Count
            ? placedResources[placedIndex]
            : Result.Failure();
    }

    public void StoreToCache(ref UnsafeArray<int> outLogicalToPlaced, ref UnsafeArray<PlacedResourceData> outPlacedData, ref UnsafeArray<int> outAliasedLogicalResources)
    {
        if (logicalToPlaced.IsCreated && logicalToPlaced.Length > 0 && outLogicalToPlaced.IsCreated)
        {
            outLogicalToPlaced.CopyFrom(logicalToPlaced);
        }

        if (placedResources.Count != 0)
        {
            outPlacedData.Clear();
            var aliasedLogicalResourceOffset = 0;
            for (var i = 0; i < placedResources.Count; i++)
            {
                var placed = placedResources[i];
                outPlacedData[i] = new PlacedResourceData
                {
                    index = placed.index,
                    type = placed.type,
                    heapOffset = placed.heapOffset,
                    sizeInBytes = placed.sizeInBytes,
                    firstUsePass = placed.firstUsePass,
                    lastUsePass = placed.lastUsePass,
                    aliasedLogicalResourceOffset = aliasedLogicalResourceOffset,
                    aliasedLogicalResourceCount = placed.aliasedLogicalResources.Count
                };

                for (var aliasIndex = 0; aliasIndex < placed.aliasedLogicalResources.Count; aliasIndex++)
                {
                    outAliasedLogicalResources[aliasedLogicalResourceOffset++] = placed.aliasedLogicalResources[aliasIndex];
                }
            }
        }
    }

    public void Dispose()
    {
        for (var i = 0; i < placedResources.Count; i++)
        {
            placedResources[i].Dispose();
        }

        placedResources.Dispose();
        if (logicalToPlaced.IsCreated)
        {
            logicalToPlaced.Dispose();
        }
    }
}

internal static class RenderGraphAliasingBuilder
{
    private readonly struct LogicalResourceEntry
    {
        public readonly int index;
        public readonly RenderGraphResource resource;
        public readonly ulong size;
        public readonly bool allowAliasing;

        public LogicalResourceEntry(int index, RenderGraphResource resource, ulong size, bool allowAliasing)
        {
            this.index = index;
            this.resource = resource;
            this.size = size;
            this.allowAliasing = allowAliasing;
        }
    }

    internal struct ResourceHeap : IDisposable
    {
        private struct HeapSlot
        {
            public ulong offset;
            public ulong size;
            public bool isFree;
            public bool allowAliasing;
            public int firstMember;
            public int lastMember;
        }

        private struct SlotMember
        {
            public int logicalResourceIndex;
            public int next;
        }

        private UnsafeList<HeapSlot> _slots;
        private UnsafeList<SlotMember> _members;
        private RenderGraphResourceOrdering _ordering;

        public int Index
        {
            get; set;
        }

        public ulong Size
        {
            get; set;
        }

        public const ulong DEFAULT_ALIGNMENT = 65536; // 64KB

        public ResourceHeap(int index, ulong size, RenderGraphResourceOrdering ordering, AllocationHandle allocationHandle)
        {
            Index = index;
            Size = size;
            _ordering = ordering;
            _slots = new UnsafeList<HeapSlot>(32, allocationHandle);
            _members = new UnsafeList<SlotMember>(32, allocationHandle);

            _slots.Add(new HeapSlot
            {
                offset = 0,
                size = size,
                isFree = true,
                allowAliasing = true,
                firstMember = -1,
                lastMember = -1
            });
        }

        public (bool success, ulong offset, int slotIndex, MemoryInfo block) TryAllocate(ulong requestedSize, int firstUsePass, int lastUsePass, int logicalResourceIndex,
            ulong alignment = DEFAULT_ALIGNMENT, bool allowAliasing = true)
        {
            var alignedSize = AlignUp(requestedSize, alignment);

            var fitSlotIndex = -1;
            var fitOffset = 0UL;

            for (var i = 0; i < _slots.Count; i++)
            {
                ref readonly var slot = ref _slots[i];
                if (!slot.isFree && (!allowAliasing || !slot.allowAliasing))
                {
                    continue;
                }

                var alignedOffset = AlignUp(slot.offset, alignment);
                var endOffset = alignedOffset + alignedSize;
                if (endOffset > slot.offset + slot.size)
                {
                    continue;
                }

                if (slot.isFree)
                {
                    fitSlotIndex = i;
                    fitOffset = alignedOffset;
                    break;
                }

                // Alias membership and first-use barriers are represented per exact slot.
                if (alignedOffset == slot.offset && CanAliasSlot(in slot, logicalResourceIndex))
                {
                    fitSlotIndex = i;
                    fitOffset = alignedOffset;
                    break;
                }
            }

            if (fitSlotIndex == -1)
            {
                return (false, 0, -1, default);
            }

            var memberIndex = _members.Count;
            _members.Add(new SlotMember
            {
                logicalResourceIndex = logicalResourceIndex,
                next = -1
            });

            ref var targetSlot = ref _slots[fitSlotIndex];
            if (targetSlot.isFree)
            {
                var remainingSize = (targetSlot.offset + targetSlot.size) - (fitOffset + alignedSize);

                targetSlot.offset = fitOffset;
                targetSlot.size = alignedSize;
                targetSlot.isFree = false;
                targetSlot.allowAliasing = allowAliasing;
                targetSlot.firstMember = memberIndex;
                targetSlot.lastMember = memberIndex;

                if (remainingSize > 0)
                {
                    var freeSlot = new HeapSlot
                    {
                        offset = fitOffset + alignedSize,
                        size = remainingSize,
                        isFree = true,
                        allowAliasing = true,
                        firstMember = -1,
                        lastMember = -1
                    };
                    _slots.Insert(fitSlotIndex + 1, freeSlot);
                }
            }
            else
            {
                var prevLast = targetSlot.lastMember;
                var prev = _members[prevLast];
                prev.next = memberIndex;
                _members[prevLast] = prev;
                targetSlot.lastMember = memberIndex;
            }

            var block = new MemoryInfo(fitOffset, alignedSize)
            {
                isFree = false,
                firstUsePass = firstUsePass,
                lastUsePass = lastUsePass,
                logicalResourceIndex = logicalResourceIndex
            };

            return (true, fitOffset, fitSlotIndex, block);
        }

        private readonly bool CanAliasSlot(scoped in HeapSlot slot, int logicalResourceIndex)
        {
            var memberIndex = slot.firstMember;
            while (memberIndex >= 0)
            {
                var member = _members[memberIndex];
                if (!_ordering.CanAlias(logicalResourceIndex, member.logicalResourceIndex))
                {
                    return false;
                }

                memberIndex = member.next;
            }

            return true;
        }

        public readonly void CopyOtherSlotMembers(int slotIndex, int selfLogicalIndex, ref UnsafeList<int> dst)
        {
            var memberIndex = _slots[slotIndex].firstMember;
            while (memberIndex >= 0)
            {
                var member = _members[memberIndex];
                if (member.logicalResourceIndex != selfLogicalIndex)
                {
                    dst.Add(member.logicalResourceIndex);
                }

                memberIndex = member.next;
            }
        }

        public readonly ulong GetPeakUsage()
        {
            var peak = 0ul;
            foreach (var slot in _slots)
            {
                if (!slot.isFree)
                {
                    peak = Math.Max(peak, slot.offset + slot.size);
                }
            }

            return peak;
        }

        private static ulong AlignUp(ulong value, ulong alignment)
        {
            return (value + alignment - 1) & ~(alignment - 1);
        }

        public void Dispose()
        {
            _slots.Dispose();
            _members.Dispose();
        }
    }


    private const ulong DEFAULT_TEXTURE_ALIGNMENT = 65536; // 64KB
    private const ulong DEFAULT_BUFFER_ALIGNMENT = 65536;  // 64KB

    private static ulong GetResourceSize(RenderGraphResource resource, IResourceAllocator allocator)
    {
        if (resource.type == RGResourceType.Texture)
        {
            var textureDesc = resource.rgTextureDesc.ToTextureDesc(resource.resolvedWidth, resource.resolvedHeight);
            return allocator.GetSizeInfo(ResourceDesc.Texture(textureDesc)).Size;
        }
        else // Buffer
        {
            return allocator.GetSizeInfo(ResourceDesc.Buffer(resource.bufferDesc)).Size;
        }
    }

    private static ulong AlignUp(ulong value, ulong alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    /// <summary>
    /// Builds a memory aliasing plan for all transient resources in the registry.
    /// </summary>
    public static AliasingPlan Build(RenderGraphResourceRegistry registry, IResourceAllocator allocator, RenderGraphResourceOrdering ordering, bool noAliasing, AllocationHandle allocationHandle, ReadOnlySpan<CommandQueueType> effectiveQueues = default)
    {
        var plan = new AliasingPlan(registry.ResourceCount, allocationHandle);

        if (noAliasing)
        {
            for (var i = 0; i < registry.ResourceCount; i++)
            {
                var resource = registry.GetResourceByIndex(i);
                if (!resource.isImported && !resource.isExtracted)
                {
                    var size = GetResourceSize(resource, allocator);
                    var placed = new PlacedResource(allocationHandle)
                    {
                        index = plan.placedResources.Count,
                        type = resource.type,
                        heapOffset = plan.totalHeapSize,
                        sizeInBytes = size,
                        firstUsePass = ordering.GetFirstUseScheduleIndex(resource.index),
                        lastUsePass = ordering.GetLastUseScheduleIndex(resource.index)
                    };

                    placed.aliasedLogicalResources.Add(resource.index);
                    plan.placedResources.Add(placed);
                    plan.logicalToPlaced[resource.index] = placed.index;
                    plan.totalHeapSize += size;
                }
            }

            return plan;
        }

        using var scope = AllocationManager.CreateStackScope();
        using var logicalResources = new UnsafeList<LogicalResourceEntry>(registry.ResourceCount, scope.AllocationHandle);

        for (var i = 0; i < registry.ResourceCount; i++)
        {
            var resource = registry.GetResourceByIndex(i);

            // Don't memory-alias imported, extracted, or upload resources
            if (resource.CanBeAliased)
            {
                var size = GetResourceSize(resource, allocator);
                var allowAliasing = true;
                if (resource.type == RGResourceType.Texture && !effectiveQueues.IsEmpty)
                {
                    var firstUse = ordering.GetFirstUseScheduleIndex(resource.index);
                    if (firstUse >= 0 && effectiveQueues[firstUse] == CommandQueueType.Copy)
                    {
                        // Copy queues do not support layout transition / discard aliasing barriers.
                        allowAliasing = false;
                    }
                }

                logicalResources.Add(new LogicalResourceEntry(resource.index, resource, size, allowAliasing));
            }
        }

        // Sort by size descending
        logicalResources.AsSpan().Sort(static (x, y) => y.size.CompareTo(x.size));

        // Simulate allocation to find peak memory usage
        using var simulationHeap = new ResourceHeap(0, ulong.MaxValue, ordering, scope.AllocationHandle);
        using var slotIndexByPlaced = new UnsafeArray<int>(logicalResources.Count, scope.AllocationHandle);
        for (var i = 0; i < logicalResources.Count; i++)
        {
            ref readonly var item = ref logicalResources[i];
            var alignment = item.resource.type == RGResourceType.Texture
                ? DEFAULT_TEXTURE_ALIGNMENT
                : DEFAULT_BUFFER_ALIGNMENT;

            var firstUseScheduleIndex = ordering.GetFirstUseScheduleIndex(item.index);
            var lastUseScheduleIndex = ordering.GetLastUseScheduleIndex(item.index);
            var (success, offset, slotIndex, memInfo) = simulationHeap.TryAllocate(
                item.size,
                firstUseScheduleIndex,
                lastUseScheduleIndex,
                item.index,
                alignment,
                item.allowAliasing);

            Logger.DebugAssert(success, "Simulation allocation failed - heap should be unlimited in size");

            var assignedPlaced = new PlacedResource(allocationHandle)
            {
                index = plan.placedResources.Count,
                type = item.resource.type,
                heapOffset = offset,
                sizeInBytes = item.size,
                firstUsePass = firstUseScheduleIndex,
                lastUsePass = lastUseScheduleIndex,
                memoryInfo = memInfo
            };

            assignedPlaced.aliasedLogicalResources.Add(item.index);

            slotIndexByPlaced[assignedPlaced.index] = slotIndex;
            plan.placedResources.Add(assignedPlaced);
            plan.logicalToPlaced[item.index] = assignedPlaced.index;
        }

        var peakMemoryUsage = AlignUp(simulationHeap.GetPeakUsage(), DEFAULT_TEXTURE_ALIGNMENT);
        plan.totalHeapSize = peakMemoryUsage;

        // Populate aliasedLogicalResources from slot members
        for (var i = 0; i < plan.placedResources.Count; i++)
        {
            ref var placed = ref plan.placedResources[i];
            simulationHeap.CopyOtherSlotMembers(slotIndexByPlaced[i], placed.aliasedLogicalResources[0], ref placed.aliasedLogicalResources);
        }

        return plan;
    }

    /// <summary>
    /// Rebuilds size-dependent placement while preserving cached alias groups exactly.
    /// </summary>
    public static AliasingPlan ResizeCachedSlots(RenderGraphResourceRegistry registry, IResourceAllocator allocator, ReadOnlySpan<int> logicalToPlaced, ReadOnlySpan<PlacedResourceData> placedData,
        ReadOnlySpan<int> aliasedLogicalResources, AllocationHandle allocationHandle)
    {
        var plan = new AliasingPlan(registry.ResourceCount, allocationHandle);
        if (placedData.IsEmpty)
        {
            return plan;
        }

        using var scope = AllocationManager.CreateStackScope();
        using var logicalIndexByPlacedIndex = new UnsafeArray<int>(placedData.Length, scope.AllocationHandle);
        using var resourceSizes = new UnsafeArray<ulong>(placedData.Length, scope.AllocationHandle);
        using var resizedOffsets = new UnsafeArray<ulong>(placedData.Length, scope.AllocationHandle);
        logicalIndexByPlacedIndex.AsSpan().Fill(-1);
        resizedOffsets.AsSpan().Fill(ulong.MaxValue);

        if (!logicalToPlaced.IsEmpty && plan.logicalToPlaced.IsCreated)
        {
            plan.logicalToPlaced.CopyFrom(logicalToPlaced);
            for (var logicalIdx = 0; logicalIdx < logicalToPlaced.Length; logicalIdx++)
            {
                var placedIdx = logicalToPlaced[logicalIdx];
                if ((uint)placedIdx < (uint)placedData.Length)
                {
                    logicalIndexByPlacedIndex[placedIdx] = logicalIdx;
                }
            }
        }

        for (var placedIndex = 0; placedIndex < placedData.Length; placedIndex++)
        {
            var logicalResourceIndex = logicalIndexByPlacedIndex[placedIndex];
            Logger.DebugAssert(logicalResourceIndex >= 0);
            ref readonly var resource = ref registry.GetResourceByIndex(logicalResourceIndex);
            resourceSizes[placedIndex] = GetResourceSize(resource, allocator);
        }

        var nextOffset = 0UL;
        for (var placedIndex = 0; placedIndex < placedData.Length; placedIndex++)
        {
            if (resizedOffsets[placedIndex] != ulong.MaxValue)
            {
                continue;
            }

            ref readonly var placedDataEntry = ref placedData[placedIndex];
            var aliasStart = placedDataEntry.aliasedLogicalResourceOffset;
            var aliasEnd = aliasStart + placedDataEntry.aliasedLogicalResourceCount;
            var slotSize = 0UL;
            var slotAlignment = DEFAULT_BUFFER_ALIGNMENT;

            for (var aliasIndex = aliasStart; aliasIndex < aliasEnd; aliasIndex++)
            {
                var memberLogicalIndex = aliasedLogicalResources[aliasIndex];
                var memberPlacedIndex = logicalToPlaced[memberLogicalIndex];
                ref readonly var memberResource = ref registry.GetResourceByIndex(memberLogicalIndex);
                var alignment = memberResource.type == RGResourceType.Texture
                    ? DEFAULT_TEXTURE_ALIGNMENT
                    : DEFAULT_BUFFER_ALIGNMENT;
                slotAlignment = Math.Max(slotAlignment, alignment);
                slotSize = Math.Max(slotSize, AlignUp(resourceSizes[memberPlacedIndex], alignment));
            }

            nextOffset = AlignUp(nextOffset, slotAlignment);
            for (var aliasIndex = aliasStart; aliasIndex < aliasEnd; aliasIndex++)
            {
                var memberPlacedIndex = logicalToPlaced[aliasedLogicalResources[aliasIndex]];
                resizedOffsets[memberPlacedIndex] = nextOffset;
            }
            nextOffset += slotSize;
        }

        for (var placedIndex = 0; placedIndex < placedData.Length; placedIndex++)
        {
            ref readonly var cachedPlaced = ref placedData[placedIndex];
            var placed = new PlacedResource(allocationHandle)
            {
                index = cachedPlaced.index,
                type = cachedPlaced.type,
                heapOffset = resizedOffsets[placedIndex],
                sizeInBytes = resourceSizes[placedIndex],
                firstUsePass = cachedPlaced.firstUsePass,
                lastUsePass = cachedPlaced.lastUsePass
            };

            var aliasEnd = cachedPlaced.aliasedLogicalResourceOffset + cachedPlaced.aliasedLogicalResourceCount;
            for (var aliasIndex = cachedPlaced.aliasedLogicalResourceOffset; aliasIndex < aliasEnd; aliasIndex++)
            {
                placed.aliasedLogicalResources.Add(aliasedLogicalResources[aliasIndex]);
            }

            plan.placedResources.Add(placed);
        }

        plan.totalHeapSize = AlignUp(nextOffset, DEFAULT_TEXTURE_ALIGNMENT);
        return plan;
    }
}
