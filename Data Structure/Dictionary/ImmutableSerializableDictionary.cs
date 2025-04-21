using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ArchiementTest;
using UnityEngine;


[System.Serializable]
public sealed class ImmutableSerializableDictionary<K, V> : ISerializationCallbackReceiver, IReadOnlyDictionary<K, V>
{
#if UNITY_EDITOR
    [System.Serializable]
    internal struct KVP<K, V>
    {
        internal KVP(KeyValuePair<K, V> pair)
        {
            key = pair.Key;
            value = pair.Value;
        }
        [SerializeField] internal K key; [SerializeField] internal V value;
    }
    [Header("Immutable Data(Can not change)")]
    [SerializeField] private List<KVP<K, V>> _serializedData;
    public void OnBeforeSerialize()
    {
        if (_internal == null)
            return;
        if(_serializedData == null)
            _serializedData = new List<KVP<K, V>>();
        _serializedData.Clear();
        var list = _internal.ToList<KeyValuePair<K, V>>();
        for(int i = 0; i < list.Count; i++)
        {
            var pair = list[i];
            _serializedData.Add(new KVP<K, V>(pair));
        }
    }
    public void OnAfterDeserialize() 
    {
        //Immutable은 Inspector 조작도 차단
    }

#else
    public void OnAfterDeserialize() { }
    public void OnBeforeSerialize() { }
#endif

    private readonly Dictionary<K, V> _internal;

    private ImmutableSerializableDictionary() { }       //단순 생성 차단

    private ImmutableSerializableDictionary(Dictionary<K, V> dictionary)
    {
        _internal = dictionary;
    }

    public static ImmutableSerializableDictionary<K, V> Create(IDictionary<K, V> dictionary, EqualityComparer<K> comparer = null)
    {
        if (dictionary == null) 
            throw new ArgumentNullException(nameof(dictionary));

        Dictionary<K,V> dic = new Dictionary<K,V>(dictionary, comparer == null ? EqualityComparer<K>.Default : comparer);
        return new ImmutableSerializableDictionary<K,V>(dic);
    }

    public static ImmutableSerializableDictionary<K, V> Create(IEnumerable<KeyValuePair<K, V>> kvPairs, EqualityComparer<K> comparer = null)
    {
        if (kvPairs == null)
            throw new ArgumentNullException(nameof(kvPairs));
        Dictionary<K, V> dic = new Dictionary<K, V>(kvPairs, comparer == null ? EqualityComparer<K>.Default : comparer);
        return new ImmutableSerializableDictionary<K, V>(dic);
    }

    public int Count => _internal.Count;

    public IEnumerable<K> Keys => _internal.Keys;

    public IEnumerable<V> Values => _internal.Values;

    public V this[K key] => _internal[key];

    public bool ContainsKey(K key) => _internal.ContainsKey(key);

    public bool TryGetValue(K key, out V value) => _internal.TryGetValue(key, out value);

    public IEnumerable<KeyValuePair<K, V>> AsEnumerable() => _internal.AsEnumerable();

    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _internal.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _internal.GetEnumerator();

}
