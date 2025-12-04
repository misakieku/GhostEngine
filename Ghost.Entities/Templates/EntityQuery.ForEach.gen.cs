
using Ghost.Core;

namespace Ghost.Entities;

public unsafe partial struct EntityQuery
{
    public readonly void ForEach<T0>(ForEach<T0> action)
        where T0 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value };
        var offsets = stackalloc int[1];
        var basePtrs = stackalloc byte*[1];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 1; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 1; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value };
        var offsets = stackalloc int[2];
        var basePtrs = stackalloc byte*[2];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 2; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 2; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));

                    action(ref *pComp0,ref *pComp1);
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2>(ForEach<T0, T1, T2> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value };
        var offsets = stackalloc int[3];
        var basePtrs = stackalloc byte*[3];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 3; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 3; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));

                    action(ref *pComp0,ref *pComp1,ref *pComp2);
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value, ComponentTypeID<T3>.value };
        var offsets = stackalloc int[4];
        var basePtrs = stackalloc byte*[4];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 4; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 4; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));

                    action(ref *pComp0,ref *pComp1,ref *pComp2,ref *pComp3);
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value, ComponentTypeID<T3>.value, ComponentTypeID<T4>.value };
        var offsets = stackalloc int[5];
        var basePtrs = stackalloc byte*[5];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 5; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 5; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));

                    action(ref *pComp0,ref *pComp1,ref *pComp2,ref *pComp3,ref *pComp4);
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value, ComponentTypeID<T3>.value, ComponentTypeID<T4>.value, ComponentTypeID<T5>.value };
        var offsets = stackalloc int[6];
        var basePtrs = stackalloc byte*[6];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 6; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 6; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));

                    action(ref *pComp0,ref *pComp1,ref *pComp2,ref *pComp3,ref *pComp4,ref *pComp5);
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value, ComponentTypeID<T3>.value, ComponentTypeID<T4>.value, ComponentTypeID<T5>.value, ComponentTypeID<T6>.value };
        var offsets = stackalloc int[7];
        var basePtrs = stackalloc byte*[7];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 7; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 7; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));
                    var pComp6 = (T6*)(basePtrs[6] + (sizeof(T6) * entityIndex));

                    action(ref *pComp0,ref *pComp1,ref *pComp2,ref *pComp3,ref *pComp4,ref *pComp5,ref *pComp6);
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value, ComponentTypeID<T3>.value, ComponentTypeID<T4>.value, ComponentTypeID<T5>.value, ComponentTypeID<T6>.value, ComponentTypeID<T7>.value };
        var offsets = stackalloc int[8];
        var basePtrs = stackalloc byte*[8];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 8; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 8; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));
                    var pComp6 = (T6*)(basePtrs[6] + (sizeof(T6) * entityIndex));
                    var pComp7 = (T7*)(basePtrs[7] + (sizeof(T7) * entityIndex));

                    action(ref *pComp0,ref *pComp1,ref *pComp2,ref *pComp3,ref *pComp4,ref *pComp5,ref *pComp6,ref *pComp7);
                }
            }
        }
    }


    public readonly void ForEach<T0>(ForEachWithEntity<T0> action)
        where T0 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value };
        var offsets = stackalloc int[1];
        var basePtrs = stackalloc byte*[1];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 1; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 1; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pEntity = (Entity*)(chunk.GetUnsafePtr() + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));

                    action(*pEntity, ref *pComp0);
                }
            }
        }
    }

    public readonly void ForEach<T0, T1>(ForEachWithEntity<T0, T1> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value };
        var offsets = stackalloc int[2];
        var basePtrs = stackalloc byte*[2];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 2; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 2; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pEntity = (Entity*)(chunk.GetUnsafePtr() + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));

                    action(*pEntity, ref *pComp0,ref *pComp1);
                }
            }
        }
    }

    public readonly void ForEach<T0, T1, T2>(ForEachWithEntity<T0, T1, T2> action)
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value };
        var offsets = stackalloc int[3];
        var basePtrs = stackalloc byte*[3];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 3; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 3; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pEntity = (Entity*)(chunk.GetUnsafePtr() + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));

                    action(*pEntity, ref *pComp0,ref *pComp1,ref *pComp2);
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value, ComponentTypeID<T3>.value };
        var offsets = stackalloc int[4];
        var basePtrs = stackalloc byte*[4];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 4; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 4; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pEntity = (Entity*)(chunk.GetUnsafePtr() + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));

                    action(*pEntity, ref *pComp0,ref *pComp1,ref *pComp2,ref *pComp3);
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value, ComponentTypeID<T3>.value, ComponentTypeID<T4>.value };
        var offsets = stackalloc int[5];
        var basePtrs = stackalloc byte*[5];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 5; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 5; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pEntity = (Entity*)(chunk.GetUnsafePtr() + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));

                    action(*pEntity, ref *pComp0,ref *pComp1,ref *pComp2,ref *pComp3,ref *pComp4);
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value, ComponentTypeID<T3>.value, ComponentTypeID<T4>.value, ComponentTypeID<T5>.value };
        var offsets = stackalloc int[6];
        var basePtrs = stackalloc byte*[6];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 6; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 6; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pEntity = (Entity*)(chunk.GetUnsafePtr() + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));

                    action(*pEntity, ref *pComp0,ref *pComp1,ref *pComp2,ref *pComp3,ref *pComp4,ref *pComp5);
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value, ComponentTypeID<T3>.value, ComponentTypeID<T4>.value, ComponentTypeID<T5>.value, ComponentTypeID<T6>.value };
        var offsets = stackalloc int[7];
        var basePtrs = stackalloc byte*[7];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 7; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 7; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pEntity = (Entity*)(chunk.GetUnsafePtr() + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));
                    var pComp6 = (T6*)(basePtrs[6] + (sizeof(T6) * entityIndex));

                    action(*pEntity, ref *pComp0,ref *pComp1,ref *pComp2,ref *pComp3,ref *pComp4,ref *pComp5,ref *pComp6);
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
        var world = World.GetWorld(_worldID).GetValueOrThrow(ResultStatus.Success);

        var compTypeIDs = stackalloc int[] { ComponentTypeID<T0>.value, ComponentTypeID<T1>.value, ComponentTypeID<T2>.value, ComponentTypeID<T3>.value, ComponentTypeID<T4>.value, ComponentTypeID<T5>.value, ComponentTypeID<T6>.value, ComponentTypeID<T7>.value };
        var offsets = stackalloc int[8];
        var basePtrs = stackalloc byte*[8];

        foreach (var archetypeID in _matchingArchetypes)
        {
            ref var archetype = ref world.GetArchetypeReference(archetypeID);
            var hasAllComponents = true;
            for (var index = 0; index < 8; index++)
            {
                offsets[index] = archetype.GetOffset(compTypeIDs[index]);
                if (offsets[index] == -1)
                {
                    hasAllComponents = false;
                    break;
                }
            }

            if (!hasAllComponents)
            {
                continue;
            }

            for (var chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref var chunk = ref archetype.GetChunkReference(chunkIndex);
                var count = chunk.Count;
                for (var index = 0; index < 8; index++)
                {
                    basePtrs[index] = chunk.GetUnsafePtr() + offsets[index];
                }

                for (var entityIndex = 0; entityIndex < count; entityIndex++)
                {
                    var pEntity = (Entity*)(chunk.GetUnsafePtr() + archetype.EntityIDsOffset + (sizeof(Entity) * entityIndex));
                    var pComp0 = (T0*)(basePtrs[0] + (sizeof(T0) * entityIndex));
                    var pComp1 = (T1*)(basePtrs[1] + (sizeof(T1) * entityIndex));
                    var pComp2 = (T2*)(basePtrs[2] + (sizeof(T2) * entityIndex));
                    var pComp3 = (T3*)(basePtrs[3] + (sizeof(T3) * entityIndex));
                    var pComp4 = (T4*)(basePtrs[4] + (sizeof(T4) * entityIndex));
                    var pComp5 = (T5*)(basePtrs[5] + (sizeof(T5) * entityIndex));
                    var pComp6 = (T6*)(basePtrs[6] + (sizeof(T6) * entityIndex));
                    var pComp7 = (T7*)(basePtrs[7] + (sizeof(T7) * entityIndex));

                    action(*pEntity, ref *pComp0,ref *pComp1,ref *pComp2,ref *pComp3,ref *pComp4,ref *pComp5,ref *pComp6,ref *pComp7);
                }
            }
        }
    }

}