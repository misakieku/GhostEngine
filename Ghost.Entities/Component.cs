using Ghost.Entities.Helpers;
using Ghost.Entities.Registries;
using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Entities;

public interface IComponent
{

}

[SkipLocalsInit]
internal readonly record struct ComponentData
{
    public readonly int id;
    public readonly int sizeInByte;

    public ComponentData(int id, int sizeInByte)
    {
        this.id = id;
        this.sizeInByte = sizeInByte;
    }
}

internal static class Component
{
    public static unsafe UnsafeArray<int> ToLookupArray(Span<ComponentData> datas, Allocator allocator)
    {
        var max = 0;
        foreach (var data in datas)
        {
            var componentId = data.id;
            if (componentId >= max)
            {
                max = componentId;
            }
        }

        // Create lookup table where the component ID points to the component index.
        var array = new UnsafeArray<int>(max + 1, allocator);
        array.AsSpan().Fill(-1);

        for (var index = 0; index < datas.Length; index++)
        {
            ref var type = ref datas[index];
            var componentId = type.id;
            array[componentId] = index;
        }

        return array;
    }

    public static int GetHashCode(Span<ComponentData> components)
    {
        // Search for the highest id to determine how much uints we need for the stack.
        var highestId = 0;
        foreach (ref var cmp in components)
        {
            if (cmp.id > highestId)
            {
                highestId = cmp.id;
            }
        }

        // Allocate the stack and set bits to replicate a bitset
        var length = BitSet.RequiredLength(highestId + 1);
        Span<uint> stack = stackalloc uint[length];
        var spanBitSet = new SpanBitSet(stack);

        foreach (ref var type in components)
        {
            var x = type.id;
            spanBitSet.SetBit(x);
        }

        return GetHashCode(stack);
    }

    public static int GetHashCode(Span<uint> span)
    {
        var hashCode = new HashCode();
        hashCode.AddBytes(MemoryMarshal.AsBytes(span));

        return hashCode.ToHashCode();
    }
}

internal static class Component<T>
    where T : unmanaged, IComponent
{
    public static readonly ComponentData data;

    public static readonly Signature signature;

    static Component()
    {
        data = ComponentRegistry.GetOrAdd<T>();
        signature = new Signature(data);
    }
}