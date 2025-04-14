using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ArchiementTest;
using UnityEngine;
using System;
using Newtonsoft.Json.Linq;
using System.Linq;

//Immutable할 이유가 없음
[System.Serializable]
public class SerializableConcurrentDictioanry<Key, Value> : ISerializationCallbackReceiver, IDictionary<Key, Value>, IReadOnlyDictionary<Key, Value>, IEnumerable<KeyValuePair<Key, Value>>
{
    //Serializable
#if UNITY_EDITOR
    [System.Serializable]
    private struct KVP<Key, Value>
    {
        public KVP(KeyValuePair<Key, Value> pair)
        {
            key = pair.Key;
            value = pair.Value;
        }
        public Key key; public Value value;
    }
    [Header("Immutable Data(Can not change)")]
    [SerializeField] private List<KVP<Key, Value>> _serializedData;
    public void OnBeforeSerialize()     // Dic => List
    {
        if (_internal == null)
            return;
        if(_serializedData == null)
            _serializedData = new List<KVP<Key, Value>>();
        else
        {
            _serializedData.Clear();
        }
        IEnumerable<KeyValuePair<Key, Value>> ienumerable = _internal;
        foreach (var kvp in ienumerable)
        {
            _serializedData.Add(new KVP<Key, Value>(kvp));
        }
    }
    public void OnAfterDeserialize()    //List => Dic
    {
        if (_serializedData == null || _internal == null)
            return;
        _internal.Clear();
        for (int i = 0; i < _serializedData.Count; i++)
        {
            if (_internal.TryAdd(_serializedData[i].key, _serializedData[i].value) == false)
            {
                throw new System.ArgumentException("KeyValuePair Error");
            }
        }
    }

#else
    public void OnAfterDeserialize() { }
    public void OnBeforeSerialize() { }
#endif

    private SerializableConcurrentDictioanry() { }

    private SerializableConcurrentDictioanry(ConcurrentDictionary<Key, Value> dictionary)
    {
        this._internal = dictionary;
    }

    public static SerializableConcurrentDictioanry<Key, Value> Create(IDictionary<Key, Value> dic)
    {
        if (dic == null)
            throw new NullReferenceException("Dictionary is Null");
        ConcurrentDictionary<Key, Value> cd = new ConcurrentDictionary<Key, Value>(dic);
        return new SerializableConcurrentDictioanry<Key, Value>(cd);
    }
    public static SerializableConcurrentDictioanry<Key, Value> Create(IEnumerable<KeyValuePair<Key, Value>> pairs)
    {
        if (pairs == null)
            throw new NullReferenceException("Dictionary is Null");
        ConcurrentDictionary<Key, Value> cd = new ConcurrentDictionary<Key, Value>(pairs);
        return new SerializableConcurrentDictioanry<Key, Value>(cd);
    }

    private readonly ConcurrentDictionary<Key, Value> _internal;        //느려터짐..

    //Type Restricted Funcs
    public Value this[Key key] { get => _internal[key]; set => _internal[key] = value; }

    public bool TryGetValue(Key key, out Value value)
    {
        return _internal.TryGetValue(key, out value);
    }

    public int Count => _internal.Count;

    public bool IsReadOnly => false;

    public IDictionary<Key, Value> Duplicate { get { return new Dictionary<Key, Value>(_internal); } }

    public void Add(Key key, Value value)
    {
        try
        {
            _internal.TryAdd(key, value);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public void Add(KeyValuePair<Key, Value> item)
    {
        try
        {
            _internal.TryAdd(item.Key, item.Value);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
    public bool TryAdd(Key key, Value value)
    {
        return _internal.TryAdd(key, value);
    }
    public bool TryAdd(KeyValuePair<Key, Value> item)
    {
        return _internal.TryAdd(item.Key, item.Value);
    }

    public Value AddOrUpdate(Key key, Value value, Func<Key, Value, Value> updateValueFactory)
    {
        return _internal.AddOrUpdate(key, value, updateValueFactory);
    }
    public Value AddOrUpdate(Key key, Func<Key, Value> addValueFactory, Func<Key, Value, Value> updateValueFactory)
    {
        return _internal.AddOrUpdate(key, addValueFactory, updateValueFactory);
    }

    public bool TryUpdate(Key key, Value newValue, Value oldValue)
    {
        return _internal.TryUpdate(key, newValue, oldValue);
    }
    public bool Contains(KeyValuePair<Key, Value> item)
    {
        return _internal.ContainsKey(item.Key);
    }
    public bool ContainsKey(Key key)
    {
        return _internal.ContainsKey(key);
    }

    public void Clear()
    {
        _internal.Clear();
    }

    public bool Remove(Key key)
    {
        return _internal.Remove(key, out _);
    }

    public bool Remove(KeyValuePair<Key, Value> item)
    {
        return _internal.Remove(item.Key, out _);
    }

    public bool TryRemove(Key key, out Value value)
    {
        return _internal.TryRemove(key, out value);
    }

    public bool TryRemove(KeyValuePair<Key, Value> item, out Value value)
    {
        return _internal.TryRemove(item.Key, out value);
    }



    public void CopyTo(KeyValuePair<Key, Value>[] array, int arrayIndex)
    {
       
    }

    public KeyValuePair<Key, Value>[] ToArray() 
    {
        return _internal.ToArray();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _internal.GetEnumerator();
    }

    public IEnumerator<KeyValuePair<Key, Value>> GetEnumerator()
    {
        return _internal.GetEnumerator();
    }
    public ICollection<Key> Keys => _internal.Keys;

    public ICollection<Value> Values => _internal.Values;
    IEnumerable<Key> IReadOnlyDictionary<Key, Value>.Keys => _internal.Keys;

    IEnumerable<Value> IReadOnlyDictionary<Key, Value>.Values => _internal.Values;
}
