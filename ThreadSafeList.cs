using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class ThreadSafeList<T> : IList<T>, IEnumerable<T>, IDisposable, IEnumerable
{
    private object _lock = new object();

    private List<T> _list;

    public T this[int index]
    {
        get
        {
            lock (_lock)
            {
                if (index < 0 || index > _list.Count)
                    throw new IndexOutOfRangeException("index Error");
                return _list[index];
            }
        }
    }
    public int Count
    {
        get
        {
            return _list.Count;
        }
    }

    public bool IsReadOnly => false;

    T IList<T>.this[int index]
    {
        get
        {
            lock (_lock)
            {
                if (index >= _list.Count || index < 0)
                    throw new ArgumentOutOfRangeException("index Error");
                return _list[index];
            }
        }
        set
        {
            lock (_lock)
            {
                if (index >= _list.Count || index < 0)
                    throw new ArgumentOutOfRangeException("index Error");
                _list[index] = value;
            }
        }
    }

    public ThreadSafeList()
    {
        lock (_lock)
        {
            _list = new List<T>();
        }
    }
    public ThreadSafeList(int capacity)
    {
        lock (_lock)
        {
            if (capacity <= 0)
                throw new ArgumentException("capacity <= 0");
            _list = new List<T>(capacity);
        }
    }
    public ThreadSafeList(IEnumerable<T> collection)
    {
        lock (_lock)
        {
            if (collection == null)
                throw new ArgumentException("Null Reference");
            _list = new List<T>(collection);
        }
    }

    public void Add(T item)
    {
        lock (_lock)
        {
            _list.Add(item);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _list.Clear();
        }
    }

    public void AddRange(IEnumerable<T> collection)
    {
        lock (_lock)
        {
            if (collection == null)
                throw new ArgumentNullException("Null Reference");
            _list.AddRange(collection);
        }
    }

    public bool Contains(T item)
    {
        lock (_lock)
        {
            return _list.Contains(item);
        }

    }
    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (_lock)
        {
            _list.CopyTo(array, arrayIndex);
        }
    }
    public int IndexOf(T item)
    {
        lock (_lock)
        {
            return _list.IndexOf(item);
        }

    }
    public int IndexOf(T item, int index, int count)
    {
        lock (_lock)
        {
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException("index Error");
            if (count < 0 || index + count > _list.Count)
                throw new ArgumentOutOfRangeException("count Error");

            return _list.IndexOf(item, index, count);
        }
    }
    public int IndexOf(T item, int index)
    {
        lock (_lock)
        {
            if (index >= _list.Count || index < 0)
                throw new ArgumentOutOfRangeException("index Error");
            return _list.IndexOf(item, index);
        }
    }

    public List<T> GetRange(int index, int count)
    {
        lock (_lock)
        {
            if (index >= _list.Count || index < 0)
                throw new ArgumentOutOfRangeException("index Error");
            if (count < 0 || index + count > _list.Count)
                throw new ArgumentOutOfRangeException("count");
            return _list.GetRange(index, count);
        }
    }

    public bool Exists(Predicate<T> match)
    {
        lock (_lock)
        {
            if (match == null)
                throw new ArgumentNullException("match is Null");
            return  _list.Exists(match);
        }
    }
    public T Find(Predicate<T> match)
    {
        lock (_lock)
        {
            if (match == null)
                throw new ArgumentNullException("match is Null");
            return _list.Find(match);
        }

    }
    public List<T> FindAll(Predicate<T> match)
    {
        lock (_lock)
        {
            if(match == null)
                throw new ArgumentNullException("match is Null");
            return _list.FindAll(match);
        }

    }

    public void Insert(int index, T item)
    {
        lock (_lock)
        {
            if (index >= _list.Count || index < 0)
                throw new ArgumentOutOfRangeException("index Error");
            _list.Insert(index, item);
        }
    }

    public void RemoveAt(int index)
    {
        lock (_lock)
        {
            if (index >= _list.Count || index < 0)
                throw new ArgumentOutOfRangeException("index Error");
            _list.RemoveAt(index);
        }
    }

    public bool Remove(T item)
    {
        lock (_lock)
        {
            return _list.Remove(item);
        }
    }
    public int RemoveAll(Predicate<T> match)
    {
        lock ( _lock)
        {
            if (match == null)
                throw new ArgumentNullException("match");
            return _list.RemoveAll(match);
        }
    }
    public void RemoveRange(int index, int count)
    {
        lock (_lock)
        {
            if (index >= _list.Count || index < 0)
                throw new ArgumentOutOfRangeException("index Error");
            if (count < 0 || index + count > _list.Count)
                throw new ArgumentOutOfRangeException("count");
            _list.RemoveRange(index, count);
        }
    }
    public void Reverse(int index, int count) 
    {
        lock (_lock)
        {
            if (index >= _list.Count || index < 0)
                throw new ArgumentOutOfRangeException("index Error");
            if (count < 0 || index + count > _list.Count)
                throw new ArgumentOutOfRangeException("count");
            _list.Reverse(index, count);
        }
    }
    public void Reverse()
    {
        lock(_lock)
        {
            _list.Reverse();
        }
    }

    public void Sort(Comparison<T> comparison)
    {
        lock (_lock)
        {
            if (comparison == null)
                throw new ArgumentNullException("Comparison is Null");
            _list.Sort(comparison);
        }
    }
    public void Sort(int index, int count, IComparer<T> comparer)
    {
        lock ( _lock)
        {
            if (index >= _list.Count || index < 0)
                throw new ArgumentOutOfRangeException("index Error");
            if (count < 0 || index + count > _list.Count)
                throw new ArgumentOutOfRangeException("count");
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            _list.Sort(index, count, comparer);
        }
    }
    public void Sort(IComparer<T> comparer)
    {
        lock (_lock)
        {
            if(comparer == null) 
                throw new ArgumentNullException(nameof(comparer));
            _list.Sort(comparer);
        }
    }
    public void Sort()
    {
        lock (_lock)
        {
            _list.Sort();
        }
    }

    public T[] ToArray()
    {
        lock (_lock)
        {
            return _list.ToArray();
        }
    }



    public IEnumerator<T> GetEnumerator()
    {
        lock (_lock)
        {
            return _list.GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        lock (_lock)
        {
            return _list.ToArray().GetEnumerator();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _list.Clear();
            _list = null;
            _lock = null;
        }
    }


}
