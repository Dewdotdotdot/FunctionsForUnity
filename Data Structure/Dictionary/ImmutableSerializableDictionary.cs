using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ImmutableSerializableDictionary<K, V> : ISerializationCallbackReceiver, IReadOnlyDictionary<K, V>
{
#if UNITY_EDITOR
    [System.Serializable]
    private struct KVP<K, V>
    {
        internal KVP(KeyValuePair<K, V> pair)
        {
            key = pair.Key;
            value = pair.Value;
        }
        internal K key; internal V value;
    }
    [Header("Immutable Data(Can not change)")]
    [SerializeField] private List<KVP<K, V>> _serializedData;
    public void OnBeforeSerialize()
    {
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
        Dictionary<K,V> dic = new Dictionary<K,V>(dictionary, comparer == null ? EqualityComparer<K>.Default : comparer);
        return new ImmutableSerializableDictionary<K,V>(dic);
    }

    public static ImmutableSerializableDictionary<K, V> Create(IEnumerable<KeyValuePair<K, V>> kvPairs)
    {

    }
    public static ImmutableSerializableDictionary<K, V> Create(IEnumerable<V> values)
    {
        List<int> test = new List<int>();
        IList<int> testb = test;
        ConcurrentBag<int> bag = new ConcurrentBag<int>();
        IEnumerable<int> c = bag;
        Dictionary<K, V> dasd = new Dictionary<K, V>();
        IEnumerable<KeyValuePair<K, V>> test3 = dasd;
    }

    public int Count => _internal.Count;

    public IEnumerable<K> Keys => _internal.Keys;

    public IEnumerable<V> Values => _internal.Values;

    public V this[K key] => _internal[key];

    public bool ContainsKey(K key) => _internal.ContainsKey(key);

    public bool TryGetValue(K key, out V value) => _internal.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
    {

    }

    IEnumerator IEnumerable.GetEnumerator()
    {

    }

}
