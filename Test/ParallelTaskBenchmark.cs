using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Profiling;
using System.Buffers;
using System.Diagnostics;
using System.Threading.Channels;
using Debug = UnityEngine.Debug;
using UnityEditor.ShaderGraph.Internal;

public class ParallelTaskBenchmark : MonoBehaviour
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    public static extern uint GetCurrentProcessorNumber();

    public Vector3[] results_parallaxTask = new Vector3[240];
    public Vector3[] results_job = new Vector3[240];

    const int TotalCount = 240;
    const int TaskCount = 4;
    const int ChunkSize = TotalCount / TaskCount;

    void Start()
    {
        StartCoroutine(RunParallelTaskBenchmark());
        //StartCoroutine(RunJobCoroutine());
    }

    public float A(int i)
    {
        var random = new System.Random(i);
        return (float)(random.NextDouble());
    }

    IEnumerator RunParallelTaskBenchmark()
    {
        Profiler.BeginSample("ParallaxTaskBenchmark");
        Debug.Log("▶ Parallax + Task 시작");
        Stopwatch sw = Stopwatch.StartNew();

        // Channel은 병렬 작업(Task)이 생산한 데이터를 비동기적으로 소비자(Coroutine)에게 전달하기 위해 사용됨
        // ThreadSafe임
        // Reader/Writer 모델, 비동기 스트리밍 최적화
        // CreateUnbounded는 크기 제한 없음
        Channel<(int index, Vector3 vec)> channel = Channel.CreateUnbounded<(int, Vector3)>();
        Task[] taskList = new Task[TaskCount];

        for (int t = 0; t < TaskCount; t++)
        {
            int taskIndex = t;//쓰레드 병렬
            taskList[t] = Task.Run(() =>
            {
                // Parallel.For는 현재 쓰레드 풀 내의 가용 CPU 코어를 분할하여 병렬로 처리합니다.
                Parallel.For(0, ChunkSize, i =>
                {
                    int globalIndex = taskIndex * ChunkSize + i;
                    float x = A(globalIndex) * 22f - 11f;
                    float y = A(globalIndex + 1) * 22f - 11f;
                    float z = A(globalIndex + 2) * 22f - 11f;

                    channel.Writer.TryWrite((globalIndex, new Vector3(x, y, z))); // Channel에 값 쓰기

                    // 각 쓰레드의 Managed ID, 논리 CPU (OS 스케줄 기준) 로그 출력
                    var currentThreadId = Thread.CurrentThread.ManagedThreadId;
                    int processorId = -1;
                    try
                    {
                        processorId = (int)GetCurrentProcessorNumber();
                    }
                    catch { }

                    Debug.Log($"[Thread ID: {currentThreadId}] [Vec Index: {globalIndex}] [CPU ID: {processorId}]");
                });
            });
        }

        // ArrayPool: GC 힙 할당 없이 메모리 재사용을 위한 풀 (CLR에서 관리)
        // 실제 내부적으로는 대형 object pool이 있어 GC Gen0 회피 가능
        Vector3[] pooledArray = ArrayPool<Vector3>.Shared.Rent(TotalCount); // 필요 수량만큼 가져옴
        int insertIndex = 0;

        // Reader 처리를 별도 Task로 실행하여 Coroutine에서 await 피함
        Task readerTask = Task.Run(async () =>
        {
            while (await channel.Reader.WaitToReadAsync())      //채널에 읽을 값이 들어올 때까지 비동기 대기, Complete가 호출되면 false반환
            {
                while (channel.Reader.TryRead(out var pair))        //내부가 비어있으면 false
                {
                    int index = pair.index;
                    if (index < TotalCount)
                        pooledArray[index] = pair.vec;
                }
            }
        });
        yield return new WaitUntil(() => taskList.All(task => task.IsCompleted));        // 모든 Task가 완료될 때까지 대기
        channel.Writer.Complete();                      // 채널 작성을 종료
        yield return new WaitUntil(() => readerTask.IsCompleted);                       // channel의 read 종료

        Array.Copy(pooledArray, results_parallaxTask, TotalCount);
        ArrayPool<Vector3>.Shared.Return(pooledArray); // 풀에 반환하여 재사용 가능하게 함

        sw.Stop();
        Debug.Log($"? Parallax + Task 완료: {sw.ElapsedMilliseconds} ms");
        Profiler.EndSample();

        long memory = GC.GetTotalMemory(false);
        Debug.Log($"?? GC Memory 사용량: {memory / 1024f:N2} KB");
        Debug.Log("?? 코루틴 종료");
    }

    IEnumerator RunJobCoroutine()
    {
        Debug.Log("▶ Job + Burst 시작");
        NativeArray<Vector3> jobResult = new NativeArray<Vector3>(TotalCount, Allocator.TempJob);

        JobVector3Generator job = new JobVector3Generator
        {
            results = jobResult,
            seed = (int)(Time.realtimeSinceStartup * 1000)
        };

        Stopwatch sw = Stopwatch.StartNew();
        JobHandle handle = job.Schedule(TotalCount, 32); // IJobParallelFor 병렬 스케줄링

        yield return new WaitUntil(() => handle.IsCompleted == true);
        handle.Complete();
        sw.Stop();
        jobResult.CopyTo(results_job);
        jobResult.Dispose();

        Debug.Log($"? Job 완료: {sw.ElapsedMilliseconds} ms");
    }

    [BurstCompile]
    struct JobVector3Generator : IJobParallelFor
    {
        public NativeArray<Vector3> results;
        public int seed;

        public void Execute(int index)
        {
            float x = UnityEngine.Random.Range(-11f, 11f) + index;
            float y = UnityEngine.Random.Range(-11f, 11f) + index + 1;
            float z = UnityEngine.Random.Range(-11f, 11f) + index + 2;
            results[index] = new Vector3(x, y, z);
        }
    }
}
