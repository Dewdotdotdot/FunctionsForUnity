using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using static UnityEditor.Progress;


public class ThreadSafeHashSet<T> : IDisposable, ISet<T>
{
    private readonly HashSet<T> _internal;
    private readonly ReaderWriterLockSlim _lock;

    public ThreadSafeHashSet() 
    {
        _internal = new HashSet<T>();
        _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    }

    public ThreadSafeHashSet(List<T> list)
    {
        _internal = new HashSet<T>(list);
        _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    }

    public ThreadSafeHashSet(HashSet<T> _lnternal)
    {
        this._internal = _lnternal;
        _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    }

    public int Count => _internal.Count;

    public bool IsReadOnly => false;
    public void Dispose()
    {
        _internal.Clear();
        _lock.ExitWriteLock();
        try
        {
            _lock.Dispose();
        }
        finally { _lock.Dispose(); }
    }

    public bool Add(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            return _internal.Add(item);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _internal.Clear();
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool Contains(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            return _internal.Contains(item);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _lock.EnterWriteLock();
        try
        {
            _internal.CopyTo(array, arrayIndex);
        }
        finally { _lock.ExitWriteLock(); }
    }


    public void ExceptWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            _internal.ExceptWith(other);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public IEnumerator<T> GetEnumerator()
    {
        _lock.EnterWriteLock();
        try
        {
            return _internal.GetEnumerator();
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void IntersectWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            _internal.IntersectWith(other);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            return _internal.IsProperSubsetOf(other);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            return _internal.IsProperSupersetOf(other);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            return _internal.IsSubsetOf(other);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            return _internal.IsSupersetOf(other);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            return _internal.Overlaps(other);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool Remove(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            return _internal.Remove(item);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            return _internal.SetEquals(other);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            _internal.SymmetricExceptWith(other);
        }
        finally { _lock.ExitWriteLock(); }
    }

    public void UnionWith(IEnumerable<T> other)
    {
        _lock.EnterWriteLock();
        try
        {
            _internal.UnionWith(other);
        }
        finally { _lock.ExitWriteLock(); }
    }

    void ICollection<T>.Add(T item)
    {
        _lock.EnterWriteLock();
        try
        {
            _internal.Add(item);
        }
        finally { _lock.ExitWriteLock(); }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        _lock.EnterWriteLock();
        try
        {
            return _internal.GetEnumerator();
        }
        finally { _lock.ExitWriteLock(); }
    }
}
