using Ghost.Core;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public unsafe partial struct EntityQuery
{
    public readonly ref struct ComponentIterator<T0>
        where T0 : unmanaged, IComponent
    {
        public ref struct Enumerator : IDisposable
        {
            private fixed int _compTypeIDs[1];
            private fixed int _offsets[1];
            private fixed long _compBasePtrs[1];

            private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
            private readonly EntityQueryMask _mask;
            private readonly World _world;

            private readonly VirtualStack.Scope _scope;
            private UnsafeList<int> _changedComponentIDs;

            private int _reqCount;
            private fixed int _reqOffsets[16];
            private int _reqDisCount;
            private fixed int _reqDisOffsets[16];
            private int _rejCount;
            private fixed int _rejOffsets[16];
            private bool _requiresFiltering;

            private ref Archetype _currentArchetype;
            private ref Chunk _currentChunk;
            private byte* _chunkBasePtr;

            private int _currentChunkEntityCount;
            private int _currentArchetypeIndex;
            private int _currentChunkIndex;
            private int _currentEntityIndex;

            internal Enumerator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
            {
                _compTypeIDs[0] = ComponentTypeID<T0>.Value;
                _offsets[0] = 0;
                _compBasePtrs[0] = 0;

                _matchingArchetypes = matchingArchetypes;
                _mask = mask;
                _world = world;

                _scope = AllocationManager.CreateStackScope();
                _changedComponentIDs = new UnsafeList<int>(1, _scope.AllocationHandle);

                var it = _mask.writeAccess.GetIterator();
                while (it.Next(out var id))
                {
                    for (var i = 0; i < 1; i++)
                    {
                        if (id == _compTypeIDs[i])
                        {
                            _changedComponentIDs.Add(id);
                            break;
                        }
                    }
                }

                Reset();
            }

            public ref T0 Current => ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)_compBasePtrs[0])[0] : ref ((T0*)_compBasePtrs[0])[_currentEntityIndex]);

            private void SetArchetype(int index)
            {
                _currentArchetypeIndex = index;
                _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[index]);

                _requiresFiltering = RequiresEnableableFiltering(in _currentArchetype, in _mask);
                if (_requiresFiltering)
                {
                    _reqCount = 0;
                    var itE = _mask.requireEnabled.GetIterator();
                    while (itE.Next(out var id) && _reqCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqOffsets[_reqCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _reqDisCount = 0;
                    itE = _mask.requireDisabled.GetIterator();
                    while (itE.Next(out var id) && _reqDisCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqDisOffsets[_reqDisCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _rejCount = 0;
                    itE = _mask.rejectIfEnabled.GetIterator();
                    while (itE.Next(out var id) && _rejCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _rejOffsets[_rejCount++] = layoutResult.Value.enableBitsOffset;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                if (ComponentTypeID<T0>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[0] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[0];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    _compBasePtrs[0] = (long)(_chunkBasePtr + _offsets[0]);
                }

                for (var i = 0; i < _changedComponentIDs.Count; i++)
                {
                    _currentArchetype.MarkChanged(_currentChunkIndex, _changedComponentIDs[i], _world.Version);
                }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    _currentEntityIndex++;
                    if (_currentEntityIndex < _currentChunk._count)
                    {
                        var pChunkData = _currentChunk.GetUnsafePtr();
                        if (!_requiresFiltering)
                        {
                            return true;
                        }

                        var valid = true;
                        for (var h = 0; h < _reqCount; h++)
                        {
                            if (!CheckBit(pChunkData + _reqOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _reqDisCount; h++)
                        {
                            if (CheckBit(pChunkData + _reqDisOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _rejCount; h++)
                        {
                            if (CheckBit(pChunkData + _rejOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (valid) return true;
                    }

                    _currentChunkIndex++;
                    if (!Unsafe.IsNullRef(ref _currentArchetype) && _currentChunkIndex < _currentArchetype.ChunkCount)
                    {
                        SetChunk(_currentChunkIndex);
                        _currentEntityIndex = -1; // Reset for new chunk

                        continue;
                    }

                    _currentArchetypeIndex++;
                    if (_currentArchetypeIndex < _matchingArchetypes.Count)
                    {
                        SetArchetype(_currentArchetypeIndex);

                        _currentChunkIndex = 0;
                        if (_currentArchetype.ChunkCount > 0)
                        {
                            SetChunk(0);
                            _currentEntityIndex = -1;
                            continue;
                        }

                        // If archetype has no chunks, loop will try next archetype
                    }
                    else
                    {
                        return false; // End of all data
                    }
                }
            }

            public void Reset()
            {
                _currentArchetype = ref Unsafe.NullRef<Archetype>();
                _currentChunk = ref Unsafe.NullRef<Chunk>();
                _currentArchetypeIndex = 0;
                _currentChunkIndex = 0;
                _currentEntityIndex = -1;

                if (_matchingArchetypes.Count > 0)
                {
                    SetArchetype(0);
                    if (_currentArchetype.ChunkCount > 0)
                    {
                        SetChunk(0);
                    }
                }
            }

            public readonly void Dispose()
            {
                _changedComponentIDs.Dispose();
                _scope.Dispose();
            }
        }

        private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
        private readonly EntityQueryMask _mask;
        private readonly World _world;

        internal ComponentIterator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
        {
            _matchingArchetypes = matchingArchetypes;
            _mask = mask;
            _world = world;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_matchingArchetypes, _mask, _world);
        }
    }

    public readonly ComponentIterator<T0> GetComponentIterator<T0>()
        where T0 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return default;
        }

        return new ComponentIterator<T0>(_matchingArchetypes.AsReadOnly(), _mask, world);
    }

    public readonly ref struct ComponentIterator<T0, T1>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        public ref struct QueryItem
        {
            public ref T0 component0;
            public ref T1 component1;
            internal QueryItem(ref T0 component0, ref T1 component1)
            {
                this.component0 = ref component0;
                this.component1 = ref component1;
            }

            public void Deconstruct(out Ref<T0> component0, out Ref<T1> component1)
            {
                component0 = new Ref<T0>(ref this.component0);
                component1 = new Ref<T1>(ref this.component1);
            }
        }

        public ref struct Enumerator : IDisposable
        {
            private fixed int _compTypeIDs[2];
            private fixed int _offsets[2];
            private fixed long _compBasePtrs[2];

            private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
            private readonly EntityQueryMask _mask;
            private readonly World _world;

            private readonly VirtualStack.Scope _scope;
            private UnsafeList<int> _changedComponentIDs;

            private int _reqCount;
            private fixed int _reqOffsets[16];
            private int _reqDisCount;
            private fixed int _reqDisOffsets[16];
            private int _rejCount;
            private fixed int _rejOffsets[16];
            private bool _requiresFiltering;

            private ref Archetype _currentArchetype;
            private ref Chunk _currentChunk;
            private byte* _chunkBasePtr;

            private int _currentChunkEntityCount;
            private int _currentArchetypeIndex;
            private int _currentChunkIndex;
            private int _currentEntityIndex;

            internal Enumerator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
            {
                _compTypeIDs[0] = ComponentTypeID<T0>.Value;
                _offsets[0] = 0;
                _compBasePtrs[0] = 0;

                _compTypeIDs[1] = ComponentTypeID<T1>.Value;
                _offsets[1] = 0;
                _compBasePtrs[1] = 0;

                _matchingArchetypes = matchingArchetypes;
                _mask = mask;
                _world = world;

                _scope = AllocationManager.CreateStackScope();
                _changedComponentIDs = new UnsafeList<int>(2, _scope.AllocationHandle);

                var it = _mask.writeAccess.GetIterator();
                while (it.Next(out var id))
                {
                    for (var i = 0; i < 2; i++)
                    {
                        if (id == _compTypeIDs[i])
                        {
                            _changedComponentIDs.Add(id);
                            break;
                        }
                    }
                }

                Reset();
            }

            public QueryItem Current => new(
                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)_compBasePtrs[0])[0] : ref ((T0*)_compBasePtrs[0])[_currentEntityIndex]),
                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)_compBasePtrs[1])[0] : ref ((T1*)_compBasePtrs[1])[_currentEntityIndex])
            );

            private void SetArchetype(int index)
            {
                _currentArchetypeIndex = index;
                _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[index]);

                _requiresFiltering = RequiresEnableableFiltering(in _currentArchetype, in _mask);
                if (_requiresFiltering)
                {
                    _reqCount = 0;
                    var itE = _mask.requireEnabled.GetIterator();
                    while (itE.Next(out var id) && _reqCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqOffsets[_reqCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _reqDisCount = 0;
                    itE = _mask.requireDisabled.GetIterator();
                    while (itE.Next(out var id) && _reqDisCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqDisOffsets[_reqDisCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _rejCount = 0;
                    itE = _mask.rejectIfEnabled.GetIterator();
                    while (itE.Next(out var id) && _rejCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _rejOffsets[_rejCount++] = layoutResult.Value.enableBitsOffset;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                if (ComponentTypeID<T0>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[0] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[0];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    _compBasePtrs[0] = (long)(_chunkBasePtr + _offsets[0]);
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[1] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[1];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    _compBasePtrs[1] = (long)(_chunkBasePtr + _offsets[1]);
                }

                for (var i = 0; i < _changedComponentIDs.Count; i++)
                {
                    _currentArchetype.MarkChanged(_currentChunkIndex, _changedComponentIDs[i], _world.Version);
                }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    _currentEntityIndex++;
                    if (_currentEntityIndex < _currentChunk._count)
                    {
                        var pChunkData = _currentChunk.GetUnsafePtr();
                        if (!_requiresFiltering)
                        {
                            return true;
                        }

                        var valid = true;
                        for (var h = 0; h < _reqCount; h++)
                        {
                            if (!CheckBit(pChunkData + _reqOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _reqDisCount; h++)
                        {
                            if (CheckBit(pChunkData + _reqDisOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _rejCount; h++)
                        {
                            if (CheckBit(pChunkData + _rejOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (valid) return true;
                    }

                    _currentChunkIndex++;
                    if (!Unsafe.IsNullRef(ref _currentArchetype) && _currentChunkIndex < _currentArchetype.ChunkCount)
                    {
                        SetChunk(_currentChunkIndex);
                        _currentEntityIndex = -1; // Reset for new chunk

                        continue;
                    }

                    _currentArchetypeIndex++;
                    if (_currentArchetypeIndex < _matchingArchetypes.Count)
                    {
                        SetArchetype(_currentArchetypeIndex);

                        _currentChunkIndex = 0;
                        if (_currentArchetype.ChunkCount > 0)
                        {
                            SetChunk(0);
                            _currentEntityIndex = -1;
                            continue;
                        }

                        // If archetype has no chunks, loop will try next archetype
                    }
                    else
                    {
                        return false; // End of all data
                    }
                }
            }

            public void Reset()
            {
                _currentArchetype = ref Unsafe.NullRef<Archetype>();
                _currentChunk = ref Unsafe.NullRef<Chunk>();
                _currentArchetypeIndex = 0;
                _currentChunkIndex = 0;
                _currentEntityIndex = -1;

                if (_matchingArchetypes.Count > 0)
                {
                    SetArchetype(0);
                    if (_currentArchetype.ChunkCount > 0)
                    {
                        SetChunk(0);
                    }
                }
            }

            public readonly void Dispose()
            {
                _changedComponentIDs.Dispose();
                _scope.Dispose();
            }
        }

        private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
        private readonly EntityQueryMask _mask;
        private readonly World _world;

        internal ComponentIterator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
        {
            _matchingArchetypes = matchingArchetypes;
            _mask = mask;
            _world = world;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_matchingArchetypes, _mask, _world);
        }
    }

    public readonly ComponentIterator<T0, T1> GetComponentIterator<T0, T1>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return default;
        }

        return new ComponentIterator<T0, T1>(_matchingArchetypes.AsReadOnly(), _mask, world);
    }

    public readonly ref struct ComponentIterator<T0, T1, T2>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        public ref struct QueryItem
        {
            public ref T0 component0;
            public ref T1 component1;
            public ref T2 component2;
            internal QueryItem(ref T0 component0, ref T1 component1, ref T2 component2)
            {
                this.component0 = ref component0;
                this.component1 = ref component1;
                this.component2 = ref component2;
            }

            public void Deconstruct(out Ref<T0> component0, out Ref<T1> component1, out Ref<T2> component2)
            {
                component0 = new Ref<T0>(ref this.component0);
                component1 = new Ref<T1>(ref this.component1);
                component2 = new Ref<T2>(ref this.component2);
            }
        }

        public ref struct Enumerator : IDisposable
        {
            private fixed int _compTypeIDs[3];
            private fixed int _offsets[3];
            private fixed long _compBasePtrs[3];

            private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
            private readonly EntityQueryMask _mask;
            private readonly World _world;

            private readonly VirtualStack.Scope _scope;
            private UnsafeList<int> _changedComponentIDs;

            private int _reqCount;
            private fixed int _reqOffsets[16];
            private int _reqDisCount;
            private fixed int _reqDisOffsets[16];
            private int _rejCount;
            private fixed int _rejOffsets[16];
            private bool _requiresFiltering;

            private ref Archetype _currentArchetype;
            private ref Chunk _currentChunk;
            private byte* _chunkBasePtr;

            private int _currentChunkEntityCount;
            private int _currentArchetypeIndex;
            private int _currentChunkIndex;
            private int _currentEntityIndex;

            internal Enumerator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
            {
                _compTypeIDs[0] = ComponentTypeID<T0>.Value;
                _offsets[0] = 0;
                _compBasePtrs[0] = 0;

                _compTypeIDs[1] = ComponentTypeID<T1>.Value;
                _offsets[1] = 0;
                _compBasePtrs[1] = 0;

                _compTypeIDs[2] = ComponentTypeID<T2>.Value;
                _offsets[2] = 0;
                _compBasePtrs[2] = 0;

                _matchingArchetypes = matchingArchetypes;
                _mask = mask;
                _world = world;

                _scope = AllocationManager.CreateStackScope();
                _changedComponentIDs = new UnsafeList<int>(3, _scope.AllocationHandle);

                var it = _mask.writeAccess.GetIterator();
                while (it.Next(out var id))
                {
                    for (var i = 0; i < 3; i++)
                    {
                        if (id == _compTypeIDs[i])
                        {
                            _changedComponentIDs.Add(id);
                            break;
                        }
                    }
                }

                Reset();
            }

            public QueryItem Current => new(
                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)_compBasePtrs[0])[0] : ref ((T0*)_compBasePtrs[0])[_currentEntityIndex]),
                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)_compBasePtrs[1])[0] : ref ((T1*)_compBasePtrs[1])[_currentEntityIndex]),
                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)_compBasePtrs[2])[0] : ref ((T2*)_compBasePtrs[2])[_currentEntityIndex])
            );

            private void SetArchetype(int index)
            {
                _currentArchetypeIndex = index;
                _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[index]);

                _requiresFiltering = RequiresEnableableFiltering(in _currentArchetype, in _mask);
                if (_requiresFiltering)
                {
                    _reqCount = 0;
                    var itE = _mask.requireEnabled.GetIterator();
                    while (itE.Next(out var id) && _reqCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqOffsets[_reqCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _reqDisCount = 0;
                    itE = _mask.requireDisabled.GetIterator();
                    while (itE.Next(out var id) && _reqDisCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqDisOffsets[_reqDisCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _rejCount = 0;
                    itE = _mask.rejectIfEnabled.GetIterator();
                    while (itE.Next(out var id) && _rejCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _rejOffsets[_rejCount++] = layoutResult.Value.enableBitsOffset;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                if (ComponentTypeID<T0>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[0] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[0];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    _compBasePtrs[0] = (long)(_chunkBasePtr + _offsets[0]);
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[1] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[1];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    _compBasePtrs[1] = (long)(_chunkBasePtr + _offsets[1]);
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[2] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[2];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    _compBasePtrs[2] = (long)(_chunkBasePtr + _offsets[2]);
                }

                for (var i = 0; i < _changedComponentIDs.Count; i++)
                {
                    _currentArchetype.MarkChanged(_currentChunkIndex, _changedComponentIDs[i], _world.Version);
                }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    _currentEntityIndex++;
                    if (_currentEntityIndex < _currentChunk._count)
                    {
                        var pChunkData = _currentChunk.GetUnsafePtr();
                        if (!_requiresFiltering)
                        {
                            return true;
                        }

                        var valid = true;
                        for (var h = 0; h < _reqCount; h++)
                        {
                            if (!CheckBit(pChunkData + _reqOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _reqDisCount; h++)
                        {
                            if (CheckBit(pChunkData + _reqDisOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _rejCount; h++)
                        {
                            if (CheckBit(pChunkData + _rejOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (valid) return true;
                    }

                    _currentChunkIndex++;
                    if (!Unsafe.IsNullRef(ref _currentArchetype) && _currentChunkIndex < _currentArchetype.ChunkCount)
                    {
                        SetChunk(_currentChunkIndex);
                        _currentEntityIndex = -1; // Reset for new chunk

                        continue;
                    }

                    _currentArchetypeIndex++;
                    if (_currentArchetypeIndex < _matchingArchetypes.Count)
                    {
                        SetArchetype(_currentArchetypeIndex);

                        _currentChunkIndex = 0;
                        if (_currentArchetype.ChunkCount > 0)
                        {
                            SetChunk(0);
                            _currentEntityIndex = -1;
                            continue;
                        }

                        // If archetype has no chunks, loop will try next archetype
                    }
                    else
                    {
                        return false; // End of all data
                    }
                }
            }

            public void Reset()
            {
                _currentArchetype = ref Unsafe.NullRef<Archetype>();
                _currentChunk = ref Unsafe.NullRef<Chunk>();
                _currentArchetypeIndex = 0;
                _currentChunkIndex = 0;
                _currentEntityIndex = -1;

                if (_matchingArchetypes.Count > 0)
                {
                    SetArchetype(0);
                    if (_currentArchetype.ChunkCount > 0)
                    {
                        SetChunk(0);
                    }
                }
            }

            public readonly void Dispose()
            {
                _changedComponentIDs.Dispose();
                _scope.Dispose();
            }
        }

        private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
        private readonly EntityQueryMask _mask;
        private readonly World _world;

        internal ComponentIterator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
        {
            _matchingArchetypes = matchingArchetypes;
            _mask = mask;
            _world = world;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_matchingArchetypes, _mask, _world);
        }
    }

    public readonly ComponentIterator<T0, T1, T2> GetComponentIterator<T0, T1, T2>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return default;
        }

        return new ComponentIterator<T0, T1, T2>(_matchingArchetypes.AsReadOnly(), _mask, world);
    }

    public readonly ref struct ComponentIterator<T0, T1, T2, T3>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        public ref struct QueryItem
        {
            public ref T0 component0;
            public ref T1 component1;
            public ref T2 component2;
            public ref T3 component3;
            internal QueryItem(ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3)
            {
                this.component0 = ref component0;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
            }

            public void Deconstruct(out Ref<T0> component0, out Ref<T1> component1, out Ref<T2> component2, out Ref<T3> component3)
            {
                component0 = new Ref<T0>(ref this.component0);
                component1 = new Ref<T1>(ref this.component1);
                component2 = new Ref<T2>(ref this.component2);
                component3 = new Ref<T3>(ref this.component3);
            }
        }

        public ref struct Enumerator : IDisposable
        {
            private fixed int _compTypeIDs[4];
            private fixed int _offsets[4];
            private fixed long _compBasePtrs[4];

            private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
            private readonly EntityQueryMask _mask;
            private readonly World _world;

            private readonly VirtualStack.Scope _scope;
            private UnsafeList<int> _changedComponentIDs;

            private int _reqCount;
            private fixed int _reqOffsets[16];
            private int _reqDisCount;
            private fixed int _reqDisOffsets[16];
            private int _rejCount;
            private fixed int _rejOffsets[16];
            private bool _requiresFiltering;

            private ref Archetype _currentArchetype;
            private ref Chunk _currentChunk;
            private byte* _chunkBasePtr;

            private int _currentChunkEntityCount;
            private int _currentArchetypeIndex;
            private int _currentChunkIndex;
            private int _currentEntityIndex;

            internal Enumerator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
            {
                _compTypeIDs[0] = ComponentTypeID<T0>.Value;
                _offsets[0] = 0;
                _compBasePtrs[0] = 0;

                _compTypeIDs[1] = ComponentTypeID<T1>.Value;
                _offsets[1] = 0;
                _compBasePtrs[1] = 0;

                _compTypeIDs[2] = ComponentTypeID<T2>.Value;
                _offsets[2] = 0;
                _compBasePtrs[2] = 0;

                _compTypeIDs[3] = ComponentTypeID<T3>.Value;
                _offsets[3] = 0;
                _compBasePtrs[3] = 0;

                _matchingArchetypes = matchingArchetypes;
                _mask = mask;
                _world = world;

                _scope = AllocationManager.CreateStackScope();
                _changedComponentIDs = new UnsafeList<int>(4, _scope.AllocationHandle);

                var it = _mask.writeAccess.GetIterator();
                while (it.Next(out var id))
                {
                    for (var i = 0; i < 4; i++)
                    {
                        if (id == _compTypeIDs[i])
                        {
                            _changedComponentIDs.Add(id);
                            break;
                        }
                    }
                }

                Reset();
            }

            public QueryItem Current => new(
                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)_compBasePtrs[0])[0] : ref ((T0*)_compBasePtrs[0])[_currentEntityIndex]),
                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)_compBasePtrs[1])[0] : ref ((T1*)_compBasePtrs[1])[_currentEntityIndex]),
                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)_compBasePtrs[2])[0] : ref ((T2*)_compBasePtrs[2])[_currentEntityIndex]),
                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)_compBasePtrs[3])[0] : ref ((T3*)_compBasePtrs[3])[_currentEntityIndex])
            );

            private void SetArchetype(int index)
            {
                _currentArchetypeIndex = index;
                _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[index]);

                _requiresFiltering = RequiresEnableableFiltering(in _currentArchetype, in _mask);
                if (_requiresFiltering)
                {
                    _reqCount = 0;
                    var itE = _mask.requireEnabled.GetIterator();
                    while (itE.Next(out var id) && _reqCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqOffsets[_reqCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _reqDisCount = 0;
                    itE = _mask.requireDisabled.GetIterator();
                    while (itE.Next(out var id) && _reqDisCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqDisOffsets[_reqDisCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _rejCount = 0;
                    itE = _mask.rejectIfEnabled.GetIterator();
                    while (itE.Next(out var id) && _rejCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _rejOffsets[_rejCount++] = layoutResult.Value.enableBitsOffset;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                if (ComponentTypeID<T0>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[0] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[0];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    _compBasePtrs[0] = (long)(_chunkBasePtr + _offsets[0]);
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[1] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[1];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    _compBasePtrs[1] = (long)(_chunkBasePtr + _offsets[1]);
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[2] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[2];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    _compBasePtrs[2] = (long)(_chunkBasePtr + _offsets[2]);
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[3]).GetValueOrThrow();
                    _offsets[3] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[3] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[3];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[3]).GetValueOrThrow();
                    _offsets[3] = layout.offset;
                    _compBasePtrs[3] = (long)(_chunkBasePtr + _offsets[3]);
                }

                for (var i = 0; i < _changedComponentIDs.Count; i++)
                {
                    _currentArchetype.MarkChanged(_currentChunkIndex, _changedComponentIDs[i], _world.Version);
                }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    _currentEntityIndex++;
                    if (_currentEntityIndex < _currentChunk._count)
                    {
                        var pChunkData = _currentChunk.GetUnsafePtr();
                        if (!_requiresFiltering)
                        {
                            return true;
                        }

                        var valid = true;
                        for (var h = 0; h < _reqCount; h++)
                        {
                            if (!CheckBit(pChunkData + _reqOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _reqDisCount; h++)
                        {
                            if (CheckBit(pChunkData + _reqDisOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _rejCount; h++)
                        {
                            if (CheckBit(pChunkData + _rejOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (valid) return true;
                    }

                    _currentChunkIndex++;
                    if (!Unsafe.IsNullRef(ref _currentArchetype) && _currentChunkIndex < _currentArchetype.ChunkCount)
                    {
                        SetChunk(_currentChunkIndex);
                        _currentEntityIndex = -1; // Reset for new chunk

                        continue;
                    }

                    _currentArchetypeIndex++;
                    if (_currentArchetypeIndex < _matchingArchetypes.Count)
                    {
                        SetArchetype(_currentArchetypeIndex);

                        _currentChunkIndex = 0;
                        if (_currentArchetype.ChunkCount > 0)
                        {
                            SetChunk(0);
                            _currentEntityIndex = -1;
                            continue;
                        }

                        // If archetype has no chunks, loop will try next archetype
                    }
                    else
                    {
                        return false; // End of all data
                    }
                }
            }

            public void Reset()
            {
                _currentArchetype = ref Unsafe.NullRef<Archetype>();
                _currentChunk = ref Unsafe.NullRef<Chunk>();
                _currentArchetypeIndex = 0;
                _currentChunkIndex = 0;
                _currentEntityIndex = -1;

                if (_matchingArchetypes.Count > 0)
                {
                    SetArchetype(0);
                    if (_currentArchetype.ChunkCount > 0)
                    {
                        SetChunk(0);
                    }
                }
            }

            public readonly void Dispose()
            {
                _changedComponentIDs.Dispose();
                _scope.Dispose();
            }
        }

        private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
        private readonly EntityQueryMask _mask;
        private readonly World _world;

        internal ComponentIterator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
        {
            _matchingArchetypes = matchingArchetypes;
            _mask = mask;
            _world = world;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_matchingArchetypes, _mask, _world);
        }
    }

    public readonly ComponentIterator<T0, T1, T2, T3> GetComponentIterator<T0, T1, T2, T3>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return default;
        }

        return new ComponentIterator<T0, T1, T2, T3>(_matchingArchetypes.AsReadOnly(), _mask, world);
    }

    public readonly ref struct ComponentIterator<T0, T1, T2, T3, T4>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        public ref struct QueryItem
        {
            public ref T0 component0;
            public ref T1 component1;
            public ref T2 component2;
            public ref T3 component3;
            public ref T4 component4;
            internal QueryItem(ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4)
            {
                this.component0 = ref component0;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
            }

            public void Deconstruct(out Ref<T0> component0, out Ref<T1> component1, out Ref<T2> component2, out Ref<T3> component3, out Ref<T4> component4)
            {
                component0 = new Ref<T0>(ref this.component0);
                component1 = new Ref<T1>(ref this.component1);
                component2 = new Ref<T2>(ref this.component2);
                component3 = new Ref<T3>(ref this.component3);
                component4 = new Ref<T4>(ref this.component4);
            }
        }

        public ref struct Enumerator : IDisposable
        {
            private fixed int _compTypeIDs[5];
            private fixed int _offsets[5];
            private fixed long _compBasePtrs[5];

            private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
            private readonly EntityQueryMask _mask;
            private readonly World _world;

            private readonly VirtualStack.Scope _scope;
            private UnsafeList<int> _changedComponentIDs;

            private int _reqCount;
            private fixed int _reqOffsets[16];
            private int _reqDisCount;
            private fixed int _reqDisOffsets[16];
            private int _rejCount;
            private fixed int _rejOffsets[16];
            private bool _requiresFiltering;

            private ref Archetype _currentArchetype;
            private ref Chunk _currentChunk;
            private byte* _chunkBasePtr;

            private int _currentChunkEntityCount;
            private int _currentArchetypeIndex;
            private int _currentChunkIndex;
            private int _currentEntityIndex;

            internal Enumerator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
            {
                _compTypeIDs[0] = ComponentTypeID<T0>.Value;
                _offsets[0] = 0;
                _compBasePtrs[0] = 0;

                _compTypeIDs[1] = ComponentTypeID<T1>.Value;
                _offsets[1] = 0;
                _compBasePtrs[1] = 0;

                _compTypeIDs[2] = ComponentTypeID<T2>.Value;
                _offsets[2] = 0;
                _compBasePtrs[2] = 0;

                _compTypeIDs[3] = ComponentTypeID<T3>.Value;
                _offsets[3] = 0;
                _compBasePtrs[3] = 0;

                _compTypeIDs[4] = ComponentTypeID<T4>.Value;
                _offsets[4] = 0;
                _compBasePtrs[4] = 0;

                _matchingArchetypes = matchingArchetypes;
                _mask = mask;
                _world = world;

                _scope = AllocationManager.CreateStackScope();
                _changedComponentIDs = new UnsafeList<int>(5, _scope.AllocationHandle);

                var it = _mask.writeAccess.GetIterator();
                while (it.Next(out var id))
                {
                    for (var i = 0; i < 5; i++)
                    {
                        if (id == _compTypeIDs[i])
                        {
                            _changedComponentIDs.Add(id);
                            break;
                        }
                    }
                }

                Reset();
            }

            public QueryItem Current => new(
                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)_compBasePtrs[0])[0] : ref ((T0*)_compBasePtrs[0])[_currentEntityIndex]),
                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)_compBasePtrs[1])[0] : ref ((T1*)_compBasePtrs[1])[_currentEntityIndex]),
                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)_compBasePtrs[2])[0] : ref ((T2*)_compBasePtrs[2])[_currentEntityIndex]),
                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)_compBasePtrs[3])[0] : ref ((T3*)_compBasePtrs[3])[_currentEntityIndex]),
                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)_compBasePtrs[4])[0] : ref ((T4*)_compBasePtrs[4])[_currentEntityIndex])
            );

            private void SetArchetype(int index)
            {
                _currentArchetypeIndex = index;
                _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[index]);

                _requiresFiltering = RequiresEnableableFiltering(in _currentArchetype, in _mask);
                if (_requiresFiltering)
                {
                    _reqCount = 0;
                    var itE = _mask.requireEnabled.GetIterator();
                    while (itE.Next(out var id) && _reqCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqOffsets[_reqCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _reqDisCount = 0;
                    itE = _mask.requireDisabled.GetIterator();
                    while (itE.Next(out var id) && _reqDisCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqDisOffsets[_reqDisCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _rejCount = 0;
                    itE = _mask.rejectIfEnabled.GetIterator();
                    while (itE.Next(out var id) && _rejCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _rejOffsets[_rejCount++] = layoutResult.Value.enableBitsOffset;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                if (ComponentTypeID<T0>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[0] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[0];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    _compBasePtrs[0] = (long)(_chunkBasePtr + _offsets[0]);
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[1] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[1];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    _compBasePtrs[1] = (long)(_chunkBasePtr + _offsets[1]);
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[2] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[2];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    _compBasePtrs[2] = (long)(_chunkBasePtr + _offsets[2]);
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[3]).GetValueOrThrow();
                    _offsets[3] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[3] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[3];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[3]).GetValueOrThrow();
                    _offsets[3] = layout.offset;
                    _compBasePtrs[3] = (long)(_chunkBasePtr + _offsets[3]);
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[4]).GetValueOrThrow();
                    _offsets[4] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[4] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[4];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[4]).GetValueOrThrow();
                    _offsets[4] = layout.offset;
                    _compBasePtrs[4] = (long)(_chunkBasePtr + _offsets[4]);
                }

                for (var i = 0; i < _changedComponentIDs.Count; i++)
                {
                    _currentArchetype.MarkChanged(_currentChunkIndex, _changedComponentIDs[i], _world.Version);
                }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    _currentEntityIndex++;
                    if (_currentEntityIndex < _currentChunk._count)
                    {
                        var pChunkData = _currentChunk.GetUnsafePtr();
                        if (!_requiresFiltering)
                        {
                            return true;
                        }

                        var valid = true;
                        for (var h = 0; h < _reqCount; h++)
                        {
                            if (!CheckBit(pChunkData + _reqOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _reqDisCount; h++)
                        {
                            if (CheckBit(pChunkData + _reqDisOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _rejCount; h++)
                        {
                            if (CheckBit(pChunkData + _rejOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (valid) return true;
                    }

                    _currentChunkIndex++;
                    if (!Unsafe.IsNullRef(ref _currentArchetype) && _currentChunkIndex < _currentArchetype.ChunkCount)
                    {
                        SetChunk(_currentChunkIndex);
                        _currentEntityIndex = -1; // Reset for new chunk

                        continue;
                    }

                    _currentArchetypeIndex++;
                    if (_currentArchetypeIndex < _matchingArchetypes.Count)
                    {
                        SetArchetype(_currentArchetypeIndex);

                        _currentChunkIndex = 0;
                        if (_currentArchetype.ChunkCount > 0)
                        {
                            SetChunk(0);
                            _currentEntityIndex = -1;
                            continue;
                        }

                        // If archetype has no chunks, loop will try next archetype
                    }
                    else
                    {
                        return false; // End of all data
                    }
                }
            }

            public void Reset()
            {
                _currentArchetype = ref Unsafe.NullRef<Archetype>();
                _currentChunk = ref Unsafe.NullRef<Chunk>();
                _currentArchetypeIndex = 0;
                _currentChunkIndex = 0;
                _currentEntityIndex = -1;

                if (_matchingArchetypes.Count > 0)
                {
                    SetArchetype(0);
                    if (_currentArchetype.ChunkCount > 0)
                    {
                        SetChunk(0);
                    }
                }
            }

            public readonly void Dispose()
            {
                _changedComponentIDs.Dispose();
                _scope.Dispose();
            }
        }

        private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
        private readonly EntityQueryMask _mask;
        private readonly World _world;

        internal ComponentIterator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
        {
            _matchingArchetypes = matchingArchetypes;
            _mask = mask;
            _world = world;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_matchingArchetypes, _mask, _world);
        }
    }

    public readonly ComponentIterator<T0, T1, T2, T3, T4> GetComponentIterator<T0, T1, T2, T3, T4>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return default;
        }

        return new ComponentIterator<T0, T1, T2, T3, T4>(_matchingArchetypes.AsReadOnly(), _mask, world);
    }

    public readonly ref struct ComponentIterator<T0, T1, T2, T3, T4, T5>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
    {
        public ref struct QueryItem
        {
            public ref T0 component0;
            public ref T1 component1;
            public ref T2 component2;
            public ref T3 component3;
            public ref T4 component4;
            public ref T5 component5;
            internal QueryItem(ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5)
            {
                this.component0 = ref component0;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
            }

            public void Deconstruct(out Ref<T0> component0, out Ref<T1> component1, out Ref<T2> component2, out Ref<T3> component3, out Ref<T4> component4, out Ref<T5> component5)
            {
                component0 = new Ref<T0>(ref this.component0);
                component1 = new Ref<T1>(ref this.component1);
                component2 = new Ref<T2>(ref this.component2);
                component3 = new Ref<T3>(ref this.component3);
                component4 = new Ref<T4>(ref this.component4);
                component5 = new Ref<T5>(ref this.component5);
            }
        }

        public ref struct Enumerator : IDisposable
        {
            private fixed int _compTypeIDs[6];
            private fixed int _offsets[6];
            private fixed long _compBasePtrs[6];

            private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
            private readonly EntityQueryMask _mask;
            private readonly World _world;

            private readonly VirtualStack.Scope _scope;
            private UnsafeList<int> _changedComponentIDs;

            private int _reqCount;
            private fixed int _reqOffsets[16];
            private int _reqDisCount;
            private fixed int _reqDisOffsets[16];
            private int _rejCount;
            private fixed int _rejOffsets[16];
            private bool _requiresFiltering;

            private ref Archetype _currentArchetype;
            private ref Chunk _currentChunk;
            private byte* _chunkBasePtr;

            private int _currentChunkEntityCount;
            private int _currentArchetypeIndex;
            private int _currentChunkIndex;
            private int _currentEntityIndex;

            internal Enumerator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
            {
                _compTypeIDs[0] = ComponentTypeID<T0>.Value;
                _offsets[0] = 0;
                _compBasePtrs[0] = 0;

                _compTypeIDs[1] = ComponentTypeID<T1>.Value;
                _offsets[1] = 0;
                _compBasePtrs[1] = 0;

                _compTypeIDs[2] = ComponentTypeID<T2>.Value;
                _offsets[2] = 0;
                _compBasePtrs[2] = 0;

                _compTypeIDs[3] = ComponentTypeID<T3>.Value;
                _offsets[3] = 0;
                _compBasePtrs[3] = 0;

                _compTypeIDs[4] = ComponentTypeID<T4>.Value;
                _offsets[4] = 0;
                _compBasePtrs[4] = 0;

                _compTypeIDs[5] = ComponentTypeID<T5>.Value;
                _offsets[5] = 0;
                _compBasePtrs[5] = 0;

                _matchingArchetypes = matchingArchetypes;
                _mask = mask;
                _world = world;

                _scope = AllocationManager.CreateStackScope();
                _changedComponentIDs = new UnsafeList<int>(6, _scope.AllocationHandle);

                var it = _mask.writeAccess.GetIterator();
                while (it.Next(out var id))
                {
                    for (var i = 0; i < 6; i++)
                    {
                        if (id == _compTypeIDs[i])
                        {
                            _changedComponentIDs.Add(id);
                            break;
                        }
                    }
                }

                Reset();
            }

            public QueryItem Current => new(
                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)_compBasePtrs[0])[0] : ref ((T0*)_compBasePtrs[0])[_currentEntityIndex]),
                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)_compBasePtrs[1])[0] : ref ((T1*)_compBasePtrs[1])[_currentEntityIndex]),
                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)_compBasePtrs[2])[0] : ref ((T2*)_compBasePtrs[2])[_currentEntityIndex]),
                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)_compBasePtrs[3])[0] : ref ((T3*)_compBasePtrs[3])[_currentEntityIndex]),
                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)_compBasePtrs[4])[0] : ref ((T4*)_compBasePtrs[4])[_currentEntityIndex]),
                ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)_compBasePtrs[5])[0] : ref ((T5*)_compBasePtrs[5])[_currentEntityIndex])
            );

            private void SetArchetype(int index)
            {
                _currentArchetypeIndex = index;
                _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[index]);

                _requiresFiltering = RequiresEnableableFiltering(in _currentArchetype, in _mask);
                if (_requiresFiltering)
                {
                    _reqCount = 0;
                    var itE = _mask.requireEnabled.GetIterator();
                    while (itE.Next(out var id) && _reqCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqOffsets[_reqCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _reqDisCount = 0;
                    itE = _mask.requireDisabled.GetIterator();
                    while (itE.Next(out var id) && _reqDisCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqDisOffsets[_reqDisCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _rejCount = 0;
                    itE = _mask.rejectIfEnabled.GetIterator();
                    while (itE.Next(out var id) && _rejCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _rejOffsets[_rejCount++] = layoutResult.Value.enableBitsOffset;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                if (ComponentTypeID<T0>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[0] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[0];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    _compBasePtrs[0] = (long)(_chunkBasePtr + _offsets[0]);
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[1] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[1];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    _compBasePtrs[1] = (long)(_chunkBasePtr + _offsets[1]);
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[2] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[2];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    _compBasePtrs[2] = (long)(_chunkBasePtr + _offsets[2]);
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[3]).GetValueOrThrow();
                    _offsets[3] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[3] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[3];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[3]).GetValueOrThrow();
                    _offsets[3] = layout.offset;
                    _compBasePtrs[3] = (long)(_chunkBasePtr + _offsets[3]);
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[4]).GetValueOrThrow();
                    _offsets[4] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[4] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[4];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[4]).GetValueOrThrow();
                    _offsets[4] = layout.offset;
                    _compBasePtrs[4] = (long)(_chunkBasePtr + _offsets[4]);
                }
                if (ComponentTypeID<T5>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[5]).GetValueOrThrow();
                    _offsets[5] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[5] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[5];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[5]).GetValueOrThrow();
                    _offsets[5] = layout.offset;
                    _compBasePtrs[5] = (long)(_chunkBasePtr + _offsets[5]);
                }

                for (var i = 0; i < _changedComponentIDs.Count; i++)
                {
                    _currentArchetype.MarkChanged(_currentChunkIndex, _changedComponentIDs[i], _world.Version);
                }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    _currentEntityIndex++;
                    if (_currentEntityIndex < _currentChunk._count)
                    {
                        var pChunkData = _currentChunk.GetUnsafePtr();
                        if (!_requiresFiltering)
                        {
                            return true;
                        }

                        var valid = true;
                        for (var h = 0; h < _reqCount; h++)
                        {
                            if (!CheckBit(pChunkData + _reqOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _reqDisCount; h++)
                        {
                            if (CheckBit(pChunkData + _reqDisOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _rejCount; h++)
                        {
                            if (CheckBit(pChunkData + _rejOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (valid) return true;
                    }

                    _currentChunkIndex++;
                    if (!Unsafe.IsNullRef(ref _currentArchetype) && _currentChunkIndex < _currentArchetype.ChunkCount)
                    {
                        SetChunk(_currentChunkIndex);
                        _currentEntityIndex = -1; // Reset for new chunk

                        continue;
                    }

                    _currentArchetypeIndex++;
                    if (_currentArchetypeIndex < _matchingArchetypes.Count)
                    {
                        SetArchetype(_currentArchetypeIndex);

                        _currentChunkIndex = 0;
                        if (_currentArchetype.ChunkCount > 0)
                        {
                            SetChunk(0);
                            _currentEntityIndex = -1;
                            continue;
                        }

                        // If archetype has no chunks, loop will try next archetype
                    }
                    else
                    {
                        return false; // End of all data
                    }
                }
            }

            public void Reset()
            {
                _currentArchetype = ref Unsafe.NullRef<Archetype>();
                _currentChunk = ref Unsafe.NullRef<Chunk>();
                _currentArchetypeIndex = 0;
                _currentChunkIndex = 0;
                _currentEntityIndex = -1;

                if (_matchingArchetypes.Count > 0)
                {
                    SetArchetype(0);
                    if (_currentArchetype.ChunkCount > 0)
                    {
                        SetChunk(0);
                    }
                }
            }

            public readonly void Dispose()
            {
                _changedComponentIDs.Dispose();
                _scope.Dispose();
            }
        }

        private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
        private readonly EntityQueryMask _mask;
        private readonly World _world;

        internal ComponentIterator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
        {
            _matchingArchetypes = matchingArchetypes;
            _mask = mask;
            _world = world;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_matchingArchetypes, _mask, _world);
        }
    }

    public readonly ComponentIterator<T0, T1, T2, T3, T4, T5> GetComponentIterator<T0, T1, T2, T3, T4, T5>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return default;
        }

        return new ComponentIterator<T0, T1, T2, T3, T4, T5>(_matchingArchetypes.AsReadOnly(), _mask, world);
    }

    public readonly ref struct ComponentIterator<T0, T1, T2, T3, T4, T5, T6>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
    {
        public ref struct QueryItem
        {
            public ref T0 component0;
            public ref T1 component1;
            public ref T2 component2;
            public ref T3 component3;
            public ref T4 component4;
            public ref T5 component5;
            public ref T6 component6;
            internal QueryItem(ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6)
            {
                this.component0 = ref component0;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
            }

            public void Deconstruct(out Ref<T0> component0, out Ref<T1> component1, out Ref<T2> component2, out Ref<T3> component3, out Ref<T4> component4, out Ref<T5> component5, out Ref<T6> component6)
            {
                component0 = new Ref<T0>(ref this.component0);
                component1 = new Ref<T1>(ref this.component1);
                component2 = new Ref<T2>(ref this.component2);
                component3 = new Ref<T3>(ref this.component3);
                component4 = new Ref<T4>(ref this.component4);
                component5 = new Ref<T5>(ref this.component5);
                component6 = new Ref<T6>(ref this.component6);
            }
        }

        public ref struct Enumerator : IDisposable
        {
            private fixed int _compTypeIDs[7];
            private fixed int _offsets[7];
            private fixed long _compBasePtrs[7];

            private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
            private readonly EntityQueryMask _mask;
            private readonly World _world;

            private readonly VirtualStack.Scope _scope;
            private UnsafeList<int> _changedComponentIDs;

            private int _reqCount;
            private fixed int _reqOffsets[16];
            private int _reqDisCount;
            private fixed int _reqDisOffsets[16];
            private int _rejCount;
            private fixed int _rejOffsets[16];
            private bool _requiresFiltering;

            private ref Archetype _currentArchetype;
            private ref Chunk _currentChunk;
            private byte* _chunkBasePtr;

            private int _currentChunkEntityCount;
            private int _currentArchetypeIndex;
            private int _currentChunkIndex;
            private int _currentEntityIndex;

            internal Enumerator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
            {
                _compTypeIDs[0] = ComponentTypeID<T0>.Value;
                _offsets[0] = 0;
                _compBasePtrs[0] = 0;

                _compTypeIDs[1] = ComponentTypeID<T1>.Value;
                _offsets[1] = 0;
                _compBasePtrs[1] = 0;

                _compTypeIDs[2] = ComponentTypeID<T2>.Value;
                _offsets[2] = 0;
                _compBasePtrs[2] = 0;

                _compTypeIDs[3] = ComponentTypeID<T3>.Value;
                _offsets[3] = 0;
                _compBasePtrs[3] = 0;

                _compTypeIDs[4] = ComponentTypeID<T4>.Value;
                _offsets[4] = 0;
                _compBasePtrs[4] = 0;

                _compTypeIDs[5] = ComponentTypeID<T5>.Value;
                _offsets[5] = 0;
                _compBasePtrs[5] = 0;

                _compTypeIDs[6] = ComponentTypeID<T6>.Value;
                _offsets[6] = 0;
                _compBasePtrs[6] = 0;

                _matchingArchetypes = matchingArchetypes;
                _mask = mask;
                _world = world;

                _scope = AllocationManager.CreateStackScope();
                _changedComponentIDs = new UnsafeList<int>(7, _scope.AllocationHandle);

                var it = _mask.writeAccess.GetIterator();
                while (it.Next(out var id))
                {
                    for (var i = 0; i < 7; i++)
                    {
                        if (id == _compTypeIDs[i])
                        {
                            _changedComponentIDs.Add(id);
                            break;
                        }
                    }
                }

                Reset();
            }

            public QueryItem Current => new(
                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)_compBasePtrs[0])[0] : ref ((T0*)_compBasePtrs[0])[_currentEntityIndex]),
                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)_compBasePtrs[1])[0] : ref ((T1*)_compBasePtrs[1])[_currentEntityIndex]),
                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)_compBasePtrs[2])[0] : ref ((T2*)_compBasePtrs[2])[_currentEntityIndex]),
                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)_compBasePtrs[3])[0] : ref ((T3*)_compBasePtrs[3])[_currentEntityIndex]),
                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)_compBasePtrs[4])[0] : ref ((T4*)_compBasePtrs[4])[_currentEntityIndex]),
                ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)_compBasePtrs[5])[0] : ref ((T5*)_compBasePtrs[5])[_currentEntityIndex]),
                ref (ComponentTypeID<T6>.IsShared ? ref ((T6*)_compBasePtrs[6])[0] : ref ((T6*)_compBasePtrs[6])[_currentEntityIndex])
            );

            private void SetArchetype(int index)
            {
                _currentArchetypeIndex = index;
                _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[index]);

                _requiresFiltering = RequiresEnableableFiltering(in _currentArchetype, in _mask);
                if (_requiresFiltering)
                {
                    _reqCount = 0;
                    var itE = _mask.requireEnabled.GetIterator();
                    while (itE.Next(out var id) && _reqCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqOffsets[_reqCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _reqDisCount = 0;
                    itE = _mask.requireDisabled.GetIterator();
                    while (itE.Next(out var id) && _reqDisCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqDisOffsets[_reqDisCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _rejCount = 0;
                    itE = _mask.rejectIfEnabled.GetIterator();
                    while (itE.Next(out var id) && _rejCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _rejOffsets[_rejCount++] = layoutResult.Value.enableBitsOffset;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                if (ComponentTypeID<T0>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[0] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[0];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    _compBasePtrs[0] = (long)(_chunkBasePtr + _offsets[0]);
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[1] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[1];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    _compBasePtrs[1] = (long)(_chunkBasePtr + _offsets[1]);
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[2] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[2];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    _compBasePtrs[2] = (long)(_chunkBasePtr + _offsets[2]);
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[3]).GetValueOrThrow();
                    _offsets[3] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[3] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[3];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[3]).GetValueOrThrow();
                    _offsets[3] = layout.offset;
                    _compBasePtrs[3] = (long)(_chunkBasePtr + _offsets[3]);
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[4]).GetValueOrThrow();
                    _offsets[4] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[4] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[4];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[4]).GetValueOrThrow();
                    _offsets[4] = layout.offset;
                    _compBasePtrs[4] = (long)(_chunkBasePtr + _offsets[4]);
                }
                if (ComponentTypeID<T5>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[5]).GetValueOrThrow();
                    _offsets[5] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[5] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[5];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[5]).GetValueOrThrow();
                    _offsets[5] = layout.offset;
                    _compBasePtrs[5] = (long)(_chunkBasePtr + _offsets[5]);
                }
                if (ComponentTypeID<T6>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[6]).GetValueOrThrow();
                    _offsets[6] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[6] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[6];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[6]).GetValueOrThrow();
                    _offsets[6] = layout.offset;
                    _compBasePtrs[6] = (long)(_chunkBasePtr + _offsets[6]);
                }

                for (var i = 0; i < _changedComponentIDs.Count; i++)
                {
                    _currentArchetype.MarkChanged(_currentChunkIndex, _changedComponentIDs[i], _world.Version);
                }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    _currentEntityIndex++;
                    if (_currentEntityIndex < _currentChunk._count)
                    {
                        var pChunkData = _currentChunk.GetUnsafePtr();
                        if (!_requiresFiltering)
                        {
                            return true;
                        }

                        var valid = true;
                        for (var h = 0; h < _reqCount; h++)
                        {
                            if (!CheckBit(pChunkData + _reqOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _reqDisCount; h++)
                        {
                            if (CheckBit(pChunkData + _reqDisOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _rejCount; h++)
                        {
                            if (CheckBit(pChunkData + _rejOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (valid) return true;
                    }

                    _currentChunkIndex++;
                    if (!Unsafe.IsNullRef(ref _currentArchetype) && _currentChunkIndex < _currentArchetype.ChunkCount)
                    {
                        SetChunk(_currentChunkIndex);
                        _currentEntityIndex = -1; // Reset for new chunk

                        continue;
                    }

                    _currentArchetypeIndex++;
                    if (_currentArchetypeIndex < _matchingArchetypes.Count)
                    {
                        SetArchetype(_currentArchetypeIndex);

                        _currentChunkIndex = 0;
                        if (_currentArchetype.ChunkCount > 0)
                        {
                            SetChunk(0);
                            _currentEntityIndex = -1;
                            continue;
                        }

                        // If archetype has no chunks, loop will try next archetype
                    }
                    else
                    {
                        return false; // End of all data
                    }
                }
            }

            public void Reset()
            {
                _currentArchetype = ref Unsafe.NullRef<Archetype>();
                _currentChunk = ref Unsafe.NullRef<Chunk>();
                _currentArchetypeIndex = 0;
                _currentChunkIndex = 0;
                _currentEntityIndex = -1;

                if (_matchingArchetypes.Count > 0)
                {
                    SetArchetype(0);
                    if (_currentArchetype.ChunkCount > 0)
                    {
                        SetChunk(0);
                    }
                }
            }

            public readonly void Dispose()
            {
                _changedComponentIDs.Dispose();
                _scope.Dispose();
            }
        }

        private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
        private readonly EntityQueryMask _mask;
        private readonly World _world;

        internal ComponentIterator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
        {
            _matchingArchetypes = matchingArchetypes;
            _mask = mask;
            _world = world;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_matchingArchetypes, _mask, _world);
        }
    }

    public readonly ComponentIterator<T0, T1, T2, T3, T4, T5, T6> GetComponentIterator<T0, T1, T2, T3, T4, T5, T6>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return default;
        }

        return new ComponentIterator<T0, T1, T2, T3, T4, T5, T6>(_matchingArchetypes.AsReadOnly(), _mask, world);
    }

    public readonly ref struct ComponentIterator<T0, T1, T2, T3, T4, T5, T6, T7>
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
    {
        public ref struct QueryItem
        {
            public ref T0 component0;
            public ref T1 component1;
            public ref T2 component2;
            public ref T3 component3;
            public ref T4 component4;
            public ref T5 component5;
            public ref T6 component6;
            public ref T7 component7;
            internal QueryItem(ref T0 component0, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6, ref T7 component7)
            {
                this.component0 = ref component0;
                this.component1 = ref component1;
                this.component2 = ref component2;
                this.component3 = ref component3;
                this.component4 = ref component4;
                this.component5 = ref component5;
                this.component6 = ref component6;
                this.component7 = ref component7;
            }

            public void Deconstruct(out Ref<T0> component0, out Ref<T1> component1, out Ref<T2> component2, out Ref<T3> component3, out Ref<T4> component4, out Ref<T5> component5, out Ref<T6> component6, out Ref<T7> component7)
            {
                component0 = new Ref<T0>(ref this.component0);
                component1 = new Ref<T1>(ref this.component1);
                component2 = new Ref<T2>(ref this.component2);
                component3 = new Ref<T3>(ref this.component3);
                component4 = new Ref<T4>(ref this.component4);
                component5 = new Ref<T5>(ref this.component5);
                component6 = new Ref<T6>(ref this.component6);
                component7 = new Ref<T7>(ref this.component7);
            }
        }

        public ref struct Enumerator : IDisposable
        {
            private fixed int _compTypeIDs[8];
            private fixed int _offsets[8];
            private fixed long _compBasePtrs[8];

            private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
            private readonly EntityQueryMask _mask;
            private readonly World _world;

            private readonly VirtualStack.Scope _scope;
            private UnsafeList<int> _changedComponentIDs;

            private int _reqCount;
            private fixed int _reqOffsets[16];
            private int _reqDisCount;
            private fixed int _reqDisOffsets[16];
            private int _rejCount;
            private fixed int _rejOffsets[16];
            private bool _requiresFiltering;

            private ref Archetype _currentArchetype;
            private ref Chunk _currentChunk;
            private byte* _chunkBasePtr;

            private int _currentChunkEntityCount;
            private int _currentArchetypeIndex;
            private int _currentChunkIndex;
            private int _currentEntityIndex;

            internal Enumerator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
            {
                _compTypeIDs[0] = ComponentTypeID<T0>.Value;
                _offsets[0] = 0;
                _compBasePtrs[0] = 0;

                _compTypeIDs[1] = ComponentTypeID<T1>.Value;
                _offsets[1] = 0;
                _compBasePtrs[1] = 0;

                _compTypeIDs[2] = ComponentTypeID<T2>.Value;
                _offsets[2] = 0;
                _compBasePtrs[2] = 0;

                _compTypeIDs[3] = ComponentTypeID<T3>.Value;
                _offsets[3] = 0;
                _compBasePtrs[3] = 0;

                _compTypeIDs[4] = ComponentTypeID<T4>.Value;
                _offsets[4] = 0;
                _compBasePtrs[4] = 0;

                _compTypeIDs[5] = ComponentTypeID<T5>.Value;
                _offsets[5] = 0;
                _compBasePtrs[5] = 0;

                _compTypeIDs[6] = ComponentTypeID<T6>.Value;
                _offsets[6] = 0;
                _compBasePtrs[6] = 0;

                _compTypeIDs[7] = ComponentTypeID<T7>.Value;
                _offsets[7] = 0;
                _compBasePtrs[7] = 0;

                _matchingArchetypes = matchingArchetypes;
                _mask = mask;
                _world = world;

                _scope = AllocationManager.CreateStackScope();
                _changedComponentIDs = new UnsafeList<int>(8, _scope.AllocationHandle);

                var it = _mask.writeAccess.GetIterator();
                while (it.Next(out var id))
                {
                    for (var i = 0; i < 8; i++)
                    {
                        if (id == _compTypeIDs[i])
                        {
                            _changedComponentIDs.Add(id);
                            break;
                        }
                    }
                }

                Reset();
            }

            public QueryItem Current => new(
                ref (ComponentTypeID<T0>.IsShared ? ref ((T0*)_compBasePtrs[0])[0] : ref ((T0*)_compBasePtrs[0])[_currentEntityIndex]),
                ref (ComponentTypeID<T1>.IsShared ? ref ((T1*)_compBasePtrs[1])[0] : ref ((T1*)_compBasePtrs[1])[_currentEntityIndex]),
                ref (ComponentTypeID<T2>.IsShared ? ref ((T2*)_compBasePtrs[2])[0] : ref ((T2*)_compBasePtrs[2])[_currentEntityIndex]),
                ref (ComponentTypeID<T3>.IsShared ? ref ((T3*)_compBasePtrs[3])[0] : ref ((T3*)_compBasePtrs[3])[_currentEntityIndex]),
                ref (ComponentTypeID<T4>.IsShared ? ref ((T4*)_compBasePtrs[4])[0] : ref ((T4*)_compBasePtrs[4])[_currentEntityIndex]),
                ref (ComponentTypeID<T5>.IsShared ? ref ((T5*)_compBasePtrs[5])[0] : ref ((T5*)_compBasePtrs[5])[_currentEntityIndex]),
                ref (ComponentTypeID<T6>.IsShared ? ref ((T6*)_compBasePtrs[6])[0] : ref ((T6*)_compBasePtrs[6])[_currentEntityIndex]),
                ref (ComponentTypeID<T7>.IsShared ? ref ((T7*)_compBasePtrs[7])[0] : ref ((T7*)_compBasePtrs[7])[_currentEntityIndex])
            );

            private void SetArchetype(int index)
            {
                _currentArchetypeIndex = index;
                _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[index]);

                _requiresFiltering = RequiresEnableableFiltering(in _currentArchetype, in _mask);
                if (_requiresFiltering)
                {
                    _reqCount = 0;
                    var itE = _mask.requireEnabled.GetIterator();
                    while (itE.Next(out var id) && _reqCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqOffsets[_reqCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _reqDisCount = 0;
                    itE = _mask.requireDisabled.GetIterator();
                    while (itE.Next(out var id) && _reqDisCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _reqDisOffsets[_reqDisCount++] = layoutResult.Value.enableBitsOffset;
                    }

                    _rejCount = 0;
                    itE = _mask.rejectIfEnabled.GetIterator();
                    while (itE.Next(out var id) && _rejCount < 16)
                    {
                        var layoutResult = _currentArchetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                            _rejOffsets[_rejCount++] = layoutResult.Value.enableBitsOffset;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                if (ComponentTypeID<T0>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[0] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[0];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[0]).GetValueOrThrow();
                    _offsets[0] = layout.offset;
                    _compBasePtrs[0] = (long)(_chunkBasePtr + _offsets[0]);
                }
                if (ComponentTypeID<T1>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[1] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[1];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[1]).GetValueOrThrow();
                    _offsets[1] = layout.offset;
                    _compBasePtrs[1] = (long)(_chunkBasePtr + _offsets[1]);
                }
                if (ComponentTypeID<T2>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[2] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[2];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[2]).GetValueOrThrow();
                    _offsets[2] = layout.offset;
                    _compBasePtrs[2] = (long)(_chunkBasePtr + _offsets[2]);
                }
                if (ComponentTypeID<T3>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[3]).GetValueOrThrow();
                    _offsets[3] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[3] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[3];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[3]).GetValueOrThrow();
                    _offsets[3] = layout.offset;
                    _compBasePtrs[3] = (long)(_chunkBasePtr + _offsets[3]);
                }
                if (ComponentTypeID<T4>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[4]).GetValueOrThrow();
                    _offsets[4] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[4] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[4];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[4]).GetValueOrThrow();
                    _offsets[4] = layout.offset;
                    _compBasePtrs[4] = (long)(_chunkBasePtr + _offsets[4]);
                }
                if (ComponentTypeID<T5>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[5]).GetValueOrThrow();
                    _offsets[5] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[5] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[5];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[5]).GetValueOrThrow();
                    _offsets[5] = layout.offset;
                    _compBasePtrs[5] = (long)(_chunkBasePtr + _offsets[5]);
                }
                if (ComponentTypeID<T6>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[6]).GetValueOrThrow();
                    _offsets[6] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[6] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[6];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[6]).GetValueOrThrow();
                    _offsets[6] = layout.offset;
                    _compBasePtrs[6] = (long)(_chunkBasePtr + _offsets[6]);
                }
                if (ComponentTypeID<T7>.IsShared)
                {
                    var layout = _currentArchetype.GetSharedLayout(_compTypeIDs[7]).GetValueOrThrow();
                    _offsets[7] = layout.offset;
                    var sharedSpan = _currentArchetype._chunkGroups[_currentChunk._groupIndex].sharedData.AsSpan();
                    _compBasePtrs[7] = (long)Unsafe.AsPointer(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(sharedSpan)) + _offsets[7];
                }
                else
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[7]).GetValueOrThrow();
                    _offsets[7] = layout.offset;
                    _compBasePtrs[7] = (long)(_chunkBasePtr + _offsets[7]);
                }

                for (var i = 0; i < _changedComponentIDs.Count; i++)
                {
                    _currentArchetype.MarkChanged(_currentChunkIndex, _changedComponentIDs[i], _world.Version);
                }
            }

            public bool MoveNext()
            {
                while (true)
                {
                    _currentEntityIndex++;
                    if (_currentEntityIndex < _currentChunk._count)
                    {
                        var pChunkData = _currentChunk.GetUnsafePtr();
                        if (!_requiresFiltering)
                        {
                            return true;
                        }

                        var valid = true;
                        for (var h = 0; h < _reqCount; h++)
                        {
                            if (!CheckBit(pChunkData + _reqOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _reqDisCount; h++)
                        {
                            if (CheckBit(pChunkData + _reqDisOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (!valid) continue;

                        for (var h = 0; h < _rejCount; h++)
                        {
                            if (CheckBit(pChunkData + _rejOffsets[h], _currentEntityIndex)) { valid = false; break; }
                        }
                        if (valid) return true;
                    }

                    _currentChunkIndex++;
                    if (!Unsafe.IsNullRef(ref _currentArchetype) && _currentChunkIndex < _currentArchetype.ChunkCount)
                    {
                        SetChunk(_currentChunkIndex);
                        _currentEntityIndex = -1; // Reset for new chunk

                        continue;
                    }

                    _currentArchetypeIndex++;
                    if (_currentArchetypeIndex < _matchingArchetypes.Count)
                    {
                        SetArchetype(_currentArchetypeIndex);

                        _currentChunkIndex = 0;
                        if (_currentArchetype.ChunkCount > 0)
                        {
                            SetChunk(0);
                            _currentEntityIndex = -1;
                            continue;
                        }

                        // If archetype has no chunks, loop will try next archetype
                    }
                    else
                    {
                        return false; // End of all data
                    }
                }
            }

            public void Reset()
            {
                _currentArchetype = ref Unsafe.NullRef<Archetype>();
                _currentChunk = ref Unsafe.NullRef<Chunk>();
                _currentArchetypeIndex = 0;
                _currentChunkIndex = 0;
                _currentEntityIndex = -1;

                if (_matchingArchetypes.Count > 0)
                {
                    SetArchetype(0);
                    if (_currentArchetype.ChunkCount > 0)
                    {
                        SetChunk(0);
                    }
                }
            }

            public readonly void Dispose()
            {
                _changedComponentIDs.Dispose();
                _scope.Dispose();
            }
        }

        private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
        private readonly EntityQueryMask _mask;
        private readonly World _world;

        internal ComponentIterator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, EntityQueryMask mask, World world)
        {
            _matchingArchetypes = matchingArchetypes;
            _mask = mask;
            _world = world;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_matchingArchetypes, _mask, _world);
        }
    }

    public readonly ComponentIterator<T0, T1, T2, T3, T4, T5, T6, T7> GetComponentIterator<T0, T1, T2, T3, T4, T5, T6, T7>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent
    {
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return default;
        }

        return new ComponentIterator<T0, T1, T2, T3, T4, T5, T6, T7>(_matchingArchetypes.AsReadOnly(), _mask, world);
    }

}
