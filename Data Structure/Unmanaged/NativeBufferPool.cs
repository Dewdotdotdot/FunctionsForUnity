namespace NativeBufferPool
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Numerics;
    using UnityEngine;
    using Unity.Collections;
    using Unity.Collections.LowLevel;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Burst;
    using System.Buffers;
    using System.Collections.Generic;


    /* 일단 아래에 해당되는 AI써서 구현한거임
     * 전부 이상해서 참고용으로만
     * NativeRefBufferPool에서 NativeMemory는 Unity에서 사용이 안됨 ai이상함
     * Unity 환경에서는 GCFreeNativeBufferPool 권장한다는데 걍 NativeArray 쓰십쇼
     * GCFreeNativeBufferPool랑 HybridNativeBufferPool은 ai가 만든건데 뭔가 이상함
     * MarshalBufferPool은 사용x 매우느림
     */

    // UnsafePointerBufferPool부분에서 ConcurrentStack 쓰는거 때문에 완전한 GC-Free는 아님
    //아래 참조

    public class NativeBufferPool
    {
        // NativeMemory + ref T Pool => NativeMemory가 Unity에서 사용 불가능, ai가 out ref T value 말하는데 unity에서는 안됨 => c#버전이 다름
#if !UNITY_STANDALONE
        public unsafe class NativeRefBufferPool<T> where T : unmanaged
        {
            private IntPtr buffer;
            private bool[] used;
            private int capacity;
            private object locker = new();

            public NativeRefBufferPool(int count)
            {
                capacity = count;
                used = new bool[count];
                buffer = NativeMemory.Alloc((nuint)(sizeof(T) * count));
            }

            public int Alloc()
            {
                lock (locker)
                {
                    for (int i = 0; i < capacity; i++)
                    {
                        if (!used[i])
                        {
                            used[i] = true;
                            return i;
                        }
                    }
                }
                throw new InvalidOperationException("No more space");
            }

            public void Free(int index)
            {
                lock (locker)
                {
                    used[index] = false;
                }
            }

            public ref T GetRef(int index)
            {
                return ref Unsafe.AsRef<T>((void*)(buffer + index * sizeof(T)));
            }

            public void Dispose()
            {
                NativeMemory.Free(buffer);
                buffer = IntPtr.Zero;
            }
        }
#endif

        //매우 느림
        public class MarshalBufferPool<T> where T : struct
        {
            private IntPtr buffer;
            private bool[] used;
            private int capacity;
            private int size;
            private object locker = new();

            public MarshalBufferPool(int count)
            {
                capacity = count;
                size = Marshal.SizeOf<T>();
                buffer = Marshal.AllocHGlobal(size * count);
                used = new bool[count];
            }

            public int Alloc()
            {
                lock (locker)
                {
                    for (int i = 0; i < capacity; i++)
                    {
                        if (!used[i])
                        {
                            used[i] = true;
                            return i;
                        }
                    }
                }
                throw new InvalidOperationException("No more space");
            }

            public void Free(int index)
            {
                lock (locker)
                {
                    used[index] = false;
                }
            }

            public T Get(int index)
            {
                IntPtr ptr = buffer + index * size;
                return Marshal.PtrToStructure<T>(ptr);
            }

            public void Set(int index, T value)
            {
                IntPtr ptr = buffer + index * size;
                Marshal.StructureToPtr(value, ptr, false);
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(buffer);
                buffer = IntPtr.Zero;
            }
        }


        /// <summary>
        ///  Unmanaged Buffer Pool(use with using block)
        ///  <para>ex):   </para>
        ///  <para>int index1 = TPool.Alloc();</para>
        ///  <para>ref T value1 = ref TPool.Get(index1);</para>
        ///  <para>value1 = 100;</para>
        ///  <para>TPool.Free(index1);</para>
        ///  <para>Exception이 문제면 Try함수 사용</para>
        ///  <para>안전성 내다 던짐</para>
        /// </summary>
        /// <typeparam name="T">T is unmanaged</typeparam>
        public unsafe class UnsafePointerBufferPool<T> : System.IDisposable where T : unmanaged //where의 순서 중요
        {
            private T* buffer;              //
            private int* usedBuffer;        //Interlocked가 byte*를 사용할 수 없음(최소 4바이트 이상 요구함)
            private int capacity;
            private ConcurrentStack<int> freeStack;     //가비지 나옴(int라 많지는 않음 4 * capacity정도) => 근데 빠르고 안정적임
            private bool disposed = false;
            //private object locker;  //Interlocked이랑 목적이 다름 안전목적
            private volatile int isClearing = 0;        //volatile 사용
            private UnsafePointerBufferPool() { }       //사이즈 없이 생성 차단

            /// <param name="count">Buffer Size</param>
            public UnsafePointerBufferPool(int count)
            {
                if(count <= 0)
                    throw new ArgumentOutOfRangeException("count can not be less than 1");
                capacity = count;
                buffer = (T*)Marshal.AllocHGlobal(sizeof(T) * count);
                usedBuffer = (int*)Marshal.AllocHGlobal(sizeof(int) * count);
                

                freeStack = new ConcurrentStack<int>();
                for (int i = capacity - 1; i >= 0; i--)
                {
                    usedBuffer[i] = 0; // 0은 false (사용 가능)
                    freeStack.Push(i);  //역순으로 index값 넣음 => pop은 0부터
                }
            }

            public int Alloc()
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(UnsafePointerBufferPool<T>));
                if (isClearing == 1)
                    throw new InvalidOperationException("Buffer is being cleared. Try again later.");

                while (freeStack.TryPop(out int index))     //if의 경우, 동시성 환경에서 usedBuffer[index]가 1이면 실패처리 되어버림 -> 실패하면 다음 index를 Pop
                {                
                    if (Interlocked.Exchange(ref usedBuffer[index], 1) == 0)// 락
                        return index;
                    /*usedBuffer[index] = 1;
                    return index;*/
                }
                throw new InvalidOperationException("Pool exhausted. Consider implementing Grow().");
            }
            /// <summary>
            /// Non Exception Function
            /// </summary>
            public int TryAlloc(out bool success)
            {
                if (disposed || isClearing == 1)
                {
                    success = false;
                    return -1;
                }
                while (freeStack.TryPop(out int index))     //if의 경우, 동시성 환경에서 usedBuffer[index]가 1이면 실패처리 되어버림 -> 실패하면 다음 index를 Pop
                {
                    if (Interlocked.Exchange(ref usedBuffer[index], 1) == 0)// 락
                    {
                        success = true;
                        return index;
                    }
                }
                success = false;
                return -1;
            }

            public void Free(int index)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(UnsafePointerBufferPool<T>));
                if (isClearing == 1)
                    throw new InvalidOperationException("Buffer is being cleared. Try again later.");

                if (usedBuffer[index] == 0)
                    throw new InvalidOperationException("Accessing unallocated buffer.");            
                if (Interlocked.Exchange(ref usedBuffer[index], 0) == 1)  // 언락ㄴ
                {
                    freeStack.Push(index);
                }
                /*lock (locker)
                {
                    usedBuffer[index] = 0;
                    freeStack.Push(index);
                }*/
            }
            /// <summary>
            /// Non Exception Function
            /// </summary>
            public bool TryFree(int index)
            {
                if (disposed || isClearing == 1 || usedBuffer[index] == 0)
                    return false;

                if (Interlocked.Exchange(ref usedBuffer[index], 0) == 1)  // 언락ㄴ
                {
                    freeStack.Push(index);
                }
                return true;
            }

            public ref T Get(int index)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(UnsafePointerBufferPool<T>));
                if (isClearing == 1)
                    throw new InvalidOperationException("Buffer is being cleared. Try again later.");

                return ref Unsafe.AsRef<T>((byte*)buffer + index * Unsafe.SizeOf<T>());
                //return ref buffer[index];
            }
            /// <summary>
            /// Non Exception Function
            /// </summary>
            public ref T TryGet(int index, out bool success)
            {
                if (disposed || isClearing == 1 || index < 0 || index >= capacity)
                {
                    success = false;
                    return ref Unsafe.NullRef<T>();
                }
                success = true;
                return ref buffer[index];
            }

            public unsafe Span<T> AsSpan()
            {
                return new Span<T>(buffer, capacity);
            }

            public unsafe void Clear()
            {
                // Clear 작업 시작 전에 isClearing 상태를 원자적으로 설정
                if (Interlocked.CompareExchange(ref isClearing, 1, 0) == 1)
                    throw new InvalidOperationException("Buffer is already being cleared.");

                try
                {
                    // Clear 작업을 수행
                    for (int i = 0; i < capacity; i++)
                    {
                        usedBuffer[i] = 0; // 모든 슬롯을 비워줌
                    }

                    // freeStack을 초기화하거나 다른 작업 수행
                    freeStack.Clear();
                    for (int i = capacity - 1; i >= 0; i--)
                    {
                        freeStack.Push(i);
                    }
                }
                finally
                {
                    // Clear 작업 완료 후 isClearing 상태를 false로 변경
                    Interlocked.Exchange(ref isClearing, 0);
                }
            }

            private int spinLock = 0;
            //이거 다른 스레드에서 Free나 Alloc하는 것은 테스트 안했음
            public void Grow(int growSize)
            {
                if (growSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(growSize), "growSize is under zero.");

                if (disposed)
                    throw new ObjectDisposedException(nameof(UnsafePointerBufferPool<T>));

                if (Interlocked.CompareExchange(ref isClearing, 1, 0) == 1)
                    throw new InvalidOperationException("Buffer is currently in a transition state.");

                try
                {
                    int oldCapacity = capacity;
                    int newCapacity = capacity + growSize;

                    T* newBuffer = (T*)Marshal.AllocHGlobal(sizeof(T) * newCapacity);
                    Buffer.MemoryCopy(buffer, newBuffer, sizeof(T) * newCapacity, sizeof(T) * oldCapacity);

                    int* newUsedBuffer = (int*)Marshal.AllocHGlobal(sizeof(int) * newCapacity);
                    Buffer.MemoryCopy(usedBuffer, newUsedBuffer, sizeof(int) * newCapacity, sizeof(int) * oldCapacity);

                    Marshal.FreeHGlobal((IntPtr)buffer);
                    Marshal.FreeHGlobal((IntPtr)usedBuffer);

                    buffer = newBuffer;
                    usedBuffer = newUsedBuffer;

                    for (int i = newCapacity - 1; i >= oldCapacity; i--)
                    {
                        newUsedBuffer[i] = 0;
                        freeStack.Push(i);
                    }

                    capacity = newCapacity;
                }
                finally
                {
                    Interlocked.Exchange(ref isClearing, 0);
                }
            }

            public void Dispose()
            {
                if (!disposed)
                {
                    Marshal.FreeHGlobal((IntPtr)buffer);
                    Marshal.FreeHGlobal((IntPtr)usedBuffer);
                    freeStack.Clear();
                    buffer = null;
                    usedBuffer = null;
                    freeStack = null;
                    disposed = true;
                    GC.SuppressFinalize(this);
                }
            }


            ~UnsafePointerBufferPool()
            {
                Dispose(false);
            }
            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (!disposing)
                    {
                        Marshal.FreeHGlobal((IntPtr)buffer);
                        Marshal.FreeHGlobal((IntPtr)usedBuffer);
                        freeStack.Clear();
                        buffer = null;
                        usedBuffer = null;
                        freeStack = null;
                        GC.SuppressFinalize(this);
                    }
                    disposed = true;
                }
            }
        }

        #region 사용 x, ai 딸깍 결과물이라서 검증이 안됨, 사용 차단
        #if false
        public unsafe class GCFreeNativeBufferPool<T> : IDisposable where T : unmanaged
        {
            // 원시 메모리 블록 포인터
            private void* buffer;
            // 할당 가능한 슬롯 수
            private readonly int capacity;
            // free list: 사용 가능한 슬롯의 인덱스를 저장
            private int[] freeList;
            // free list의 현재 인덱스 (스택 구조로 사용)
            private int freeIndex;
            // 각 슬롯의 크기 (T의 크기)
            private readonly int elementSize;

            // 할당에 사용되는 Allocator (예: Allocator.Persistent)
            private Allocator allocator;

            // 생성자: count 만큼의 슬롯을 가진 메모리 풀을 초기화
            public GCFreeNativeBufferPool(int count, Allocator allocator = Allocator.Persistent)
            {
                capacity = count;
                elementSize = UnsafeUtility.SizeOf<T>();
                this.allocator = allocator;
                // UnsafeUtility.Malloc을 통해 GC-free 메모리 블록 할당
                buffer = UnsafeUtility.Malloc(elementSize * capacity, elementSize, allocator);

                // free list 초기화: 모든 인덱스가 사용 가능한 상태
                freeList = new int[capacity];
                for (int i = 0; i < capacity; i++)
                {
                    freeList[i] = i;
                }
                freeIndex = capacity - 1;
            }

            // Alloc: free list에서 하나의 슬롯 인덱스를 반환
            public int Alloc()
            {
                if (freeIndex < 0)
                {
                    throw new InvalidOperationException("No more free slots available!");
                }
                // 현재 freeIndex의 슬롯 인덱스를 가져옴
                int index = freeList[freeIndex];
                freeIndex--; // free list에서 제거 (스택 pop 연산)
                return index;
            }

            // Free: 사용이 끝난 슬롯 인덱스를 free list에 다시 추가 (스택 push 연산)
            public void Free(int index)
            {
                if (freeIndex >= capacity - 1)
                {
                    throw new InvalidOperationException("All slots are already free!");
                }
                freeIndex++;
                freeList[freeIndex] = index;
            }

            // Get: 인덱스에 해당하는 T 타입 데이터에 대한 ref 반환
            public ref T Get(int index)
            {
                // 메모리 블록의 base pointer에 index * elementSize 만큼 오프셋 계산
                void* elementPtr = (byte*)buffer + index * elementSize;
                return ref UnsafeUtility.AsRef<T>(elementPtr);
            }

            // Dispose: 할당된 메모리 블록을 해제
            public void Dispose()
            {
                if (buffer != null)
                {
                    UnsafeUtility.Free(buffer, allocator);
                    buffer = null;
                }
                freeList = null;
            }
        }
        public unsafe class HybridNativeBufferPool<T> : IDisposable where T : unmanaged
        {
            private void* buffer;
            private int capacity;
            private bool[] used;
            private Stack<int> freeStack;

            public HybridNativeBufferPool(int initialCapacity)
            {
                capacity = initialCapacity;
                long size = (long)Unsafe.SizeOf<T>() * capacity;
                buffer = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
                used = new bool[capacity];
                freeStack = new Stack<int>(capacity);
                for (int i = capacity - 1; i >= 0; i--)
                    freeStack.Push(i);
            }

            public int Allocate()
            {
                if (freeStack.Count == 0)
                    throw new InvalidOperationException("Pool exhausted. Consider implementing Grow().");

                int index = freeStack.Pop();
                used[index] = true;
                return index;
            }

            public void Release(int index)
            {
                if (!used[index])
                    throw new InvalidOperationException("Double release detected.");

                used[index] = false;
                freeStack.Push(index);
            }

            public ref T GetRef(int index)
            {
                if (!used[index])
                    throw new InvalidOperationException("Accessing unallocated slot.");
                return ref Unsafe.AsRef<T>((byte*)buffer + index * Unsafe.SizeOf<T>());
            }

            public void Dispose()
            {
                UnsafeUtility.Free(buffer, Allocator.Persistent);
                buffer = null;
            }

            public bool IsAllocated(int index) => used[index];
        }
        #endif
        #endregion


