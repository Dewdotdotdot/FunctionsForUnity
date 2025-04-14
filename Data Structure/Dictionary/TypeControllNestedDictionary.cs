using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;


[System.Serializable]
public class TypeControllNestedDictionary<K, V> : ISerializationCallbackReceiver
{
    private SerializableConcurrentDictioanry<Type, SerializableConcurrentDictioanry<K, V>> _internal;

#if UNITY_EDITOR
    [System.Serializable]
    private struct KVP<Key, Value>
    {
        internal KVP(Key k, Value v)
        {
            key = k;
            value = v;
        }
        internal KVP(KeyValuePair<Key, Value> pair)
        {
            key = pair.Key;
            value = pair.Value;
        }
        public Key key; public Value value;
    }
    [System.Serializable]
    private class StringToPairList
    {
        internal StringToPairList(string key, List<KVP<K, V>> list)
        {
            this.key = key;
            this.pairList = list;
        }
        public string key;
        public List<KVP<K, V>> pairList;
    }
    //이게 문제임
    [SerializeField] private List<StringToPairList> internalData = new List<StringToPairList>();
    public void OnBeforeSerialize()
    {
        if (_internal == null)
            return;
        if(internalData == null)
        {
            internalData = new List<StringToPairList>();
        }
        else    //이게 문제임
        {
            for (int i = 0; i < internalData.Count; i++)
            {
                internalData[i].pairList.Clear();       //해제 처리
                internalData[i].pairList = null;
                internalData[i].key = null;
            }
            internalData.Clear();
        }

        var ks = _internal.Keys;
        foreach (var key in ks)
        {
            string keyName = key.Name;
            var value = _internal[key];
            List<KVP<K,V>> list = new List<KVP<K, V>>();

            var nKs = value.Keys;
            foreach (var k in nKs)
            {
                if (_internal[key].TryGetValue(k, out V v))
                {
                    list.Add(new KVP<K, V>(k, v));
                }
                else
                {
                    list.Add(new KVP<K, V>());
                }
            }
            internalData.Add(new StringToPairList(keyName, list));
        }

    }

    public void OnAfterDeserialize()
    {
        //변경 금지 처리
    }
#else
    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize(){ }
#endif

    public TypeControllNestedDictionary()
    {
        IDictionary<Type, SerializableConcurrentDictioanry<K, V>> dic = new Dictionary<Type, SerializableConcurrentDictioanry<K, V>>();
        _internal = SerializableConcurrentDictioanry<Type, SerializableConcurrentDictioanry<K, V>>.Create(dic);
    }
    public ICollection<K> NestedKeys(Type type) { return _internal[type].Keys; }
    public ICollection<V> NestedValues(Type type) { return _internal[type].Values; }

    public IEnumerable<KeyValuePair<K, V>> NestedPair(Type type) { return _internal[type]; }

    public bool TryGetValue(Type type, out SerializableConcurrentDictioanry<K, V> value)
    {
        value = null;
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;
        return _internal.TryGetValue(typeof(Type), out value);
    }
    public bool TryNestedGetValue(Type type, K k, out V v)
    {
        v = default(V);
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;
        if (_internal[type].ContainsKey(k) == false)
            return false;
        return _internal[type].TryGetValue(k, out v);
    }

    public bool ContainsKey(Type type)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;

        return _internal.ContainsKey(type);
    }
    public bool NestedContainsKey(Type type, K k)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;

        if (ContainsKey(type) == false)
            return false;

        return _internal[type].ContainsKey(k);
    }

    public bool TryAdd(Type type, IDictionary<K, V> dic)     //Dictionary를 추가
    {
        if (ContainsKey(type) == true)
            return false;
        
        return _internal.TryAdd(type, SerializableConcurrentDictioanry<K, V>.Create(dic));
    }

    public bool TryAdd(Type type, SerializableConcurrentDictioanry<K, V> dic)     //Dictionary를 추가
    {
        if (ContainsKey(type) == true)
            return false;
        
        return _internal.TryAdd(type, dic);
    }
    public bool TryNestedAdd(Type type, K k, V v)   //데이터를 추가
    {
        if (typeof(K).IsAssignableFrom(type)  == false)  //K와 동일한지     
            return false;      
     
        if (_internal.ContainsKey(type) == false)
        {
            return false;
        }

        if (_internal[type].ContainsKey(k) == true)     //업데이트를 해야됨
            return false;
        Debug.Log("Added");
        _internal[type].Add(k, v);
        return true;
    }

    public bool TryNestedAdd(Type type, KeyValuePair<K, V> pair)   //데이터를 추가
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;

        if (_internal.ContainsKey(type) == false)
        {
            return false;
        }

        if (_internal[type].ContainsKey(pair.Key) == true)     //업데이트를 해야됨
            return false;

        _internal[type].Add(pair);
        return true;
    }


    public bool TryAddOrUpdate(Type type, IDictionary<K, V> dic)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == true)     // 업데이트
        {
            var s = _internal[type];
            s.Clear();
            s = SerializableConcurrentDictioanry<K, V>.Create(dic);
            _internal[type] = s;
        }
        else //Add
        {
            _internal.TryAdd(type, SerializableConcurrentDictioanry<K, V>.Create(dic));
        }
        return true;
    }
    public bool TryAddOrUpdate(Type type, SerializableConcurrentDictioanry<K, V> dic)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == true)     // 업데이트
        {
            _internal[type].Clear();
            _internal[type] = dic;
        }
        else //Add
        {
            _internal.TryAdd(type, dic);
        }
        return true;
    }

    public bool TryNestedAddOrUpdate(Type type, K k , V v)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;

        if(_internal[type].ContainsKey(k) == false)
            return false;
        return _internal[type].TryAdd(k, v);
    }
    public bool TryNestedAddOrUpdate(Type type, KeyValuePair<K, V> pair)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;
        if (_internal[type].ContainsKey(pair.Key) == false)     
            return false;
        return _internal[type].TryAdd(pair.Key, pair.Value);
    }

    public bool TryRemove(Type type)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;
        return _internal.Remove(type);
    }
    public bool TryNestedRemove(Type type, K k) 
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;
        if (_internal[type].ContainsKey(k) == false)
            return false;
        return _internal[type].Remove(k);
    }

    public bool TryUpdate(Type type, IDictionary<K, V> dic)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;
        return _internal.TryUpdate(type, SerializableConcurrentDictioanry < K, V >.Create(dic), _internal[type]);
    }
    public bool TryUpdate(Type type, SerializableConcurrentDictioanry<K, V> dic)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;
        return _internal.TryUpdate(type, dic, _internal[type]);
    }
    public bool TryNestedUpdate(Type type, K k, V v)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;
        if (_internal[type].ContainsKey(k) == false)
            return false;
        return _internal[type].TryUpdate(k, v, _internal[type][k]);
    }
    public bool TryNestedUpdate(Type type, KeyValuePair<K, V> pair)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;
        if (_internal[type].ContainsKey(pair.Key) == false)
            return false;
        return _internal[type].TryUpdate(pair.Key, pair.Value, _internal[type][pair.Key]);
    }

    public bool TryClear(Type type)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;
        _internal.Clear();
        return true;
    }
    public bool TryNestedClear(Type type, K k)
    {
        if (typeof(K).IsAssignableFrom(type) == false)  //K와 동일한지     
            return false;
        if (_internal.ContainsKey(type) == false)
            return false;
        if (_internal[type].ContainsKey(k) == false)
            return false;
        _internal[type].Clear();
        return true;
    }


}