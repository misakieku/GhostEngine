using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

internal struct Chunks : IDisposable
{
    private UnsafeArray<Chunk> _chunks;
    private int _count;
    private int _capacity;

    public readonly int Count => _count;
    public readonly int Capacity => _capacity;

    public ref Chunk this[int index] => ref _chunks[index];

    public Chunks(int capacity)
    {
        _chunks = new(capacity, Allocator.Persistent);
        _count = 0;
        _capacity = capacity;
    }

    public void Add(Chunk chunk)
    {
        _chunks[_count] = chunk;
        _count++;
    }

    public void EnsureCapacity(int newCapacity)
    {
        if (newCapacity <= _capacity)
        {
            return;
        }

        _chunks.Resize(newCapacity);
    }

    public void TrimExcess()
    {
        if (_count == _capacity)
        {
            return;
        }

        _chunks.Resize(_count);
    }

    public void Clear()
    {
        for (var i = 0; i < _count; i++)
        {
            _chunks[i].Clear();
        }

        _count = 0;
        _capacity = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<Chunk> AsSpan()
    {
        return _chunks.AsSpan();
    }

    public void Dispose()
    {
        for (var i = 0; i < _count; i++)
        {
            _chunks[i].Dispose();
        }

        _chunks.Dispose();
        _count = 0;
        _capacity = 0;
    }
}

internal struct Chunk : IDisposable
{
    public UnsafeArray<Entity> entities;
    public UnsafeArray<UnsafeArray<byte>> components;

    // The component lookup array is used to quickly find the index of a component in the components array.
    // Mapping component ID to component index in the components array.
    private UnsafeArray<int> _componentLookup;

    private int _count;
    private readonly int _capacity;
    private bool _isDisposed;

    public readonly int Count => _count;
    public readonly int Capacity => _capacity;

    public Chunk(int capacity, Span<ComponentData> data) : this(capacity, data, Component.ToLookupArray(data, Allocator.Persistent))
    {
    }

    public Chunk(int capacity, Span<ComponentData> data, UnsafeArray<int> lookup)
    {
        _count = 0;
        _capacity = capacity;

        entities = new(capacity, Allocator.Persistent);
        components = new(data.Length, Allocator.Persistent);

        _componentLookup = lookup;

        for (var i = 0; i < data.Length; i++)
        {
            var component = data[i];
            components[component.id] = new UnsafeArray<byte>(capacity * component.sizeInByte, Allocator.Persistent);
        }
    }

    public int Add(Entity entity)
    {
        var index = _count;
        entities[index] = entity;
        _count++;

        return index;
    }

    public unsafe bool Remove(int index)
    {
        if (index < 0 || index >= _count)
        {
            return false;
        }

        var lastIndex = _count--;
        entities[index] = entities[lastIndex];

        for (var i = 0; i < components.Count; i++)
        {
            var componentArray = UnsafeUtilities.ReadArrayElementUnsafe<UnsafeArray<byte>>(components.GetUnsafePtr(), i);
            var componentSize = componentArray->Count / _capacity;
            var removedComponent = UnsafeUtilities.ReadArrayElementUnsafe<byte>(componentArray->GetUnsafePtr(), index * componentSize);
            var lastComponent = UnsafeUtilities.ReadArrayElementUnsafe<byte>(componentArray->GetUnsafePtr(), lastIndex * componentSize);
            MemoryUtilities.MemCpy(removedComponent, lastComponent, (nuint)componentSize);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int IndexOf<T>()
        where T : unmanaged, IComponent
    {
        var id = Component<T>.data.id;
        Debug.Assert(id != -1 && id < _componentLookup.Count, $"Index is out of bounds, component {typeof(T)} with id {id} does not exist in this chunk.");
        return _componentLookup[id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Has<T>()
        where T : unmanaged, IComponent
    {
        var id = Component<T>.data.id;
        return id < _componentLookup.Count && _componentLookup[id] != -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe UnsafeArray<T> GetArrayOf<T>()
        where T : unmanaged, IComponent
    {
        var index = IndexOf<T>();
        var componentArray = components[index];
        return UnsafeUtilities.CastArray<byte, T>(componentArray);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly unsafe ref T GetComponent<T>(int index)
        where T : unmanaged, IComponent
    {
        var componentArray = GetArrayOf<T>();
        return ref componentArray[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Entity GetEntity(int index)
    {
        return entities[index];
    }

    public void Clear()
    {
        _count = 0;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        entities.Dispose();
        _componentLookup.Dispose();

        for (var i = 0; i < components.Count; i++)
        {
            components[i].Dispose();
        }
        components.Dispose();

        _isDisposed = true;
    }
}