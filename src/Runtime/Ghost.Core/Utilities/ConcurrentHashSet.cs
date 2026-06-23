using System.Collections;
using System.Collections.Concurrent;

namespace Ghost.Core.Utilities;

public class ConcurrentHashSet<T>
    where T : notnull
{
    private class Enumerator : IEnumerator<T>
    {
        private readonly ConcurrentHashSet<T> _set;
        private readonly IEnumerator<KeyValuePair<T, byte>> _enumerator;

        public Enumerator(ConcurrentHashSet<T> set)
        {
            _set = set;
            _enumerator = _set._hashSet.GetEnumerator();
        }

        public T Current => _enumerator.Current.Key;
        object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }

    private readonly ConcurrentDictionary<T, byte> _hashSet = new ConcurrentDictionary<T, byte>();

    public int Count => _hashSet.Count;

    public bool IsEmpty => _hashSet.IsEmpty;

    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    public bool Add(T item)
    {
        return _hashSet.TryAdd(item, 0);
    }

    public bool Contains(T item)
    {
        return _hashSet.ContainsKey(item);
    }

    public bool Remove(T item)
    {
        return _hashSet.TryRemove(item, out _);
    }

    public void Clear()
    {
        _hashSet.Clear();
    }
}
