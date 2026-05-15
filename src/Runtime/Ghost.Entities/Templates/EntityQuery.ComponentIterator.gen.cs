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

            public ref T0 Current => ref *(T0*)(_compBasePtrs[0] + _currentEntityIndex * sizeof(T0));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                for (var index = 0; index < 1; index++)
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[index])
                        .GetValueOrThrow();
                    _offsets[index] = layout.offset;
                    _compBasePtrs[index] = (long)(_chunkBasePtr + _offsets[index]);
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
                        if (IsEntityValid(pChunkData, _currentEntityIndex, in _currentArchetype, in _mask))
                        {
                            return true;
                        }

                        continue;
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
                        _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[_currentArchetypeIndex]);

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
                    _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[0]);
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
                ref *(T0*)(_compBasePtrs[0] + _currentEntityIndex * sizeof(T0)),
                ref *(T1*)(_compBasePtrs[1] + _currentEntityIndex * sizeof(T1))
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                for (var index = 0; index < 2; index++)
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[index])
                        .GetValueOrThrow();
                    _offsets[index] = layout.offset;
                    _compBasePtrs[index] = (long)(_chunkBasePtr + _offsets[index]);
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
                        if (IsEntityValid(pChunkData, _currentEntityIndex, in _currentArchetype, in _mask))
                        {
                            return true;
                        }

                        continue;
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
                        _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[_currentArchetypeIndex]);

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
                    _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[0]);
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
                ref *(T0*)(_compBasePtrs[0] + _currentEntityIndex * sizeof(T0)),
                ref *(T1*)(_compBasePtrs[1] + _currentEntityIndex * sizeof(T1)),
                ref *(T2*)(_compBasePtrs[2] + _currentEntityIndex * sizeof(T2))
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                for (var index = 0; index < 3; index++)
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[index])
                        .GetValueOrThrow();
                    _offsets[index] = layout.offset;
                    _compBasePtrs[index] = (long)(_chunkBasePtr + _offsets[index]);
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
                        if (IsEntityValid(pChunkData, _currentEntityIndex, in _currentArchetype, in _mask))
                        {
                            return true;
                        }

                        continue;
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
                        _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[_currentArchetypeIndex]);

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
                    _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[0]);
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
                ref *(T0*)(_compBasePtrs[0] + _currentEntityIndex * sizeof(T0)),
                ref *(T1*)(_compBasePtrs[1] + _currentEntityIndex * sizeof(T1)),
                ref *(T2*)(_compBasePtrs[2] + _currentEntityIndex * sizeof(T2)),
                ref *(T3*)(_compBasePtrs[3] + _currentEntityIndex * sizeof(T3))
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                for (var index = 0; index < 4; index++)
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[index])
                        .GetValueOrThrow();
                    _offsets[index] = layout.offset;
                    _compBasePtrs[index] = (long)(_chunkBasePtr + _offsets[index]);
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
                        if (IsEntityValid(pChunkData, _currentEntityIndex, in _currentArchetype, in _mask))
                        {
                            return true;
                        }

                        continue;
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
                        _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[_currentArchetypeIndex]);

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
                    _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[0]);
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
                ref *(T0*)(_compBasePtrs[0] + _currentEntityIndex * sizeof(T0)),
                ref *(T1*)(_compBasePtrs[1] + _currentEntityIndex * sizeof(T1)),
                ref *(T2*)(_compBasePtrs[2] + _currentEntityIndex * sizeof(T2)),
                ref *(T3*)(_compBasePtrs[3] + _currentEntityIndex * sizeof(T3)),
                ref *(T4*)(_compBasePtrs[4] + _currentEntityIndex * sizeof(T4))
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                for (var index = 0; index < 5; index++)
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[index])
                        .GetValueOrThrow();
                    _offsets[index] = layout.offset;
                    _compBasePtrs[index] = (long)(_chunkBasePtr + _offsets[index]);
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
                        if (IsEntityValid(pChunkData, _currentEntityIndex, in _currentArchetype, in _mask))
                        {
                            return true;
                        }

                        continue;
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
                        _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[_currentArchetypeIndex]);

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
                    _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[0]);
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
                ref *(T0*)(_compBasePtrs[0] + _currentEntityIndex * sizeof(T0)),
                ref *(T1*)(_compBasePtrs[1] + _currentEntityIndex * sizeof(T1)),
                ref *(T2*)(_compBasePtrs[2] + _currentEntityIndex * sizeof(T2)),
                ref *(T3*)(_compBasePtrs[3] + _currentEntityIndex * sizeof(T3)),
                ref *(T4*)(_compBasePtrs[4] + _currentEntityIndex * sizeof(T4)),
                ref *(T5*)(_compBasePtrs[5] + _currentEntityIndex * sizeof(T5))
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                for (var index = 0; index < 6; index++)
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[index])
                        .GetValueOrThrow();
                    _offsets[index] = layout.offset;
                    _compBasePtrs[index] = (long)(_chunkBasePtr + _offsets[index]);
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
                        if (IsEntityValid(pChunkData, _currentEntityIndex, in _currentArchetype, in _mask))
                        {
                            return true;
                        }

                        continue;
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
                        _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[_currentArchetypeIndex]);

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
                    _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[0]);
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
                ref *(T0*)(_compBasePtrs[0] + _currentEntityIndex * sizeof(T0)),
                ref *(T1*)(_compBasePtrs[1] + _currentEntityIndex * sizeof(T1)),
                ref *(T2*)(_compBasePtrs[2] + _currentEntityIndex * sizeof(T2)),
                ref *(T3*)(_compBasePtrs[3] + _currentEntityIndex * sizeof(T3)),
                ref *(T4*)(_compBasePtrs[4] + _currentEntityIndex * sizeof(T4)),
                ref *(T5*)(_compBasePtrs[5] + _currentEntityIndex * sizeof(T5)),
                ref *(T6*)(_compBasePtrs[6] + _currentEntityIndex * sizeof(T6))
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                for (var index = 0; index < 7; index++)
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[index])
                        .GetValueOrThrow();
                    _offsets[index] = layout.offset;
                    _compBasePtrs[index] = (long)(_chunkBasePtr + _offsets[index]);
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
                        if (IsEntityValid(pChunkData, _currentEntityIndex, in _currentArchetype, in _mask))
                        {
                            return true;
                        }

                        continue;
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
                        _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[_currentArchetypeIndex]);

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
                    _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[0]);
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
                ref *(T0*)(_compBasePtrs[0] + _currentEntityIndex * sizeof(T0)),
                ref *(T1*)(_compBasePtrs[1] + _currentEntityIndex * sizeof(T1)),
                ref *(T2*)(_compBasePtrs[2] + _currentEntityIndex * sizeof(T2)),
                ref *(T3*)(_compBasePtrs[3] + _currentEntityIndex * sizeof(T3)),
                ref *(T4*)(_compBasePtrs[4] + _currentEntityIndex * sizeof(T4)),
                ref *(T5*)(_compBasePtrs[5] + _currentEntityIndex * sizeof(T5)),
                ref *(T6*)(_compBasePtrs[6] + _currentEntityIndex * sizeof(T6)),
                ref *(T7*)(_compBasePtrs[7] + _currentEntityIndex * sizeof(T7))
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetChunk(int chunkIndex)
            {
                _currentChunk = ref _currentArchetype.GetChunkReference(chunkIndex);
                _chunkBasePtr = _currentChunk.GetUnsafePtr();
                _currentChunkEntityCount = _currentChunk._count;

                for (var index = 0; index < 8; index++)
                {
                    var layout = _currentArchetype.GetLayout(_compTypeIDs[index])
                        .GetValueOrThrow();
                    _offsets[index] = layout.offset;
                    _compBasePtrs[index] = (long)(_chunkBasePtr + _offsets[index]);
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
                        if (IsEntityValid(pChunkData, _currentEntityIndex, in _currentArchetype, in _mask))
                        {
                            return true;
                        }

                        continue;
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
                        _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[_currentArchetypeIndex]);

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
                    _currentArchetype = ref _world.ComponentManager.GetArchetypeReference(_matchingArchetypes[0]);
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
