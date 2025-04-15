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
    using Habrador_Computational_Geometry;
    using System.Collections;

    public unsafe class UnsafeDebug
    {
        public unsafe void Debug()
        {
            unsafe
            {
                byte* a = (byte*)Marshal.AllocHGlobal(sizeof(byte) * 4);
                a[0] = 1;
                a[1] = 2;
                a[2] = 3;
                a[3] = 4;
                int* p = stackalloc int[1];
                p[0] = a[0];
                int* q = (int*)Marshal.AllocHGlobal(sizeof(int));
                *q = a[0];

                UnityEngine.Debug.Log(a[0] + "" + a[1] + "" + a[2] + "" + a[3]);
                UnityEngine.Debug.Log(p[0]);
                UnityEngine.Debug.Log(*q);
            }

        }
    }

    //베이스는 ai로 뽑고 수정한 코드    
    // fixed (UnmanagedStackNode<T>** pList = &_freeList) 이거 주의
    public unsafe class UnmanagedConcurrentStack<T> : IDisposable where T : unmanaged
    {
        private UnmanagedConcurrentStack() { }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct UnmanagedStackNode<T> where T : unmanaged
        {
            public T Data;
            public UnmanagedStackNode<T>* Next;         //좌측의 노드(헤드)
        }

        private UnmanagedStackNode<T>* head;                // 스택의 최상단, 실제 데이터의 구조
        private readonly int _maxNodes;                     
        private readonly IntPtr _unmanagedMemoryPtr;        // 메모리 시작점
        private UnmanagedStackNode<T>* _nodeBuffer;          // 실제 메모리 공간상의 노드의 시작점
        private UnmanagedStackNode<T>* _freeList;           // 다음에 사용할 수 있는 빈 노드, 자원을 관리

        /* FreeList에 대한 추가적인 설명
         * 반환된 객체를 재사용 처리하는 방법임
         * 객체가 반환되었을 때, 그 해당 객체를 저장했다가 바로 반환처리함
         * Pop할 때 freelist에 추가해주고, Push 할 때는 freelist에서 객체를 가져옴
         * 하지만 Push가 연속적으로 발생하는 경우 -> freeList에 있던 이전 사용 객체를 
         */



        public UnmanagedConcurrentStack(int maxNodes)
        {
            _maxNodes = maxNodes;

            // Allocate memory manually using Marshal
            int sizeInBytes = sizeof(UnmanagedStackNode<T>) * maxNodes;
            _unmanagedMemoryPtr = Marshal.AllocHGlobal(sizeInBytes);
            _nodeBuffer = (UnmanagedStackNode<T>*)_unmanagedMemoryPtr;
            _freeList = null;

            // 연결고리에 대한 이해 : 좌 -> 우 방향으로 연결 리스트가 연결됨, 따라서 0번 노드의 Next = null
            // Next는 헤더(좌측:이전노드 또는 하위노드)
            // freeList 배열이 가진 Next의 노드를 서로 연결시키는 작업
            for (int i = 0; i < maxNodes; i++)
            {
                var node = &_nodeBuffer[i];       //i위치에 해당하는 노드의 메모리 주소를 가져옴
                node->Next = _freeList;         //자신의 좌측 노드에 이전 노드(i=0일 경우 null)을 연결, 자신 이전에 연결된 노드가 Next임  
                _freeList = node;               // 
            }

            head = null;                        // 생성자 이후에는 항상 null => 값이 비어있음
        }

        //
        public unsafe bool TryPush(T item)
        {
            UnmanagedStackNode<T>* node;
            do
            {
                //fixed : 포인터 고정
                // UnmanagedStackNode<T>* _freeList => 실제 UnmanagedStackNode<T>가 담긴 메모리의 주소
                // &_freeList => _freeList변수의 메모리 주소(실제 구조체 주소 아님)
                // UnmanagedStackNode<T>** pList => _freeList라는 변수의 메모리 주소를 담는 이중 포인터
                // UnmanagedStackNode<T>** pList = &_freeList => _freeList 변수의 메모리 주소를 이중 포인터에 담음
                fixed (UnmanagedStackNode<T>** pList = &_freeList)
                {
                    node = _freeList;
                    if (node == null)
                        return false; // 자유 리스트가 비어 있음  ==> head.Count == maxCount 상태, 가져올 자유 리스트가 없음

                    //Interlocked.CompareExchange(ref A, B , C) => if(A == C) { ref Temp = A; ref A = B; return Temp; }
                    // *(IntPtr*)pList => pList가 가리키는 _freeList 변수의 메모리 위치의 값을 Inptr로 받아옴
                    // *(IntPtr*)pList == (IntPtr)node인지 대조
                    // 둘이 같으면 pList를 node->Next로 옮김
                    // 이때 원본 pList가 (IntPtr)node와 동일한지 확인
                    //      => pList와 node가 동일해야 node->Next로 pList가 변경이 가능함
                    //      => 만약 불일치하면, _freeList는 공유변수이기에 아래의 내용을 수행중에 다른 스레드가 _freeList의 값을 변경한 것
                    //          =>현재 스레드가 _freeList의 값을 node에서 읽어온 시점과 CompareExchange를 수행하는 시점 사이에 다른 스레드가 _freeList를 변경한 것
                    if (Interlocked.CompareExchange(ref *(IntPtr*)pList, (IntPtr)node->Next, (IntPtr)node) == (IntPtr)node)
                    {
                        break; // 자유 리스트에서 노드 획득 성공
                    }
                    // CompareExchange 실패 시 다른 스레드가 자유 리스트를 변경했으므로 재시도
                    // 위로 돌아가서 pList부터 다시 할당받음 
                }
            } while (true);

            //do-while문이라서 무조건 node는 현재 _freeList가 할당됨
            //head의 길이가 maxCount에 도달하면 freeList = null이 되어서 return false됨
            node->Data = item;

            //_freeList에서 가져온 node를 head에 연결
            fixed (UnmanagedStackNode<T>** pHead = &head)
            {
                do
                {
                    node->Next = head;  //freeList에서 가져온 새로운 노드의 헤드를 연결
                }
                //head 변수의 메모리 주소가 정상적으로 연결되어 head = node->Next가 성립하면 head의 메모리 주소를 새로 쌓인 노드로 변경
                while (Interlocked.CompareExchange(ref *(IntPtr*)pHead, (IntPtr)node, (IntPtr)node->Next) != (IntPtr)node->Next);
            }
            return true;
        }

        public bool TryPop(out T item)
        {
            UnmanagedStackNode<T>* node;            //head를 node로 가져오기 위해 + CompareExchange에서 head의 정보가 바뀔 예정이라
            UnmanagedStackNode<T>* next;            //head의 하위 노드 => 아래에 눌린 노드
            fixed (UnmanagedStackNode<T>** pHead = &head)       // 이중포인터로  head 변수의 메모리 주소 고정
            {
                do
                {
                    node = head;    //기존 최상위 스택
                    if (node == null)   //head가 비어있음 -> freeList.Cound == maxCount => Push된 데이터가 1개도 없음
                    {
                        item = default;
                        return false;
                    }
                    next = node->Next; // 헤드의 하위노드를 가져옴
                }
                //head 변수의 메모리 주소가 node랑 같으면 하위노드인 next로 교체 => node는 pop됨
                while (Interlocked.CompareExchange(ref *(IntPtr*)pHead, (IntPtr)next, (IntPtr)node) != (IntPtr)node);
            }

            item = node->Data;


            //Pop을 하면 기존 head는 비어지고 head->Next가 새로운 head가 됨(스택의 최상위)
            //이 때, freeList는 비어진 노드를 위에 쌓아야함 => node가 freeList가 되어야 함
            do
            {
                fixed (UnmanagedStackNode<T>** pFreeList = &_freeList)
                {
                    //이전 freeList는 Pop된 Node의 하위(아래에 깔린) 노드가 되어야 함
                    node->Next = _freeList;
                    //freeList가 node의 하위 노드이면 freeList는 새롭게 위에 쌓인 node로 연결되어야 함
                    if (Interlocked.CompareExchange(ref *(IntPtr*)pFreeList, (IntPtr)node, (IntPtr)node->Next) == (IntPtr)node->Next)
                    {
                        break;
                    }
                }
            } while (true);
            return true;
        }

        public void Clear()
        {
            IntPtr currentPtr;
            UnmanagedStackNode<T>* current;
            fixed (UnmanagedStackNode<T>** pHead = &head)
            {
                currentPtr = Interlocked.Exchange(ref *(IntPtr*)pHead, IntPtr.Zero);
                current = (UnmanagedStackNode<T>*)currentPtr;
                while (current != null)
                {
                    UnmanagedStackNode<T>* next = current->Next;
                    Marshal.FreeHGlobal((IntPtr)current);
                    current = next;
                }

            }
            fixed (UnmanagedStackNode<T>** pFreeList = &_freeList)
            {
                currentPtr = Interlocked.Exchange(ref *(IntPtr*)pFreeList, IntPtr.Zero);
                current = (UnmanagedStackNode<T>*)currentPtr;
                while (current != null)
                {
                    UnmanagedStackNode<T>* next = current->Next;
                    Marshal.FreeHGlobal((IntPtr)current);
                    current = next;
                }
            }

        }
        public int Count
        {
            get
            {
                int count = 0;
                UnmanagedStackNode<T>* current = head;
                while (current != null)
                {
                    count++;
                    current = current->Next;
                }
                return count;
            }
        }

        public bool Contains(T item)
        {
            UnmanagedStackNode<T>* current = head;
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            while (current != null)
            {
                if (comparer.Equals(current->Data, item))
                {
                    return true;
                }
                current = current->Next;
            }
            return false;
        }


        public T[] ToArray()
        {
            List<T> list = new List<T>();
            UnmanagedStackNode<T>* current = head;
            while (current != null)
            {
                list.Add(current->Data);
                current = current->Next;
            }
            return list.ToArray();
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("배열에 복사할 수 있는 공간이 부족합니다.");
            }

            UnmanagedStackNode<T>* current = head;
            int index = arrayIndex;
            while (current != null)
            {
                array[index++] = current->Data;
                current = current->Next;
            }
        }

        ~UnmanagedConcurrentStack()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_unmanagedMemoryPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_unmanagedMemoryPtr);
                _nodeBuffer = null;
                _freeList = null;
                head = null;
            }

        }
    }
}