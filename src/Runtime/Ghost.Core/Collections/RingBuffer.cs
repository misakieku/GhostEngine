using System.Collections;

namespace Ghost.Core.Collections;

public class RingBuffer<T> : IEnumerable<T>
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly RingBuffer<T> _ringBuffer;
        private int _index;
        public Enumerator(RingBuffer<T> ringBuffer)
        {
            _ringBuffer = ringBuffer;
            _index = -1;
        }

        public readonly T Current => _ringBuffer._buffer[(_ringBuffer._head + _index) % _ringBuffer._buffer.Length];
        readonly object? IEnumerator.Current => Current;
        
        public bool MoveNext()
        {
            if (_index + 1 >= _ringBuffer._count)
            {
                return false;
            }
            _index++;
            return true;
        }

        public void Reset()
        {
            _index = -1;
        }

        public readonly void Dispose()
        {
            // No resources to dispose
        }
    }

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

    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
