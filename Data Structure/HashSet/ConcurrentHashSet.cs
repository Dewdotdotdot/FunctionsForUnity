using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class ConcurrentHashSet<T> : ISerializationCallbackReceiver, IReadOnlyCollection<T>,  ISet<T>, IDisposable
{
    private ConcurrentDictionary<T, byte> _internal;

    public int Count => _internal.Keys.Count;

    public bool IsReadOnly => false;

    public ConcurrentHashSet()
    {
        _internal = new ConcurrentDictionary<T, byte>();

#if UNITY_EDITOR
        _HashSet = new List<T>();
#endif
    }

    public ConcurrentHashSet(IEnumerable<T> collection)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));
        _internal = new ConcurrentDictionary<T, byte>(collection.ToDictionary(v => v, v => (byte)0));

#if UNITY_EDITOR
        _HashSet = new List<T>(collection);
#endif
    }
    public ConcurrentHashSet(List<T> values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        _internal = new ConcurrentDictionary<T, byte>(values.ToDictionary(v => v, v => (byte)0));

#if UNITY_EDITOR
        _HashSet = new List<T>(values);
#endif
    }
    public ConcurrentHashSet(Dictionary<T, byte> values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        _internal = new ConcurrentDictionary<T, byte>(values, EqualityComparer<T>.Default);

#if UNITY_EDITOR
        _HashSet = new List<T>();
#endif
    }

#if UNITY_EDITOR
    [SerializeField] private List<T> _HashSet;
    public void OnBeforeSerialize()
    {
        _HashSet.Clear();
        foreach (var item in _internal.Keys)
        {
            _HashSet.Add(item);
        }
    }
    public void OnAfterDeserialize()
    {
        _internal = new ConcurrentDictionary<T, byte>(_HashSet.ToDictionary(v => v, v => (byte)0));
    }
#else
    public void OnAfterDeserialize() { }
    public void OnBeforeSerialize() { }
#endif

    public void Dispose()
    {
        _internal.Clear();
#if UNITY_EDITOR
        _HashSet.Clear();
        _HashSet = null;
#endif
    }

    public bool Add(T item)
    {
        return _internal.TryAdd(item, 0);
    }
    void ICollection<T>.Add(T item)
    {
        Add(item);
    }


    public bool Remove(T item)
    {
        return _internal.TryRemove(item, out _);
    }

    public bool Contains(T item)
    {
        return _internal.ContainsKey(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _internal.Keys.CopyTo(array, arrayIndex);
    }

    public void UnionWith(IEnumerable<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        foreach (var item in other)
        {
            Add(item);
        }
    }
    public void ExceptWith(IEnumerable<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        foreach (var item in other)
        {
            Remove(item);
        }

    }
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        var set = new HashSet<T>(other);
        foreach (var item in set)
        {
            if (!Remove(item))
            {
                Add(item);
            }
        }
    }
    public void IntersectWith(IEnumerable<T> other)     //교집합
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        var set = new HashSet<T>(other);
        foreach (var item in this)
        {
            if (!set.Contains(item))
            {
                Remove(item);
            }
        }
    }


    public bool IsSubsetOf(IEnumerable<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        var set = new HashSet<T>(other);
        foreach (var item in this)
        {
            if (!set.Contains(item))
            {
                return false;
            }
        }
        return true;
    }
    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        var set = new HashSet<T>(other);
        if (set.Count >= Count)
        {
            return false;
        }
        return IsSupersetOf(set);
    }
    public bool IsSupersetOf(IEnumerable<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        foreach (var item in other)
        {
            if (!Contains(item))
            {
                return false;
            }
        }
        return true;
    }
    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        var set = new HashSet<T>(other);
        if (set.Count >= Count)
        {
            return false;
        }
        return IsSupersetOf(set);
    }

    public bool Overlaps(IEnumerable<T> other)      // 겹치는 원소가 있는지 체크
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        foreach (var item in other)
        {
            if (Contains(item))
            {
                return true;
            }
        }
        return false;
    }


    public bool SetEquals(IEnumerable<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        var set = new HashSet<T>(other);
        if (set.Count != Count)
        {
            return false;
        }
        return IsSubsetOf(set);
    }

    public void Clear()
    {
        _internal.Clear();
    }

    public IEnumerable<T> GetItems() { return _internal.Keys; }
    public IEnumerator<T> GetEnumerator()
    {
        return _internal.Keys.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}
