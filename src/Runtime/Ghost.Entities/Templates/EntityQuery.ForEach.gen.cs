using System.Runtime.CompilerServices;
using Ghost.Core;

namespace Ghost.Entities;

public unsafe partial struct EntityQuery
{
    public readonly void ForEach<T0>(ForEach<T0> action)
        where T0 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
        };

        var changedCompIDs = stackalloc int[1];
        var offsets = stackalloc int[1];
        var basePtrs = stackalloc byte*[1];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 1; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        action(                            ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            action(                                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1>(ForEach<T0, T1> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
        };

        var changedCompIDs = stackalloc int[2];
        var offsets = stackalloc int[2];
        var basePtrs = stackalloc byte*[2];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 2; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        action(                            ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            action(                                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2>(ForEach<T0, T1, T2> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
        };

        var changedCompIDs = stackalloc int[3];
        var offsets = stackalloc int[3];
        var basePtrs = stackalloc byte*[3];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 3; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        action(                            ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            action(                                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2, T3>(ForEach<T0, T1, T2, T3> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;
        var comp3TypeID = ComponentTypeID<T3>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
            comp3TypeID.Value,
        };

        var changedCompIDs = stackalloc int[4];
        var offsets = stackalloc int[4];
        var basePtrs = stackalloc byte*[4];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 4; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T3>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[3] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[3];
                }
                else
                {
                    basePtrs[3] = pChunkData + offsets[3];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        action(                            ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                            ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            action(                                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2, T3, T4>(ForEach<T0, T1, T2, T3, T4> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;
        var comp3TypeID = ComponentTypeID<T3>.Value;
        var comp4TypeID = ComponentTypeID<T4>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
            comp3TypeID.Value,
            comp4TypeID.Value,
        };

        var changedCompIDs = stackalloc int[5];
        var offsets = stackalloc int[5];
        var basePtrs = stackalloc byte*[5];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 5; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T3>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T4>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[3] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[3];
                }
                else
                {
                    basePtrs[3] = pChunkData + offsets[3];
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[4] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[4];
                }
                else
                {
                    basePtrs[4] = pChunkData + offsets[4];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        action(                            ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                            ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                            ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            action(                                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2, T3, T4, T5>(ForEach<T0, T1, T2, T3, T4, T5> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;
        var comp3TypeID = ComponentTypeID<T3>.Value;
        var comp4TypeID = ComponentTypeID<T4>.Value;
        var comp5TypeID = ComponentTypeID<T5>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
            comp3TypeID.Value,
            comp4TypeID.Value,
            comp5TypeID.Value,
        };

        var changedCompIDs = stackalloc int[6];
        var offsets = stackalloc int[6];
        var basePtrs = stackalloc byte*[6];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 6; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T3>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T4>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T5>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[3] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[3];
                }
                else
                {
                    basePtrs[3] = pChunkData + offsets[3];
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[4] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[4];
                }
                else
                {
                    basePtrs[4] = pChunkData + offsets[4];
                }
                if (ComponentTypeID<T5>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[5] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[5];
                }
                else
                {
                    basePtrs[5] = pChunkData + offsets[5];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        action(                            ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                            ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                            ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                            ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            action(                                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                                ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2, T3, T4, T5, T6>(ForEach<T0, T1, T2, T3, T4, T5, T6> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;
        var comp3TypeID = ComponentTypeID<T3>.Value;
        var comp4TypeID = ComponentTypeID<T4>.Value;
        var comp5TypeID = ComponentTypeID<T5>.Value;
        var comp6TypeID = ComponentTypeID<T6>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
            comp3TypeID.Value,
            comp4TypeID.Value,
            comp5TypeID.Value,
            comp6TypeID.Value,
        };

        var changedCompIDs = stackalloc int[7];
        var offsets = stackalloc int[7];
        var basePtrs = stackalloc byte*[7];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 7; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T3>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T4>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T5>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T6>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[6]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[6] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[6]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[6] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[3] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[3];
                }
                else
                {
                    basePtrs[3] = pChunkData + offsets[3];
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[4] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[4];
                }
                else
                {
                    basePtrs[4] = pChunkData + offsets[4];
                }
                if (ComponentTypeID<T5>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[5] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[5];
                }
                else
                {
                    basePtrs[5] = pChunkData + offsets[5];
                }
                if (ComponentTypeID<T6>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[6] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[6];
                }
                else
                {
                    basePtrs[6] = pChunkData + offsets[6];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        action(                            ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                            ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                            ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                            ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex]),
                            ref (ComponentTypeID<T6>.IsShared ? ref ((T6*)basePtrs[6])[0] : ref ((T6*)basePtrs[6])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            action(                                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                                ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex]),
                                ref (ComponentTypeID<T6>.IsShared ? ref ((T6*)basePtrs[6])[0] : ref ((T6*)basePtrs[6])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2, T3, T4, T5, T6, T7>(ForEach<T0, T1, T2, T3, T4, T5, T6, T7> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;
        var comp3TypeID = ComponentTypeID<T3>.Value;
        var comp4TypeID = ComponentTypeID<T4>.Value;
        var comp5TypeID = ComponentTypeID<T5>.Value;
        var comp6TypeID = ComponentTypeID<T6>.Value;
        var comp7TypeID = ComponentTypeID<T7>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
            comp3TypeID.Value,
            comp4TypeID.Value,
            comp5TypeID.Value,
            comp6TypeID.Value,
            comp7TypeID.Value,
        };

        var changedCompIDs = stackalloc int[8];
        var offsets = stackalloc int[8];
        var basePtrs = stackalloc byte*[8];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 8; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T3>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T4>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T5>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T6>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[6]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[6] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[6]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[6] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T7>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[7]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[7] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[7]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[7] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[3] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[3];
                }
                else
                {
                    basePtrs[3] = pChunkData + offsets[3];
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[4] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[4];
                }
                else
                {
                    basePtrs[4] = pChunkData + offsets[4];
                }
                if (ComponentTypeID<T5>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[5] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[5];
                }
                else
                {
                    basePtrs[5] = pChunkData + offsets[5];
                }
                if (ComponentTypeID<T6>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[6] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[6];
                }
                else
                {
                    basePtrs[6] = pChunkData + offsets[6];
                }
                if (ComponentTypeID<T7>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[7] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[7];
                }
                else
                {
                    basePtrs[7] = pChunkData + offsets[7];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        action(                            ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                            ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                            ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                            ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex]),
                            ref (ComponentTypeID<T6>.IsShared ? ref ((T6*)basePtrs[6])[0] : ref ((T6*)basePtrs[6])[entityIndex]),
                            ref (ComponentTypeID<T7>.IsShared ? ref ((T7*)basePtrs[7])[0] : ref ((T7*)basePtrs[7])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            action(                                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                                ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex]),
                                ref (ComponentTypeID<T6>.IsShared ? ref ((T6*)basePtrs[6])[0] : ref ((T6*)basePtrs[6])[entityIndex]),
                                ref (ComponentTypeID<T7>.IsShared ? ref ((T7*)basePtrs[7])[0] : ref ((T7*)basePtrs[7])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0>(ForEachWithEntity<T0> action)
        where T0 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
        };

        var changedCompIDs = stackalloc int[1];
        var offsets = stackalloc int[1];
        var basePtrs = stackalloc byte*[1];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 1; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                        action(*pEntity,                             ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                            action(*pEntity,                                 ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1>(ForEachWithEntity<T0, T1> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
        };

        var changedCompIDs = stackalloc int[2];
        var offsets = stackalloc int[2];
        var basePtrs = stackalloc byte*[2];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 2; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                        action(*pEntity,                             ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                            action(*pEntity,                                 ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2>(ForEachWithEntity<T0, T1, T2> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
        };

        var changedCompIDs = stackalloc int[3];
        var offsets = stackalloc int[3];
        var basePtrs = stackalloc byte*[3];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 3; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                        action(*pEntity,                             ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                            action(*pEntity,                                 ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2, T3>(ForEachWithEntity<T0, T1, T2, T3> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;
        var comp3TypeID = ComponentTypeID<T3>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
            comp3TypeID.Value,
        };

        var changedCompIDs = stackalloc int[4];
        var offsets = stackalloc int[4];
        var basePtrs = stackalloc byte*[4];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 4; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T3>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[3] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[3];
                }
                else
                {
                    basePtrs[3] = pChunkData + offsets[3];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                        action(*pEntity,                             ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                            ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                            action(*pEntity,                                 ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2, T3, T4>(ForEachWithEntity<T0, T1, T2, T3, T4> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;
        var comp3TypeID = ComponentTypeID<T3>.Value;
        var comp4TypeID = ComponentTypeID<T4>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
            comp3TypeID.Value,
            comp4TypeID.Value,
        };

        var changedCompIDs = stackalloc int[5];
        var offsets = stackalloc int[5];
        var basePtrs = stackalloc byte*[5];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 5; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T3>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T4>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[3] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[3];
                }
                else
                {
                    basePtrs[3] = pChunkData + offsets[3];
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[4] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[4];
                }
                else
                {
                    basePtrs[4] = pChunkData + offsets[4];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                        action(*pEntity,                             ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                            ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                            ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                            action(*pEntity,                                 ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2, T3, T4, T5>(ForEachWithEntity<T0, T1, T2, T3, T4, T5> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;
        var comp3TypeID = ComponentTypeID<T3>.Value;
        var comp4TypeID = ComponentTypeID<T4>.Value;
        var comp5TypeID = ComponentTypeID<T5>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
            comp3TypeID.Value,
            comp4TypeID.Value,
            comp5TypeID.Value,
        };

        var changedCompIDs = stackalloc int[6];
        var offsets = stackalloc int[6];
        var basePtrs = stackalloc byte*[6];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 6; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T3>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T4>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T5>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[3] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[3];
                }
                else
                {
                    basePtrs[3] = pChunkData + offsets[3];
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[4] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[4];
                }
                else
                {
                    basePtrs[4] = pChunkData + offsets[4];
                }
                if (ComponentTypeID<T5>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[5] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[5];
                }
                else
                {
                    basePtrs[5] = pChunkData + offsets[5];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                        action(*pEntity,                             ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                            ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                            ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                            ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                            action(*pEntity,                                 ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                                ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2, T3, T4, T5, T6>(ForEachWithEntity<T0, T1, T2, T3, T4, T5, T6> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;
        var comp3TypeID = ComponentTypeID<T3>.Value;
        var comp4TypeID = ComponentTypeID<T4>.Value;
        var comp5TypeID = ComponentTypeID<T5>.Value;
        var comp6TypeID = ComponentTypeID<T6>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
            comp3TypeID.Value,
            comp4TypeID.Value,
            comp5TypeID.Value,
            comp6TypeID.Value,
        };

        var changedCompIDs = stackalloc int[7];
        var offsets = stackalloc int[7];
        var basePtrs = stackalloc byte*[7];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 7; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T3>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T4>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T5>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T6>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[6]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[6] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[6]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[6] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[3] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[3];
                }
                else
                {
                    basePtrs[3] = pChunkData + offsets[3];
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[4] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[4];
                }
                else
                {
                    basePtrs[4] = pChunkData + offsets[4];
                }
                if (ComponentTypeID<T5>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[5] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[5];
                }
                else
                {
                    basePtrs[5] = pChunkData + offsets[5];
                }
                if (ComponentTypeID<T6>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[6] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[6];
                }
                else
                {
                    basePtrs[6] = pChunkData + offsets[6];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                        action(*pEntity,                             ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                            ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                            ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                            ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex]),
                            ref (ComponentTypeID<T6>.IsShared ? ref ((T6*)basePtrs[6])[0] : ref ((T6*)basePtrs[6])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                            action(*pEntity,                                 ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                                ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex]),
                                ref (ComponentTypeID<T6>.IsShared ? ref ((T6*)basePtrs[6])[0] : ref ((T6*)basePtrs[6])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2, T3, T4, T5, T6, T7>(ForEachWithEntity<T0, T1, T2, T3, T4, T5, T6, T7> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.Value;
        var comp1TypeID = ComponentTypeID<T1>.Value;
        var comp2TypeID = ComponentTypeID<T2>.Value;
        var comp3TypeID = ComponentTypeID<T3>.Value;
        var comp4TypeID = ComponentTypeID<T4>.Value;
        var comp5TypeID = ComponentTypeID<T5>.Value;
        var comp6TypeID = ComponentTypeID<T6>.Value;
        var comp7TypeID = ComponentTypeID<T7>.Value;

        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.Value,
            comp1TypeID.Value,
            comp2TypeID.Value,
            comp3TypeID.Value,
            comp4TypeID.Value,
            comp5TypeID.Value,
            comp6TypeID.Value,
            comp7TypeID.Value,
        };

        var changedCompIDs = stackalloc int[8];
        var offsets = stackalloc int[8];
        var basePtrs = stackalloc byte*[8];

        var changedCompCount = 0;

        var writeIt = _mask.writeAccess.GetIterator();
        while (writeIt.Next(out var id))
        {
            for (var idx = 0; idx < 8; idx++)
            {
                if (id == compTypeIDs[idx])
                {
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        var reqOffsets = stackalloc int[16];
        var reqDisOffsets = stackalloc int[16];
        var rejOffsets = stackalloc int[16];

        for (var archIndex = 0; archIndex < _matchingArchetypes.Count; archIndex++)
        {
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(_matchingArchetypes[archIndex]);
            var hasAllComponents = true;
            if (ComponentTypeID<T0>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[0]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[0] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T1>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[1]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[1] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T2>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[2]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[2] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T3>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[3]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[3] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T4>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[4]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[4] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T5>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[5]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[5] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T6>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[6]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[6] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[6]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[6] = layoutResult.Value.offset;
            }
            if (ComponentTypeID<T7>.IsShared)
            {
                var layoutResult = archetype.GetSharedLayout(compTypeIDs[7]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[7] = layoutResult.Value.offset;
            }
            else
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[7]);
                if (!layoutResult) { hasAllComponents = false; goto skipArchetype; }
                offsets[7] = layoutResult.Value.offset;
            }
        skipArchetype:
            if (!hasAllComponents)
            {
                continue;
            }

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);
            
            var reqCount = 0;
            var reqDisCount = 0;
            var rejCount = 0;

            if (requiresFiltering)
            {
                var itE = _mask.requireEnabled.GetIterator();
                while (itE.Next(out var id) && reqCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqOffsets[reqCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.requireDisabled.GetIterator();
                while (itE.Next(out var id) && reqDisCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        reqDisOffsets[reqDisCount++] = layoutResult.Value.enableBitsOffset;
                }

                itE = _mask.rejectIfEnabled.GetIterator();
                while (itE.Next(out var id) && rejCount < 16)
                {
                    var layoutResult = archetype.GetLayout(id);
                    if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        rejOffsets[rejCount++] = layoutResult.Value.enableBitsOffset;
                }
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                if (chunk._count == 0) continue;

                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    archetype.MarkChanged(chunkIndex, changedCompIDs[j], globalVersion);
                }

                if (ComponentTypeID<T0>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[0] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[0];
                }
                else
                {
                    basePtrs[0] = pChunkData + offsets[0];
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[1] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[1];
                }
                else
                {
                    basePtrs[1] = pChunkData + offsets[1];
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[2] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[2];
                }
                else
                {
                    basePtrs[2] = pChunkData + offsets[2];
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[3] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[3];
                }
                else
                {
                    basePtrs[3] = pChunkData + offsets[3];
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[4] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[4];
                }
                else
                {
                    basePtrs[4] = pChunkData + offsets[4];
                }
                if (ComponentTypeID<T5>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[5] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[5];
                }
                else
                {
                    basePtrs[5] = pChunkData + offsets[5];
                }
                if (ComponentTypeID<T6>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[6] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[6];
                }
                else
                {
                    basePtrs[6] = pChunkData + offsets[6];
                }
                if (ComponentTypeID<T7>.IsShared)
                {
                    var sharedSpan = archetype._chunkGroups[chunk._groupIndex].sharedData.AsSpan();
                    basePtrs[7] = (byte*)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + offsets[7];
                }
                else
                {
                    basePtrs[7] = pChunkData + offsets[7];
                }

                if (!requiresFiltering)
                {
                    for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                    {
                        var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                        action(*pEntity,                             ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                            ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                            ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                            ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                            ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                            ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex]),
                            ref (ComponentTypeID<T6>.IsShared ? ref ((T6*)basePtrs[6])[0] : ref ((T6*)basePtrs[6])[entityIndex]),
                            ref (ComponentTypeID<T7>.IsShared ? ref ((T7*)basePtrs[7])[0] : ref ((T7*)basePtrs[7])[entityIndex])
);
                    }
                }
                else
                {
                    var ulongCount = (chunk._count + 63) / 64;
                    for (var block = 0; block < ulongCount; block++)
                    {
                        var validMask = ulong.MaxValue;
                        var remaining = chunk._count - (block * 64);
                        if (remaining < 64) validMask = (1UL << remaining) - 1UL;

                        for (var h = 0; h < reqCount; h++)
                        {
                            validMask &= ((ulong*)(pChunkData + reqOffsets[h]))[block];
                        }

                        for (var h = 0; h < reqDisCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + reqDisOffsets[h]))[block];
                        }

                        for (var h = 0; h < rejCount; h++)
                        {
                            validMask &= ~((ulong*)(pChunkData + rejOffsets[h]))[block];
                        }

                        while (validMask != 0)
                        {
                            var bit = System.Numerics.BitOperations.TrailingZeroCount(validMask);
                            var entityIndex = (block * 64) + bit;

                            var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                            action(*pEntity,                                 ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)basePtrs[0])[0] : ref ((T0*)basePtrs[0])[entityIndex]),
                                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)basePtrs[1])[0] : ref ((T1*)basePtrs[1])[entityIndex]),
                                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)basePtrs[2])[0] : ref ((T2*)basePtrs[2])[entityIndex]),
                                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)basePtrs[3])[0] : ref ((T3*)basePtrs[3])[entityIndex]),
                                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)basePtrs[4])[0] : ref ((T4*)basePtrs[4])[entityIndex]),
                                ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)basePtrs[5])[0] : ref ((T5*)basePtrs[5])[entityIndex]),
                                ref (ComponentTypeID<T6>.IsShared ? ref ((T6*)basePtrs[6])[0] : ref ((T6*)basePtrs[6])[entityIndex]),
                                ref (ComponentTypeID<T7>.IsShared ? ref ((T7*)basePtrs[7])[0] : ref ((T7*)basePtrs[7])[entityIndex])
);
                            validMask ^= (1UL << bit);
                        }
                    }
                }
            }
        }
    }

}
