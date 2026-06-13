using System.Collections;

namespace Ghost.Core.Utilities;

public class ConcurrentHashSet<T> : IDisposable
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly ConcurrentHashSet<T> _set;
        private HashSet<T>.Enumerator _enumerator;

        public Enumerator(ConcurrentHashSet<T> set)
        {
            _set = set;
            _set._lock.EnterReadLock();
            _enumerator = _set._hashSet.GetEnumerator();
        }

        public readonly T Current => _enumerator.Current;
        readonly object? IEnumerator.Current => Current;

        public void Dispose()
        {
            if (_set._lock.IsReadLockHeld)
            {
                _set._lock.ExitReadLock();
            }

            _enumerator.Dispose();
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
        }
    }

    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    private readonly HashSet<T> _hashSet = new HashSet<T>();

    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _hashSet.Count;
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                {
                    _lock.ExitReadLock();
                }
            }
        }
    }

    public bool IsEmpty
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _hashSet.Count == 0;
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                {
                    _lock.ExitReadLock();
                }
            }
        }
    }

    ~ConcurrentHashSet()
    {
        Dispose();
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public bool Add(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            return _hashSet.Add(item);
        }
        finally
        {
            if (_lock.IsWriteLockHeld)
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public bool Contains(T item)
    {
        _lock.EnterReadLock();
        try
        {
            return _hashSet.Contains(item);
        }
        finally
        {
            if (_lock.IsReadLockHeld)
            {
                _lock.ExitReadLock();
            }
        }
    }

    public bool Remove(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            return _hashSet.Remove(item);
        }
        finally
        {
            if (_lock.IsWriteLockHeld)
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _hashSet.Clear();
        }
        finally
        {
            if (_lock.IsWriteLockHeld)
            {
                _lock.ExitWriteLock();
            }
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
