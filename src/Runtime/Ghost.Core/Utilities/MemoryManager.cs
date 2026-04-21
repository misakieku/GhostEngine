using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections.Contracts;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MemoryHandle = System.Buffers.MemoryHandle;

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

    public static NativeMemoryManager<T> FromMemoryBlock(MemoryBlock memoryBlock, int start, int length)
    {
        return new NativeMemoryManager<T>((T*)memoryBlock.GetUnsafePtr() + start, length);
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

public sealed class CastMemoryManager<TFrom, TTo> : MemoryManager<TTo>
    where TFrom : struct
    where TTo : struct
{
    private readonly Memory<TFrom> _from;
    private MemoryHandle _innerHandle;

    public CastMemoryManager(Memory<TFrom> from)
    {
        _from = from;
    }

    public override Span<TTo> GetSpan()
    {
        return MemoryMarshal.Cast<TFrom, TTo>(_from.Span);
    }

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        _innerHandle = _from.Pin();

        unsafe
        {
            int byteOffset = elementIndex * Unsafe.SizeOf<TTo>();
            void* pointer = (byte*)_innerHandle.Pointer + byteOffset;

            return new MemoryHandle(pointer, default, this);
        }
    }

    public override void Unpin()
    {
        _innerHandle.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerHandle.Dispose();
        }
    }
}