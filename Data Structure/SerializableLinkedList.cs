using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableLinkedList<T> : IList<T>, ISerializationCallbackReceiver
{
    // Node stored as struct for value semantics
    [Serializable]
    private struct Node
    {
        public T Value;
        public int Next;
        public int Prev;
    }

    // Serialized arrays
    [SerializeField] private Node[] nodes;
    [SerializeField] private int[] freeList;
    [SerializeField] private int head;
    [SerializeField] private int tail;
    [SerializeField] private int count;
    [SerializeField] private int freeCount;

    private const int DefaultCapacity = 16;

    public SerializableLinkedList(int capacity = DefaultCapacity)
    {
        capacity = Math.Max(capacity, DefaultCapacity);
        nodes = new Node[capacity];
        freeList = new int[capacity];
        head = tail = -1;
        count = 0;
        freeCount = 0;
    }

    public int Count => count;
    public bool IsReadOnly => false;

    public T this[int index]
    {
        get
        {
            int nodeIndex = GetNodeIndexAt(index);
            return nodes[nodeIndex].Value;
        }
        set
        {
            int nodeIndex = GetNodeIndexAt(index);
            nodes[nodeIndex].Value = value;
        }
    }

    public void Add(T item) => AddLast(item);

    public void AddFirst(T value)
    {
        int idx = AllocateNode(value);
        nodes[idx].Prev = -1;
        nodes[idx].Next = head;
        if (head != -1)
            nodes[head].Prev = idx;
        head = idx;
        if (tail == -1)
            tail = idx;
    }

    public void AddLast(T value)
    {
        int idx = AllocateNode(value);
        nodes[idx].Next = -1;
        nodes[idx].Prev = tail;
        if (tail != -1)
            nodes[tail].Next = idx;
        tail = idx;
        if (head == -1)
            head = idx;
    }

    public void Insert(int index, T item)
    {
        if (index == count) { AddLast(item); return; }
        int nextIdx = GetNodeIndexAt(index);
        int prevIdx = nodes[nextIdx].Prev;
        int idx = AllocateNode(item);
        nodes[idx].Prev = prevIdx;
        nodes[idx].Next = nextIdx;
        if (prevIdx != -1)
            nodes[prevIdx].Next = idx;
        else
            head = idx;
        nodes[nextIdx].Prev = idx;
    }

    public bool Remove(T item)
    {
        for (int idx = head; idx != -1; idx = nodes[idx].Next)
        {
            if (EqualityComparer<T>.Default.Equals(nodes[idx].Value, item))
            {
                RemoveNode(idx);
                return true;
            }
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        int idx = GetNodeIndexAt(index);
        RemoveNode(idx);
    }

    public void Clear()
    {
        // reset freelist
        for (int i = 0; i < count; i++)
            freeList[i] = i;
        freeCount = count;
        head = tail = -1;
        count = 0;
    }

    public bool Contains(T item)
    {
        for (int idx = head; idx != -1; idx = nodes[idx].Next)
            if (EqualityComparer<T>.Default.Equals(nodes[idx].Value, item))
                return true;
        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        int i = arrayIndex;
        for (int idx = head; idx != -1; idx = nodes[idx].Next)
            array[i++] = nodes[idx].Value;
    }

    public int IndexOf(T item)
    {
        int i = 0;
        for (int idx = head; idx != -1; idx = nodes[idx].Next, i++)
            if (EqualityComparer<T>.Default.Equals(nodes[idx].Value, item))
                return i;
        return -1;
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Enumerator as struct to avoid allocations
    public struct Enumerator : IEnumerator<T>
    {
        private readonly SerializableLinkedList<T> list;
        private int currentIndex;
        private T current;

        internal Enumerator(SerializableLinkedList<T> list)
        {
            this.list = list;
            currentIndex = list.head;
            current = default;
        }

        public T Current => current;
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (currentIndex == -1) return false;
            current = list.nodes[currentIndex].Value;
            currentIndex = list.nodes[currentIndex].Next;
            return true;
        }

        public void Reset() { currentIndex = list.head; current = default; }
        public void Dispose() { }
    }

    // Allocate node index from freelist or expand arrays
    private int AllocateNode(T value)
    {
        int idx;
        if (freeCount > 0)
        {
            idx = freeList[--freeCount];
        }
        else
        {
            idx = count;
            if (idx >= nodes.Length)
                ExpandCapacity(nodes.Length * 2);
        }
        nodes[idx].Value = value;
        nodes[idx].Next = nodes[idx].Prev = -1;
        count++;
        return idx;
    }

    private void RemoveNode(int idx)
    {
        var n = nodes[idx];
        if (n.Prev != -1)
            nodes[n.Prev].Next = n.Next;
        else
            head = n.Next;

        if (n.Next != -1)
            nodes[n.Next].Prev = n.Prev;
        else
            tail = n.Prev;

        // add to freelist
        freeList[freeCount++] = idx;
        count--;
    }

    private void ExpandCapacity(int newCapacity)
    {
        Array.Resize(ref nodes, newCapacity);
        Array.Resize(ref freeList, newCapacity);
    }

    // Serialization callbacks
    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        // nothing: arrays and primitive fields are serialized
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        // ensure freelist capacity
        if (nodes == null)
            nodes = new Node[DefaultCapacity];
        if (freeList == null || freeList.Length != nodes.Length)
            freeList = new int[nodes.Length];

        // rebuild count and freelist
        count = 0;
        int idx = head;
        while (idx != -1)
        {
            count++;
            idx = nodes[idx].Next;
        }

        // unused slots go into freelist
        freeCount = 0;
        for (int i = 0; i < nodes.Length; i++)
        {
            if (i != head && i != tail && !IsInChain(i))
                freeList[freeCount++] = i;
        }
    }

    private bool IsInChain(int idx)
    {
        for (int cur = head; cur != -1; cur = nodes[cur].Next)
            if (cur == idx) return true;
        return false;
    }

    // Helper to find node index by position
    private int GetNodeIndexAt(int index)
    {
        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(nameof(index));
        int idx = head;
        for (int i = 0; i < index; i++)
            idx = nodes[idx].Next;
        return idx;
    }
}
