using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using System.Buffers;

namespace Ghost.Core.Utilities;

public unsafe class NativeMemoryManager<T> : MemoryManager<T>
    where T : unmanaged
{
    private readonly T* _pointer;
    private readonly int _length;

    public NativeMemoryManager(T* pointer, int length)
    {
        _pointer = pointer;
        _length = length;
    }

    public static NativeMemoryManager<T> FromUnsafeCollection<C>(ref readonly C collection)
        where C : unmanaged, IUnsafeCollection<T>
    {
        if (!collection.IsCreated)
        {
            throw new InvalidOperationException("The collection is not created.");
        }

        return new NativeMemoryManager<T>((T*)collection.GetUnsafePtr(), collection.Count);
    }

    public override Span<T> GetSpan()
    {
        return new Span<T>(_pointer, _length);
    }

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        return new MemoryHandle(_pointer + elementIndex);
    }

    public override void Unpin()
    {
    }

    protected override void Dispose(bool disposing)
    {
    }
}
