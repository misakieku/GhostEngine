using System;

namespace Ghost.Core.Collections;

public class RingBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _count;
    
    public int Count => _count;

    public RingBuffer(int capacity)
    {
        _buffer = new T[capacity];
    }

    public void Push(T item)
    {
        if (_count < _buffer.Length)
        {
            _buffer[(_head + _count) % _buffer.Length] = item;
            _count++;
        }
        else
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;
        }
    }

    public T Pop()
    {
        if (_count == 0) throw new InvalidOperationException("Ring buffer is empty.");
        _count--;
        var item = _buffer[(_head + _count) % _buffer.Length];
        _buffer[(_head + _count) % _buffer.Length] = default!; // Clear reference
        return item;
    }

    public bool TryPop(out T? item)
    {
        if (_count == 0)
        {
            item = default;
            return false;
        }
        
        _count--;
        item = _buffer[(_head + _count) % _buffer.Length];
        _buffer[(_head + _count) % _buffer.Length] = default!; // Clear reference
        return true;
    }

    public T Peek()
    {
        if (_count == 0) throw new InvalidOperationException("Ring buffer is empty.");
        return _buffer[(_head + _count - 1) % _buffer.Length];
    }

    public bool TryPeek(out T? item)
    {
        if (_count == 0)
        {
            item = default;
            return false;
        }

        item = _buffer[(_head + _count - 1) % _buffer.Length];
        return true;
    }

    public void Clear()
    {
        _head = 0;
        _count = 0;
        Array.Clear(_buffer, 0, _buffer.Length);
    }
}
