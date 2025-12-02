using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;

namespace Ghost.ArcEntities;

internal struct ComponentInfo
{
    public int size;
    public int alignment;
    public int id;
}

internal static unsafe class ComponentType<T>
    where T : unmanaged
{
    public static readonly int Size = sizeof(T);
    public static readonly int Alignment = (int)MemoryUtility.AlignOf<T>();
    public static readonly int id = ComponentRegister.s_nextComponentTypeID++;

    public static ComponentInfo GetInfo()
    {
        return new ComponentInfo
        {
            size = Size,
            alignment = Alignment,
            id = id
        };
    }
}

internal class ComponentRegister
{
    internal static int s_nextComponentTypeID = 0;
}

internal unsafe struct Chuck : IDisposable
{
    public const int CHUNK_SIZE = 16384; // 16 KB

    private UnsafeArray<byte> _data;
    private int _count;
    private int _capacity;

    public int Count => _count;
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
