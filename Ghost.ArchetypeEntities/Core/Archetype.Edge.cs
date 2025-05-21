using Misaki.HighPerformance.Unsafe.Collections;

namespace Ghost.Entities.Core;
internal partial struct Archetype : IEquatable<Archetype>
{
    public void AddInsertionEdge(Archetype archetype)
    {
        if (_insertionEdges.IsCreated)
        {
            _insertionEdges = new UnsafeHashSet<Archetype>(_BUCKET_SIZE, Allocator.Persistent);
        }
        _insertionEdges.Add(archetype);
    }

    public readonly bool Equals(Archetype other)
    {
        return signature.Equals(other.signature);
    }
}