namespace Ghost.ArcEntities;

public unsafe class EntityQuery<T1, T2>
    where T1 : unmanaged, IComponent
    where T2 : unmanaged, IComponent
{
    // The Cache Struct
    struct ArchetypeCache
    {
        public Archetype Archetype;
        public int Offset1; // Offset for T1
        public int Offset2; // Offset for T2
    }

    private List<ArchetypeCache> _cache = new();

    internal void AddMatchingArchetype(Archetype archetype)
    {
        // We look up the offsets ONCE when the archetype is registered
        int off1 = archetype.GetOffset(ComponentTypeID<T1>.value);
        int off2 = archetype.GetOffset(ComponentTypeID<T2>.value);

        _cache.Add(new ArchetypeCache
        {
            Archetype = archetype,
            Offset1 = off1,
            Offset2 = off2
        });
    }

    // The Optimized Iteration Loop
    public void ForEach(delegate*<ref T1, ref T2, void> action)
    {
        foreach (var cache in _cache)
        {
            var archetype = cache.Archetype;
            var offset1 = cache.Offset1;
            var offset2 = cache.Offset2;

            // Iterate Chunks
            for (int i = 0; i < archetype.ChunkCount; i++)
            {
                var chunk = archetype.GetChunkReference(i);
                var chunkPtr = (byte*)chunk.GetUnsafePtr();
                var count = chunk.Count;

                // POINTER MATH ONLY - NO LOOKUPS
                // We use the pre-calculated integer offsets
                T1* ptr1 = (T1*)(chunkPtr + offset1);
                T2* ptr2 = (T2*)(chunkPtr + offset2);

                // The hot loop
                for (int k = 0; k < count; k++)
                {
                    action(ref ptr1[k], ref ptr2[k]);
                }
            }
        }
    }
}
