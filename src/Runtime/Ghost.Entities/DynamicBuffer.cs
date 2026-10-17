using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public unsafe struct DynamicBuffer<T>
    where T : unmanaged, IBufferElement
{
    private readonly T* _pBase;
    private readonly T** _pExternalPtr;
    private readonly int _inlineCapacity;

    private int _capacity;
    private int _count;

    public readonly int Capacity => _capacity;
    public readonly int Count => _count;

    internal DynamicBuffer(T* pBase)
    {
        _inlineCapacity = ComponentTypeID<T>.InlineBufferCapacity;

        _pBase = pBase;
        _pExternalPtr = (T**)(_pBase + _inlineCapacity);

        _capacity = _inlineCapacity;
        _count = 0;
    }

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            CheckIndex(index);
            return ref _pBase[index];
        }
    }

    [Conditional("GHOST_SAFETY_CHECKS")]
    private readonly void CheckIndex(int index)
    {
        if (index < 0 || index >= _count)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range for DynamicBuffer<{typeof(T).Name}> with count {_count}.");
        }
    }

    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity > _capacity)
        {
            var newCapacity = Math.Max(requiredCapacity, _capacity * 2);

            if (*_pExternalPtr == null)
            {
                var newBuffer = (T*)MemoryUtility.Malloc((nuint)(newCapacity * sizeof(T)));
                *_pExternalPtr = newBuffer;
            }
            else
            {
                var newBuffer = (T*)MemoryUtility.Realloc(*_pExternalPtr, (nuint)(newCapacity * sizeof(T)));
                *_pExternalPtr = newBuffer;
            }

            _capacity = newCapacity;
        }
    }

    public void Add(T item)
    {
        EnsureCapacity(_count + 1);

        if (_capacity > _inlineCapacity)
        {
            var index = _count - _inlineCapacity;
            *_pExternalPtr[index] = item;
        }
        else
        {
            _pBase[_count] = item;
        }

        _count++;
    }

    public void RemoveAt(int index)
    {
        CheckIndex(index);

        if (_capacity > _inlineCapacity)
        {
            var externalIndex = index - _inlineCapacity;
            for (var i = externalIndex; i < _count - 1; i++)
            {
                (*_pExternalPtr)[i] = (*_pExternalPtr)[i + 1];
            }
        }
        else
        {
            for (var i = index; i < _count - 1; i++)
            {
                _pBase[i] = _pBase[i + 1];
            }
        }

        _count--;
    }

    public void RemoveAtSwapBack(int index)
    {
        CheckIndex(index);

        if (_capacity > _inlineCapacity)
        {
            var externalIndex = index - _inlineCapacity;
            (*_pExternalPtr)[externalIndex] = (*_pExternalPtr)[_count - 1];
        }
        else
        {
            _pBase[index] = _pBase[_count - 1];
        }

        _count--;
    }

    public void Clear()
    {
        _count = 0;
    }
}
