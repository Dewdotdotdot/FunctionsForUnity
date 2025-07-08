using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableQueue<T> : IEnumerable<T>
{
    private Queue<T> _queue = new Queue<T>();

#if UNITY_EDITOR
    [SerializeField]
    private List<T> _editorList = new List<T>();
#endif

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
#if UNITY_EDITOR
        _editorList.Add(item);
#endif
    }

    public T Dequeue()
    {
        if (_queue.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        T item = _queue.Dequeue();
#if UNITY_EDITOR
        _editorList.RemoveAt(0);
#endif
        return item;
    }

    public T Peek()
    {
        if (_queue.Count == 0)
            throw new InvalidOperationException("Queue is empty.");
        return _queue.Peek();
    }

    public void Clear()
    {
        _queue.Clear();
#if UNITY_EDITOR
        _editorList.Clear();
#endif
    }

    public int Count => _queue.Count;

    public bool Contains(T item) => _queue.Contains(item);

    public T[] ToArray() => _queue.ToArray();

    public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => $"SerializableQueue(Count={Count})";

    public static implicit operator Queue<T>(SerializableQueue<T> serializableQueue)
    {
        return new Queue<T>(serializableQueue._queue);
    }

    public static implicit operator SerializableQueue<T>(Queue<T> queue)
    {
        var wrapper = new SerializableQueue<T>();
        foreach (var item in queue)
            wrapper.Enqueue(item);
        return wrapper;
    }

    // Unity 직렬화 후 역직렬화 시 호출 필요 (Editor → 런타임 동기화)
#if UNITY_EDITOR
    [ContextMenu("Sync Editor List To Queue")]
    public void SyncEditorToRuntime()
    {
        _queue.Clear();
        foreach (var item in _editorList)
            _queue.Enqueue(item);
    }

    [ContextMenu("Sync Queue To Editor List")]
    public void SyncRuntimeToEditor()
    {
        _editorList.Clear();
        _editorList.AddRange(_queue);
    }
#endif
}