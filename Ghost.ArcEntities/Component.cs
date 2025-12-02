using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;

namespace Ghost.ArcEntities;

public struct ComponentInfo
{
    public int size;
    public int alignment;
    public int id;
}

internal static unsafe class ComponentTypeID<T>
    where T : unmanaged
{
    public static readonly int value = ComponentRegister.s_nextComponentTypeID++;
}

internal static class ComponentRegister
{
    internal static int s_nextComponentTypeID = 0;
    internal static List<ComponentInfo> s_registeredComponents = new();

    internal unsafe static int GetOrRegisterComponent<T>()
        where T : unmanaged
    {
        var typeId = ComponentTypeID<T>.value;
        while (s_registeredComponents.Count <= typeId)
        {
            s_registeredComponents.Add(default);
        }

        if (s_registeredComponents[typeId].size == 0)
        {
            s_registeredComponents[typeId] = new ComponentInfo
            {
                size = sizeof(T),
                alignment = (int)MemoryUtility.AlignOf<T>(),
                id = typeId
            };
        }

        return typeId;
    }

    internal static ComponentInfo GetComponentInfo(int typeId)
    {
        return s_registeredComponents[typeId];
    }
}

internal unsafe struct Chuck : IDisposable
{
    public const int CHUNK_SIZE = 16384; // 16 KB

    private UnsafeArray<byte> _data;
    private int _count;
    private int _capacity;

    public int Count
    {
        get => _count;
        set => _count = value;
    }

    public int Capacity => _capacity;

    public Chuck(int size, int capacity)
    {
        _data = new UnsafeArray<byte>(size, Allocator.Persistent);
        _capacity = capacity;
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* GetUnsafePtr()
    {
        return (byte*)_data.GetUnsafePtr();
    }

    public void Dispose()
    {
        _data.Dispose();
    }
}
