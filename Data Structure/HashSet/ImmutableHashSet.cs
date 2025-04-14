using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ImmutableHashSet<T> : ISerializationCallbackReceiver, IEnumerable<T>
{
    private readonly HashSet<T> _Hash;
    

    public void OnAfterDeserialize()
    {
        throw new System.NotImplementedException();
    }

    public void OnBeforeSerialize()
    {
        throw new System.NotImplementedException();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _Hash.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
