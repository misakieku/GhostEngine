
using Ghost.Core;

namespace Ghost.Entities;

public unsafe partial struct EntityQuery
{
    public readonly void ForEach<T0>(ForEach<T0> action)
        where T0 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
        };

        var changedCompIDs = stackalloc int[1];
        var offsets = stackalloc int[1];
        var basePtrs = stackalloc byte*[1];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 1; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 1; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 1; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));

                    action(ref *pComp0);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
        };

        var changedCompIDs = stackalloc int[2];
        var offsets = stackalloc int[2];
        var basePtrs = stackalloc byte*[2];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 2; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 2; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 2; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));

                    action(ref *pComp0, ref *pComp1);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
        };

        var changedCompIDs = stackalloc int[3];
        var offsets = stackalloc int[3];
        var basePtrs = stackalloc byte*[3];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 3; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 3; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 3; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));

                    action(ref *pComp0, ref *pComp1, ref *pComp2);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        var comp3TypeID = ComponentTypeID<T3>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
            comp3TypeID.value,
        };

        var changedCompIDs = stackalloc int[4];
        var offsets = stackalloc int[4];
        var basePtrs = stackalloc byte*[4];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 4; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 4; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 4; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));

                    action(ref *pComp0, ref *pComp1, ref *pComp2, ref *pComp3);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        var comp3TypeID = ComponentTypeID<T3>.value;
        var comp4TypeID = ComponentTypeID<T4>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
            comp3TypeID.value,
            comp4TypeID.value,
        };

        var changedCompIDs = stackalloc int[5];
        var offsets = stackalloc int[5];
        var basePtrs = stackalloc byte*[5];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 5; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 5; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 5; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));

                    action(ref *pComp0, ref *pComp1, ref *pComp2, ref *pComp3, ref *pComp4);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        var comp3TypeID = ComponentTypeID<T3>.value;
        var comp4TypeID = ComponentTypeID<T4>.value;
        var comp5TypeID = ComponentTypeID<T5>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
            comp3TypeID.value,
            comp4TypeID.value,
            comp5TypeID.value,
        };

        var changedCompIDs = stackalloc int[6];
        var offsets = stackalloc int[6];
        var basePtrs = stackalloc byte*[6];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 6; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 6; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 6; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));

                    action(ref *pComp0, ref *pComp1, ref *pComp2, ref *pComp3, ref *pComp4, ref *pComp5);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        var comp3TypeID = ComponentTypeID<T3>.value;
        var comp4TypeID = ComponentTypeID<T4>.value;
        var comp5TypeID = ComponentTypeID<T5>.value;
        var comp6TypeID = ComponentTypeID<T6>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
            comp3TypeID.value,
            comp4TypeID.value,
            comp5TypeID.value,
            comp6TypeID.value,
        };

        var changedCompIDs = stackalloc int[7];
        var offsets = stackalloc int[7];
        var basePtrs = stackalloc byte*[7];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 7; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 7; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 7; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));
                    var pComp6 = (T6*)(basePtrs[6] + (sizeof(T6) * entityIndex));

                    action(ref *pComp0, ref *pComp1, ref *pComp2, ref *pComp3, ref *pComp4, ref *pComp5, ref *pComp6);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        var comp3TypeID = ComponentTypeID<T3>.value;
        var comp4TypeID = ComponentTypeID<T4>.value;
        var comp5TypeID = ComponentTypeID<T5>.value;
        var comp6TypeID = ComponentTypeID<T6>.value;
        var comp7TypeID = ComponentTypeID<T7>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
            comp3TypeID.value,
            comp4TypeID.value,
            comp5TypeID.value,
            comp6TypeID.value,
            comp7TypeID.value,
        };

        var changedCompIDs = stackalloc int[8];
        var offsets = stackalloc int[8];
        var basePtrs = stackalloc byte*[8];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 8; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 8; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 8; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));
                    var pComp6 = (T6*)(basePtrs[6] + (sizeof(T6) * entityIndex));
                    var pComp7 = (T7*)(basePtrs[7] + (sizeof(T7) * entityIndex));

                    action(ref *pComp0, ref *pComp1, ref *pComp2, ref *pComp3, ref *pComp4, ref *pComp5, ref *pComp6, ref *pComp7);
                }
            }
        }
    }

    public readonly void ForEach<T0>(ForEachWithEntity<T0> action)
        where T0 : unmanaged, IComponent
    {
        var world = World.GetWorldUncheck(_worldID);
        var globalVersion = world.Version;

        var comp0TypeID = ComponentTypeID<T0>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
        };

        var changedCompIDs = stackalloc int[1];
        var offsets = stackalloc int[1];
        var basePtrs = stackalloc byte*[1];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 1; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 1; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 1; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));

                    var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    action(*pEntity, ref *pComp0);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
        };

        var changedCompIDs = stackalloc int[2];
        var offsets = stackalloc int[2];
        var basePtrs = stackalloc byte*[2];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 2; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 2; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 2; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));

                    var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    action(*pEntity, ref *pComp0, ref *pComp1);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
        };

        var changedCompIDs = stackalloc int[3];
        var offsets = stackalloc int[3];
        var basePtrs = stackalloc byte*[3];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 3; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 3; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 3; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));

                    var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    action(*pEntity, ref *pComp0, ref *pComp1, ref *pComp2);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        var comp3TypeID = ComponentTypeID<T3>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
            comp3TypeID.value,
        };

        var changedCompIDs = stackalloc int[4];
        var offsets = stackalloc int[4];
        var basePtrs = stackalloc byte*[4];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 4; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 4; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 4; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));

                    var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    action(*pEntity, ref *pComp0, ref *pComp1, ref *pComp2, ref *pComp3);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        var comp3TypeID = ComponentTypeID<T3>.value;
        var comp4TypeID = ComponentTypeID<T4>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
            comp3TypeID.value,
            comp4TypeID.value,
        };

        var changedCompIDs = stackalloc int[5];
        var offsets = stackalloc int[5];
        var basePtrs = stackalloc byte*[5];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 5; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 5; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 5; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));

                    var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    action(*pEntity, ref *pComp0, ref *pComp1, ref *pComp2, ref *pComp3, ref *pComp4);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        var comp3TypeID = ComponentTypeID<T3>.value;
        var comp4TypeID = ComponentTypeID<T4>.value;
        var comp5TypeID = ComponentTypeID<T5>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
            comp3TypeID.value,
            comp4TypeID.value,
            comp5TypeID.value,
        };

        var changedCompIDs = stackalloc int[6];
        var offsets = stackalloc int[6];
        var basePtrs = stackalloc byte*[6];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 6; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 6; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 6; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));

                    var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    action(*pEntity, ref *pComp0, ref *pComp1, ref *pComp2, ref *pComp3, ref *pComp4, ref *pComp5);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        var comp3TypeID = ComponentTypeID<T3>.value;
        var comp4TypeID = ComponentTypeID<T4>.value;
        var comp5TypeID = ComponentTypeID<T5>.value;
        var comp6TypeID = ComponentTypeID<T6>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
            comp3TypeID.value,
            comp4TypeID.value,
            comp5TypeID.value,
            comp6TypeID.value,
        };

        var changedCompIDs = stackalloc int[7];
        var offsets = stackalloc int[7];
        var basePtrs = stackalloc byte*[7];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 7; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 7; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 7; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));
                    var pComp6 = (T6*)(basePtrs[6] + (sizeof(T6) * entityIndex));

                    var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    action(*pEntity, ref *pComp0, ref *pComp1, ref *pComp2, ref *pComp3, ref *pComp4, ref *pComp5, ref *pComp6);
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

        var comp0TypeID = ComponentTypeID<T0>.value;
        var comp1TypeID = ComponentTypeID<T1>.value;
        var comp2TypeID = ComponentTypeID<T2>.value;
        var comp3TypeID = ComponentTypeID<T3>.value;
        var comp4TypeID = ComponentTypeID<T4>.value;
        var comp5TypeID = ComponentTypeID<T5>.value;
        var comp6TypeID = ComponentTypeID<T6>.value;
        var comp7TypeID = ComponentTypeID<T7>.value;
        
        var compTypeIDs = stackalloc int[]
        {
            comp0TypeID.value,
            comp1TypeID.value,
            comp2TypeID.value,
            comp3TypeID.value,
            comp4TypeID.value,
            comp5TypeID.value,
            comp6TypeID.value,
            comp7TypeID.value,
        };

        var changedCompIDs = stackalloc int[8];
        var offsets = stackalloc int[8];
        var basePtrs = stackalloc byte*[8];

        var changedCompCount = 0;

        var it = _mask.writeAccess.GetIterator();
        while (it.Next(out var id))
        {
            for (var i =0; i < 8; i++)
            {
                if (id == compTypeIDs[i])
                {
                    ComponentRegister.SetComponentLastWrite(id, globalVersion);
                    changedCompIDs[changedCompCount] = id;
                    changedCompCount++;
                    break;
                }
            }
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            ref var archetype = ref world.GetArchetypeReference(_matchingArchetypes[i]);
            var hasAllComponents = true;
            for (var index = 0; index < 8; index++)
            {
                var layoutResult = archetype.GetLayout(compTypeIDs[index]);
                if (!layoutResult)
                {
                    hasAllComponents = false;
                    break;
                }

                offsets[index] = layoutResult.Value.offset;
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var pChunkData = chunk.GetUnsafePtr();

                for (var j = 0; j < changedCompCount; j++)
                {
                    chunk.MarkChanged(changedCompIDs[i], globalVersion);
                }

                for (var index = 0; index < 8; index++)
                {
                    basePtrs[index] = pChunkData + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < chunk._count; entityIndex++)
                {
                    if (!IsEntityValid(pChunkData, entityIndex, in archetype, in _mask))
                    {
                        continue;
                    }

                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));
                    var pComp6 = (T6*)(basePtrs[6] + (sizeof(T6) * entityIndex));
                    var pComp7 = (T7*)(basePtrs[7] + (sizeof(T7) * entityIndex));

                    var pEntity = (Entity*)(pChunkData + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    action(*pEntity, ref *pComp0, ref *pComp1, ref *pComp2, ref *pComp3, ref *pComp4, ref *pComp5, ref *pComp6, ref *pComp7);
                }
            }
        }
    }

}