#if UNITY_2021_3_OR_NEWER
        //Job에서는 이거 사용
        public struct UnityNativeBufferPool<T> : IDisposable where T : unmanaged
        {
            private NativeArray<T> Buffer;
            private NativeArray<bool> Used;
            private Allocator allocator;

            public UnityNativeBufferPool(int count, Allocator alloc)
            {
                allocator = alloc;
                Buffer = new NativeArray<T>(count, alloc, NativeArrayOptions.UninitializedMemory);
                Used = new NativeArray<bool>(count, alloc, NativeArrayOptions.ClearMemory);
            }

            public int Alloc()
            {
                for (int i = 0; i < Buffer.Length; i++)
                {
                    if (!Used[i])
                    {
                        Used[i] = true;
                        return i;
                    }
                }
                throw new InvalidOperationException("No more space");
            }

            public void Free(int index)
            {
                Used[index] = false;
            }

            public unsafe ref T Get(int index)
            {
                return ref UnsafeUtility.ArrayElementAsRef<T>(NativeArrayUnsafeUtility.GetUnsafePtr(Buffer), index);
                //return ref Buffer[index]; // NativeArray<T>는 index 접근으로 ref 반환 가능   
                //return ref Buffer.GetRef(index);
            }

            public void Dispose()
            {
                Buffer.Dispose();
                Used.Dispose();
            }
        }
#endif
    }
}

