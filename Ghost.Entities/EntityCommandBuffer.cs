using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public unsafe class EntityCommandBuffer : IDisposable
{
    private enum ECBOpCode : byte
    {
        CreateEntity,
        CreateEntityWithComponents,
        DestroyEntity,
        AddComponent,
        RemoveComponent,
        SetComponent,
    }

    private readonly EntityManager _entityManager;
    private UnsafeList<byte> _buffer;
    private bool _disposed;

    public EntityCommandBuffer(EntityManager entityManager)
    {
        _entityManager = entityManager;
        _buffer = new UnsafeList<byte>(4096, Allocator.Persistent);
    }

    ~EntityCommandBuffer()
    {
        Dispose();
    }

    private void WriteHeader(ECBOpCode op)
    {
        _buffer.Add((byte)op);
    }

    private void Write<T>(T value)
        where T : unmanaged
    {
        var size = sizeof(T);
        var idx = _buffer.Count;

        if (idx + size > _buffer.Capacity)
        {
            _buffer.Resize(idx + size);
        }

        MemoryUtility.MemCpy((byte*)_buffer.GetUnsafePtr() + idx, &value, (nuint)size);
    }

    private void WriteSpan<T>(ReadOnlySpan<T> span)
        where T : unmanaged
    {
        var size = sizeof(T) * span.Length;
        var idx = _buffer.Count;

        if (idx + size > _buffer.Capacity)
        {
            _buffer.Resize(idx + size);
        }

        fixed (T* ptr = span)
        {
            MemoryUtility.MemCpy((byte*)_buffer.GetUnsafePtr() + idx, ptr, (nuint)size);
        }
    }

    private T Read<T>(ref int cursor)
        where T : unmanaged
    {
        var size = sizeof(T);
        var ptr = (byte*)_buffer.GetUnsafePtr();

        var value = *(T*)&ptr[cursor];
        cursor += size;

        return value;
    }

    private Span<T> ReadSpan<T>(ref int cursor, int length)
        where T : unmanaged
    {
        var size = sizeof(T) * length;
        var ptr = (byte*)_buffer.GetUnsafePtr();

        var span = new Span<T>(&ptr[cursor], length);
        cursor += size;

        return span;
    }

    private void* ReadBuffer(ref int cursor, int size)
    {
        var ptr = (byte*)_buffer.GetUnsafePtr();
        var bufferPtr = ptr + cursor;
        cursor += size;

        return bufferPtr;
    }

    public void CreateEntity()
    {
        WriteHeader(ECBOpCode.CreateEntity);
    }

    public void CreateEntity(params ReadOnlySpan<Identifier<IComponent>> componentTypeIDs)
    {
        WriteHeader(ECBOpCode.CreateEntityWithComponents);
        Write(componentTypeIDs.Length);
        WriteSpan(componentTypeIDs);
    }

    public void DestroyEntity(Entity entity)
    {
        WriteHeader(ECBOpCode.DestroyEntity);
        Write(entity);
    }

    public void AddComponent<T>(Entity entity, T component = default)
        where T : unmanaged, IComponent
    {
        WriteHeader(ECBOpCode.AddComponent);
        Write(entity);
        Write(ComponentTypeID<T>.value);
        Write(component);
    }

    public void RemoveComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        WriteHeader(ECBOpCode.RemoveComponent);
        Write(entity);
        Write(ComponentTypeID<T>.value);
    }

    public void SetComponent<T>(Entity entity, T component)
        where T : unmanaged, IComponent
    {
        WriteHeader(ECBOpCode.SetComponent);
        Write(entity);
        Write(ComponentTypeID<T>.value);
        Write(component);
    }

    internal void Playback()
    {
        var cursor = 0;
        var length = _buffer.Count;
        var ptr = (byte*)_buffer.GetUnsafePtr();

        while (cursor < length)
        {
            var op = *(ECBOpCode*)ptr[cursor];
            cursor += sizeof(ECBOpCode);

            switch (op)
            {
                case ECBOpCode.CreateEntity:
                    _entityManager.CreateEntity();
                    break;

                case ECBOpCode.CreateEntityWithComponents:
                    var compCount = Read<int>(ref cursor);
                    var compTypeIDs = ReadSpan<Identifier<IComponent>>(ref cursor, compCount);
                    _entityManager.CreateEntity(compTypeIDs);
                    break;

                case ECBOpCode.DestroyEntity:
                    var entityToDestroy = Read<Entity>(ref cursor);
                    _entityManager.DestroyEntity(entityToDestroy);
                    break;

                case ECBOpCode.AddComponent:
                    var entityToAdd = Read<Entity>(ref cursor);
                    var addCompTypeID = Read<Identifier<IComponent>>(ref cursor);
                    var pAddCompData = ReadBuffer(ref cursor, ComponentRegister.GetComponentInfo(addCompTypeID).size);
                    _entityManager.AddComponent(entityToAdd, addCompTypeID, pAddCompData);
                    break;

                case ECBOpCode.RemoveComponent:
                    var entityToRemove = Read<Entity>(ref cursor);
                    var removeCompTypeID = Read<Identifier<IComponent>>(ref cursor);
                    _entityManager.RemoveComponent(entityToRemove, removeCompTypeID);
                    break;

                case ECBOpCode.SetComponent:
                    var entityToSet = Read<Entity>(ref cursor);
                    var setCompTypeID = Read<Identifier<IComponent>>(ref cursor);
                    var pSetCompData = ReadBuffer(ref cursor, ComponentRegister.GetComponentInfo(setCompTypeID).size);
                    _entityManager.SetComponent(entityToSet, setCompTypeID, pSetCompData);
                    break;
            }
        }

        Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Reset()
    {
        _buffer.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _buffer.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
