﻿
//전처리기
#define USE_ASYNC


using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

using static Registrys;




//Mac은 보안 목적으로 FileSystem으로 byte 저장 (binary)
//Linux는 user home directory에 hidden으로 저장하되 텍스트 형식으로

//Intptr쓰는 이유 : HKEY은 실제 시스템 포인터임으로 C#에서 Intptr로 받는거이 맞음
//IntPtr은 x64, x84모두 호환됨
//C++ dll 호출할 때, 네이티브 코드로 전달해야 함으로 IntPtre사용
//IntPtr 안쓰려며 unsafe로 포인터 써야됨


//Microsoft.Win32.Registry.dll이 없을 때, 사용
//using NoneWin32;
namespace NoneWin32
{

    // c#'s RegistryValueKind  can be mapped to c++'s DWORD valueType
    public enum RegistryValueKind
    {
        //     data type REG_SZ.
        String = 1,
        //     equivalent to the Windows API registry data type REG_EXPAND_SZ.
        ExpandString = 2,
        //     data type REG_BINARY.
        Binary = 3,
        //     data type REG_DWORD.
        DWord = 4,
        //     value is equivalent to the Windows API registry data type REG_MULTI_SZ.
        MultiString = 7,
        //     data type REG_QWORD.
        QWord = 11,
        //     An unsupported registry data type. For example, the Microsoft Windows API registry
        //     data type REG_RESOURCE_LIST is unsupported. Use this value to specify that the
        //     Microsoft.Win32.RegistryKey.SetValue(System.String,System.Object) method should
        //     determine the appropriate registry data type when storing a name/value pair.
        Unknown = 0,
        //     No data type.
        None = -1
    }
}

[System.Serializable]
public class Registrys      //빌드 후, 정상적으로 작동하는 거 확인했음
{
    public enum KeyCreateMode
    {
        Skip,
        Create
    }
    public enum Base
    {
        CLASS_ROOT,
        CURRENT_CONFIG,
        CURRENT_USER,
        LOCAL_MACHINE,
        PERFORMANCE_DATA,
        USER
    }
    // Base Keys Constants
    public static IntPtr GetRootKey(Base key) => key switch
    {
        Base.CLASS_ROOT => HKEY_CLASSES_ROOT,
        Base.CURRENT_CONFIG => HKEY_CURRENT_CONFIG,
        Base.CURRENT_USER => HKEY_CURRENT_USER,
        Base.LOCAL_MACHINE => HKEY_LOCAL_MACHINE,
        Base.PERFORMANCE_DATA => HKEY_PERFORMANCE_DATA,
        Base.USER => HKEY_CURRENT_USER,
        _ => IntPtr.Zero,
    };
    public static readonly IntPtr HKEY_CLASSES_ROOT = (IntPtr)0x80000000;
    public static readonly IntPtr HKEY_CURRENT_CONFIG = (IntPtr)0x80000005;
    public static readonly IntPtr HKEY_CURRENT_USER = (IntPtr)0x80000001;
    public static readonly IntPtr HKEY_LOCAL_MACHINE = (IntPtr)0x80000002;
    public static readonly IntPtr HKEY_PERFORMANCE_DATA = (IntPtr)0x80000004;
    public static readonly IntPtr HKEY_USERS = (IntPtr)0x80000003;

    public const string SUBKEY = @"Software\McAfee";
    public const string KEYNAME = "HotKey";




    //Task Dispose 추가 필요
#if USE_ASYNC
    public async static Task<bool> RegistrySetValue<T>(string subKey, string keyName, T value, RegistryValueKind kind, KeyCreateMode mode = KeyCreateMode.Create)
    {
        UnityEngine.Debug.Log(Thread.CurrentThread.ManagedThreadId);
        var registry = OpenBaseKey(HKEY_CURRENT_USER);
        //키 확인
        var task = Task<IntPtr>.Run(() => CheckSubKeyAsync(registry, subKey));
        var taskResult = await task;
        //값 추가
        UnityEngine.Debug.Log(taskResult.ToString());
        task.Dispose();        task = null;     //메모리 해제
        return true;
    }
    public static Task<IntPtr> CheckSubKeyAsync(in IntPtr inputKey, in string subKeyName, KeyCreateMode mode = KeyCreateMode.Create)
    {
        UnityEngine.Debug.Log(Thread.CurrentThread.ManagedThreadId);
        IntPtr outkey = inputKey;
        if (inputKey == IntPtr.Zero)
            return Task.FromResult<IntPtr>(IntPtr.Zero);
        IntPtr swapKey = IntPtr.Zero;
        try
        {
            var keys = subKeyName.Split(Path.DirectorySeparatorChar);

            switch (mode)
            {
                case KeyCreateMode.Create:
                    for (int i = 0; i < keys.Length; i++)
                    {
                        //if OpenSubKey failed, swapkey is IntPtr.Zero
                        if (OpenSubKey(outkey, keys[i], out swapKey) == false)
                        {
                            //swapKey는 0가 되어버림
                            if (CreateSubKey(outkey, keys[i], out swapKey) == false) //SubKey를 만들고 swapKey에 할당
                                return Task.FromResult<IntPtr>(IntPtr.Zero);
                            Swap(ref outkey, ref swapKey);  //출력된 swapKey의 값이 outKey의 값으로
                        }
                        else
                        {
                            //하위 subKey는 swapKey에서 부터 시작함으로 Swap실행
                            Swap(ref outkey, ref swapKey);
                        }
                    }
                    return Task.FromResult<IntPtr>(outkey);
                case KeyCreateMode.Skip:
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (OpenSubKey(inputKey, keys[i], out outkey) == false)
                        {
                            return Task.FromResult<IntPtr>(IntPtr.Zero);       //키가 없으니까 반환처리
                        }
                    }
                    return Task.FromResult<IntPtr>(outkey);
                default:
                    return Task.FromResult<IntPtr>(IntPtr.Zero);
            }
        }
        catch (Exception e)
        {
            return Task.FromResult<IntPtr>(IntPtr.Zero);
        }
    }
#endif

    public static void EX_Function()
    {
        //SetRegistryValue
        List<RegistryBlock<string>> s = new List<RegistryBlock<string>>();
        List<RegistryBlock<uint>> d = new List<RegistryBlock<uint>>();
        List<RegistryBlock<ulong>> q = new List<RegistryBlock<ulong>>();
        List<RegistryBlock<byte[]>> b = new List<RegistryBlock<byte[]>>();

        s.Add(new RegistryBlock<string>(RegistryValueKind.String, "Test S 1", "tt"));
        s.Add(new RegistryBlock<string>(RegistryValueKind.ExpandString, "Test ES 1", "GIGAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"));
        d.Add(new RegistryBlock<uint>(RegistryValueKind.DWord, KEYNAME, 0X31));
        d.Add(new RegistryBlock<uint>(RegistryValueKind.DWord, KEYNAME + "  2", 0X81));
        q.Add(new RegistryBlock<ulong>(RegistryValueKind.QWord, "Test Q 1", 8451281523187));
        q.Add(new RegistryBlock<ulong>(RegistryValueKind.QWord, "Test Q 2", 78764443425234235));
        q.Add(new RegistryBlock<ulong>(RegistryValueKind.QWord, "Test Q 3", 675756864653543));
        q.Add(new RegistryBlock<ulong>(RegistryValueKind.QWord, "Test Q 4", 546352432));

        Action dispose = () =>
        {
            s.Clear(); s = null;
            d.Clear(); d = null;
            q.Clear(); q = null;
            b.Clear(); b = null;
        };

        using (UnionInput union = new UnionInput(s, d, q, b, dispose))
        {
            RegistrySetValue(Base.CURRENT_USER,SUBKEY, in union, KeyCreateMode.Create);
            if (union.Skipped.Count > 0)
                foreach (var data in union.Skipped)
                    UnityEngine.Debug.Log("Error Skipped   :   " + data);
            else
                UnityEngine.Debug.Log("All datas added successfully");
        }

        //GetRegistryValue
        UnionOuput unionOuput = Registrys.GetRegistryValue(Registrys.SUBKEY, new string[]
        {
                "Test Q 4", "HotKey  2", "Test Q 3",  "Test Q", "Test ES 1", "Test S 1",
                 "Test Q 1", "HotKey", "Test Q 2"
        });

        if (unionOuput == null)
            UnityEngine.Debug.Log("Null");
        else
        {
            foreach (var data in unionOuput.s)
                UnityEngine.Debug.Log(data.name + " : " + data.value);
            foreach (var data in unionOuput.d)
                UnityEngine.Debug.Log(data.name + " : " + data.value);
            foreach (var data in unionOuput.q)
                UnityEngine.Debug.Log(data.name + " : " + data.value);
            foreach (var data in unionOuput.b)
                UnityEngine.Debug.Log(data.name + " : " + data.value);

            if(unionOuput.s.Count > 0)
                foreach (var data in unionOuput.Skipped)
                    UnityEngine.Debug.Log("Error Skipped   :   " + data);
            else
                UnityEngine.Debug.Log("All datas get successfully");
        }
    }

    public static void Test2()
    {
        string subkey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
        using (UnionInput unionInput = new UnionInput(null, new List<RegistryBlock<uint>> { new RegistryBlock<uint>(RegistryValueKind.DWord, "ConsentPromptBehaviorAdmin", 0), new RegistryBlock<uint>(RegistryValueKind.DWord, "EnableLUA", 0) }))
        {
            if(RegistrySetValue(Base.LOCAL_MACHINE, in subkey, unionInput) == false)
            {
                UnityEngine.Debug.Log("Failed");
            }
        }

    }

    [System.Serializable]
    public struct RegistryBlock<T>   //불변 처리
    {
        public readonly RegistryValueKind kind;
        public readonly string name;
        public readonly T value;
        public RegistryBlock(RegistryValueKind kind, string name, T value)
        {
            this.kind = kind;
            this.name = name;
            this.value = value;
        }
        public RegistryBlock(RegistryValueKind kind, ref string name, ref T value)
        {
            this.kind = kind;
            this.name = name;
            this.value = value;
        }
        public RegistryBlock(ref RegistryValueKind kind, ref string name, ref T value)   //값 복사 회피
        {
            this.kind = kind;
            this.name = name;
            this.value = value;
        }
    }

    //async가 아니면 ref, in, out 사용가능

    [System.Serializable]
    public class UnionInput : IDisposable
    {
        // s = new List<Test<string>>();  // 컴파일 에러
        public IReadOnlyList<RegistryBlock<string>> s { get; private set; }
        public IReadOnlyList<RegistryBlock<uint>> d { get; private set; }
        public IReadOnlyList<RegistryBlock<ulong>> q { get; private set; }
        public IReadOnlyList<RegistryBlock<byte[]>> b { get; private set; }
        public ThreadSafeList<string> Skipped { get; private set; }
        public Action dispose { get; private set; }

        public UnionInput(List<RegistryBlock<string>> s = null, List<RegistryBlock<uint>> d = null, 
            List<RegistryBlock<ulong>> q = null, List<RegistryBlock<byte[]>> b = null, Action dispose = null)
        {
            //AsReadOnly는 값 복사, 박싱이 없음
            this.s = s?.AsReadOnly();    
            this.d = d?.AsReadOnly();
            this.q = q?.AsReadOnly();
            this.b = b?.AsReadOnly();
            Skipped = new ThreadSafeList<string>();
            this.dispose = dispose != null ? dispose : null;
        }

        public void Dispose()
        {
            dispose?.Invoke();
            s = null; d = null; q = null; b = null;
            dispose = null;
            Skipped.Clear();
            Skipped = null;
        }
    }


    //Task 쓸꺼면 값 복사로 병렬작업을 하던가 lock
    private static void Swap(ref IntPtr a, ref IntPtr b) => (b, a) = (a, b);
    //문제없음 
    public static bool CheckSubKey(in IntPtr inputKey, in string subKeyName, out IntPtr outkey, KeyCreateMode mode = KeyCreateMode.Create)
    {
        outkey = inputKey;
        if (inputKey == IntPtr.Zero)      
            return false;     

        IntPtr swapKey = IntPtr.Zero;
        try
        {
            var keys = subKeyName.Split(Path.DirectorySeparatorChar);

            if (keys.Length != 0) switch (mode)
                {
                    case KeyCreateMode.Create:
                        for (int i = 0; i < keys.Length; i++)
                        {
                            //if OpenSubKey failed, swapkey is IntPtr.Zero
                            if (OpenSubKey(outkey, keys[i], out swapKey) == false)
                            {
                                UnityEngine.Debug.Log(keys[i] + "is not exists");
                                //swapKey는 0가 되어버림
                                CreateSubKey(outkey, keys[i], out swapKey); //SubKey를 만들고 swapKey에 할당
                                Swap(ref outkey, ref swapKey);  //출력된 swapKey의 값이 outKey의 값으로
                            }
                            else
                            {
                                //하위 subKey는 swapKey에서 부터 시작함으로 Swap실행
                                Swap(ref outkey, ref swapKey);
                            }
                        }
                        return true;
                    case KeyCreateMode.Skip:
                        for (int i = 0; i < keys.Length; i++)
                        {
                            if (OpenSubKey(inputKey, keys[i], out outkey) == false)
                                return false;       //키가 없으니까 반환처리
                        }
                        return true;
                    default:
                        return false;
                }
            else
                return false;
        }
        catch (Exception e)
        {
            return false;
        }
    }



    //타입별 List<T> 방식이 타당한 이유
    //class 통째로 넘기면 참조가 맞긴함
    //어짜피 T는 정해져있음
    //params object[] 쓰면 값 타입은 boxing/unboxing됨
    //int의 boxing을 피하려고 nullable로 int?를 써도 boxing/unboxing발생
    //struct로 넘기려 해도 한에 여러 타입의 값을 넣을 수도 없고, struct를 object로 넘기면 boxing/unboxing 발생
    //Generic struct인 ValueWrapper로 값타입을 넘기려해도 generic이라 obcject로 넘기는 과정에서 boxing/unboxing함
    // ----- Type Check도 해야됨
    // Type이랑 object 가진 ReferenceWrapper로 넘기면 참조 타입이라 boxing/unboxing은 없지만, class생성되는 양이 너무 많음 +
    // Type이랑 object를 내부에서 선언해서 사용하기 때문에 값타입이 object가 될 때, boxing/unboxing
    //Generic class ReferenceWrapper<T>로 넘기고 class 생성시 ref를 잘 사용하면  params object[]를 이용할 때,
    //   --------       그나마 boxing/unboxing을 피하면서 작동 가능. 근데 class 생성량 너무 많고 타입체크 부분 고려해야됨
    //Generic struct ValueWrapperandGeneric class ReferenceWrapper with params object[]
    // -----            유연성은 가장 좋은데 느리고 코드 복잡성 너무 높은 + 유지보수 어려움

    // IDisposable => 이거 class에만 되는데다 await해서 잡고 있으면 똑같고 lock 문제 있음

    //SetRegistryValue을 최대한 여러번 호출해서 CheckSubKey부분 접근을 줄여야함 + Task로 전환 시, ref in out 못씀
    //RegistrySetValueTest<T>가 아니고 RegistrySetValueTest로 접근하는게 맞음
    public static bool RegistrySetValue(Base rootKey, in string subKey, in UnionInput value, KeyCreateMode mode = KeyCreateMode.Create)
    {
#if UNITY_STANDALONE_WIN
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
            return false;
        var registry = OpenBaseKey(GetRootKey(rootKey));
        UnityEngine.Debug.Log(registry.ToString());
        if (CheckSubKey(in registry, in subKey, out IntPtr outkey, mode) == false || outkey == IntPtr.Zero)
        {
            CloseKeys(registry, outkey);
            UnityEngine.Debug.Log("Subkey Error");
            return false;
        }

        //다중 호출 필요, kind, keyName, value 묶어서 한번에 전달
        if(value.s != null)
        {
            for (int i = 0; i < value.s.Count; i++)
            {
                //다음 루프에서 스택에 의해 해제(GC아님)
                var data = value.s[i]; //string은 참조 복사
                if (SetRegistryValue<string>(in outkey, in data) == false)
                    value.Skipped.Add(data.name);
            }
        }

        if (value.d != null)
        {
            for (int i = 0; i < value.d.Count ; i++)
            {
                var data = value.d[i];
                if (SetRegistryValue<uint>(in outkey, in data) == false)
                    value.Skipped.Add(data.name);
            }
        }

        if (value.q != null)
        {
            for (int i = 0; i < value.q.Count; i++)
            {
                var data = value.q[i];
                if (SetRegistryValue<ulong>(in outkey, in data) == false)
                    value.Skipped.Add(data.name);
            }
        }

        if (value.b != null)
        {
            for (int i = 0; i < value.b.Count; i++)
            {
                var data = value.b[i];
                if (SetRegistryValue<byte[]>(in outkey, in data) == false)
                    value.Skipped.Add(data.name);
            }
        }
        CloseKeys(registry, outkey);
        return true;
#else
        //File Stream으로
        return false;
#endif
    }
    //이미 여기서 타입 체크를 함 + T인자가 Type값으로 정해질 수가 없음
    public static bool SetRegistryValue<T>(in IntPtr inputKey, in RegistryBlock<T> block)
    {
        try
        {
            if (block.value == null || block.value is not T) //Null Check, Type Check
                return false;
            else switch (block.kind)
                {
                    //안정성을 위해 2중확인
                    case RegistryValueKind.String:      //as 참조형식
                    case RegistryValueKind.ExpandString:
                        if (typeof(T) != typeof(string) || block.value is not string)
                            return false;
                        var _s = block.value as string;   // 참조형식이라 _v는 4(x86) or 8(x64) 바이트
                        return SetRegistryValue(inputKey, block.name, _s, (uint)block.kind);
                    case RegistryValueKind.DWord:       //값 형식이라 Nullable 사용
                        {
                            UnityEngine.Debug.Log("Dword");
                            //uint로 
                            if (typeof(T) != typeof(uint))
                                return false;
                            var uintValue = block.value as uint?;
                            if (uintValue.HasValue == false)
                                return false;
                            return SetRegistryValue(inputKey, block.name, uintValue.Value);
                        }
                    case RegistryValueKind.QWord:
                        {
                            //18446744073709551615 ulong 최대값
                            if (typeof(T) != typeof(ulong))
                                return false;
                            var ulongValue = block.value as ulong?;
                            if (ulongValue.HasValue == false)
                                return false;
                            return SetRegistryValue(inputKey, block.name, ulongValue.Value);
                        }
                    case RegistryValueKind.Binary:      //as 참조형식
                        {
                            //new byte[3] { 0x31, 0x32, 0x33 }
                            if (typeof(T) != typeof(byte[]) || block.value is not byte[])
                                return false;
                            var _b = block.value as byte[];
                            //_b.Length는 SetRegistryValue에서 처리
                            //_b이 null이거나 길이가 0인 경우 false반환, 1 이상인 경우 true반환
                            // && 연산자 : 앞의 condition이 true면 && 뒤를 수행
                            return (_b?.Length > 0 == true) && SetRegistryValue(inputKey, block.name, _b);
                        }

                    case RegistryValueKind.None:
                    default:
                        return false;
                }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogException(e);
            return false;
        }
    }


    /*  ThreadSafeList로 대체됨
    public class UnionOuput : IDisposable
    {
        private object s_lock = new object();
        private object d_lock = new object();
        private object q_lock = new object();
        private object b_lock = new object();

        public List<RegistryBlock<string>> s { get; private set; }
        public List<RegistryBlock<uint>> d { get; private set; }
        public List<RegistryBlock<ulong>> q { get; private set; }
        public List<RegistryBlock<byte[]>> b { get; private set; }
    }
    */
    [System.Serializable]
    public class UnionOuput : IDisposable       //여기는 다중접근 해도 됨
    {
        public UnionOuput()
        {
            s = new ThreadSafeList<RegistryBlock<string>>();
            d = new ThreadSafeList<RegistryBlock<uint>>();
            q = new ThreadSafeList<RegistryBlock<ulong>>();
            b = new ThreadSafeList<RegistryBlock<byte[]>>();

            Skipped = new ThreadSafeList<string>();
        }

        //Async 때문에 구현됨
        public ThreadSafeList<RegistryBlock<string>> s { get; private set; }
        public ThreadSafeList<RegistryBlock<uint>> d { get; private set; }
        public ThreadSafeList<RegistryBlock<ulong>> q { get; private set; }
        public ThreadSafeList<RegistryBlock<byte[]>> b { get; private set; }

        public ThreadSafeList<string> Skipped { get; private set; }

        public void Dispose()
        {
            s.Dispose(); s = null;
            d.Dispose(); d = null;
            q.Dispose(); q = null;
            b.Dispose(); b = null;
            Skipped.Dispose(); Skipped = null;
        }
    }


    public static UnionOuput GetRegistryValue(in string subKey, string[] names, UnionOuput unionOuput = null)
    {
#if UNITY_STANDALONE_WIN
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
            return null;
        if (names.Length == 0 || names == null)
            return null;
        var registry = OpenBaseKey(HKEY_CURRENT_USER);

        if (CheckSubKey(in registry, in subKey, out IntPtr outkey) == false || outkey == IntPtr.Zero)
        {
            CloseKeys(registry, outkey);
            return null;
        }

        unionOuput ??= new UnionOuput();
        for (int i = 0; i < names.Length; i++)
        {
            if (GetRegistryValue(in outkey, names[i], ref unionOuput) == false)
            {
                unionOuput.Skipped.Add(names[i]);       //false가 return되면 Skip처리 된 name을 저장
            }
        }
        CloseKeys(registry, outkey);
        return unionOuput;
#else
        return null;
#endif
    }


    //c++코드랑 교차해서 확인 해야됨        //GetValue가 false면 값이 존재하지 않음s
    public static bool GetRegistryValue(in IntPtr inputKey, string name, ref UnionOuput output)    //비동기 처리 가능
    { 
        try
        {
            uint type = 0;
            uint valueSize = 1024;
            RegistryValueKind kind = RegistryValueKind.None;
            IntPtr value = Marshal.AllocHGlobal((int)valueSize);
            //float값이 저장된 경우 string을 Span으로 바꾸고 맨 뒷 부분에 f를 제거하고 Float.TryParse
            if (GetValue(inputKey, name, ref type, value, ref valueSize) == true) switch (kind = (RegistryValueKind)type)
                {
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString:
                        if (value == IntPtr.Zero)
                            return false;
                        var es_val = Marshal.PtrToStringAnsi(value);
                        output.s.Add(new RegistryBlock<string>(kind, name, es_val));
                        return true;
                    case RegistryValueKind.DWord:
                        if (value == IntPtr.Zero)
                            return false;
                        var d_val = (uint)Marshal.ReadInt32(value);
                        output.d.Add(new RegistryBlock<uint>(kind, name, d_val));
                        return true;
                    case RegistryValueKind.QWord:
                        if(value == IntPtr.Zero)
                            return false;
                        var q_val = (ulong)Marshal.ReadInt64(value);
                        output.q.Add(new RegistryBlock<ulong>(kind, name, q_val));
                        return true;
                    case RegistryValueKind.Binary:
                        if (value == IntPtr.Zero)
                            return false;
                        byte[] data = new byte[valueSize];
                        Marshal.Copy(value, data, 0, data.Length);
                        output.b.Add(new RegistryBlock<byte[]>(kind, name, data));
                        return true;
                    case RegistryValueKind.Unknown:
                        return false;
                    default:
                        return false;
                }
            else
            {
                UnityEngine.Debug.Log("GetValue Error");
                return false;
            }
        }
        catch (Exception e)
        {
            RegistryValueKind kind = RegistryValueKind.None;
            UnityEngine.Debug.Log(e);
            return false;
        }
    }

    public static bool DeleteSubkey(in string subKey)
    {
#if UNITY_STANDALONE_WIN
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
            return false;
        var registry = OpenBaseKey(HKEY_CURRENT_USER);
        var success = DeleteSubKey(registry, subKey);
        CloseKeys(registry);
        return success;
#else
        return false;
#endif
    }
    public static bool DeleteKey(in string subKey, in string valueName)
    {
#if UNITY_STANDALONE_WIN
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
            return false;
        var registry = OpenBaseKey(HKEY_CURRENT_USER);
        if (CheckSubKey(in registry, in subKey, out IntPtr outkey) == false || outkey == IntPtr.Zero)
        {
            CloseKeys(registry, outkey);
            return false;
        }
        var success = DeleteValue(outkey, valueName);
        CloseKeys(registry, outkey);
        return success;
#else
        return false;
#endif
    }

    private static bool CloseKeys(params IntPtr[] keys) //열린 레지스트리 키 핸들을 닫음  ***무조건 호출
    {
        try
        {
            for (int i = 0; i < keys.Length; i++)
            {
                CloseKey(keys[i]);
            }
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    //파일로 출력하는거 필요
    public static bool SetRegistryValue<T>(RegistryValueKind kind, in string name, in T value)
    {
#if UNITY_STANDALONE_WIN
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            try
            {
                if (value == null || value is not T) //Null Check, Type Check
                    return false;

                var registry = OpenBaseKey(HKEY_CURRENT_USER);
                if (OpenSubKey(registry, SUBKEY, out IntPtr outkey) == true) switch (kind)
                    {
                        //안정성을 위해 2중확인
                        case RegistryValueKind.String:      //as 참조형식
                        case RegistryValueKind.ExpandString:
                            if (typeof(T) != typeof(string) || value is not string)
                                return false;
                            var _s = value as string;   // 참조형식이라 _v는 4(x86) or 8(x64) 바이트
                            return SetRegistryValue(outkey, name, _s, (uint)kind);
                        case RegistryValueKind.DWord:       //값 형식이라 Nullable 사용
                            {
                                //uint로 
                                if (typeof(T) != typeof(uint))
                                    return false;
                                var uintValue = value as uint?;
                                if (uintValue.HasValue == false)
                                    return false;
                                return SetRegistryValue(outkey, name, uintValue.Value);
                            }
                        case RegistryValueKind.QWord:
                            {
                                //18446744073709551615 ulong 최대값
                                if (typeof(T) != typeof(ulong))
                                    return false;
                                var ulongValue = value as ulong?;
                                if (ulongValue.HasValue == false)
                                    return false;
                                return SetRegistryValue(outkey, name, ulongValue.Value);
                            }
                        case RegistryValueKind.Binary:      //as 참조형식
                            {
                                //new byte[3] { 0x31, 0x32, 0x33 }
                                if (typeof(T) != typeof(byte[]) || value is not byte[])
                                    return false;
                                var _b = value as byte[];
                                //_b.Length는 SetRegistryValue에서 처리
                                //_b이 null이거나 길이가 0인 경우 false반환, 1 이상인 경우 true반환
                                // && 연산자 : 앞의 condition이 true면 && 뒤를 수행
                                return (_b?.Length > 0 == true) && SetRegistryValue(outkey, name, _b);
                            }

                        case RegistryValueKind.None:
                            return false;
                        default:
                            return false;
                    }
                return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
                return false;
            }
        }
        else
            return false;

#else
        return false;
#endif
    }

    //************************** DLL Imports ******************************//

    //Get BaseKey
    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr OpenBaseKey(IntPtr baseKey);


    //Modify SubKey
    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool OpenSubKey(IntPtr baseKey, string subKey, out IntPtr outKey);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool CreateSubKey(IntPtr baseKey, string subKey, out IntPtr outKey);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DeleteSubKey(IntPtr baseKey, string subKey);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DeleteSubKeyTree(IntPtr baseKey, string subKey);


    //Modify Key
    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool GetValue(IntPtr key, string valueName, ref uint valueType, IntPtr value, ref uint valueSize);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool SetValue(IntPtr key, string valueName, IntPtr value, uint valueType, uint valueSize = 0);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DeleteValue(IntPtr key, string valueName);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void CloseKey(IntPtr key);


    // Set a string value in the registry (REG_SZ, REG_EXPAND_SZ)
    public static bool SetRegistryValue(IntPtr key, string valueName, string value, uint valueType)
    {
        if (valueType == 1 /* REG_SZ */ || valueType == 2 /* REG_EXPAND_SZ */)
        {
            IntPtr valuePtr = Marshal.StringToHGlobalAnsi(value);
            bool result = SetValue(key, valueName, valuePtr, valueType);
            Marshal.FreeHGlobal(valuePtr); // Clean up
            return result;
        }
        else
        {
            throw new InvalidOperationException("Unsupported value type for string.");
        }
    }

    // Set an integer value in the registry (REG_DWORD)
    public static bool SetRegistryValue(IntPtr key, string valueName, uint value)
    {
        IntPtr valuePtr = Marshal.AllocHGlobal(sizeof(uint));
        Marshal.StructureToPtr(value, valuePtr, false);
        bool result = SetValue(key, valueName, valuePtr, 4 /* REG_DWORD */);
        Marshal.FreeHGlobal(valuePtr); // Clean up
        return result;
    }

    // Set a long value in the registry (REG_QWORD)
    public static bool SetRegistryValue(IntPtr key, string valueName, ulong value)
    {
        IntPtr valuePtr = Marshal.AllocHGlobal(sizeof(ulong));
        Marshal.StructureToPtr(value, valuePtr, false);
        bool result = SetValue(key, valueName, valuePtr, 11 /* REG_QWORD */);
        Marshal.FreeHGlobal(valuePtr); // Clean up
        return result;
    }

    // Set binary data in the registry (REG_BINARY)
    public static bool SetRegistryValue(IntPtr key, string valueName, byte[] value)
    {
        IntPtr valuePtr = Marshal.AllocHGlobal(value.Length);
        Marshal.Copy(value, 0, valuePtr, value.Length);
        bool result = SetValue(key, valueName, valuePtr, 3 /* REG_BINARY */, (uint)value.Length);
        Marshal.FreeHGlobal(valuePtr); // Clean up
        return result;
    }
}



#if OLD_VERSION

    public static bool RegistrySetValue<T>(in string subKey, in string keyName, in T value, RegistryValueKind kind, KeyCreateMode mode = KeyCreateMode.Create)
    {
#if UNITY_STANDALONE_WIN
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
            return false;

        var registry = OpenBaseKey(HKEY_CURRENT_USER);
        if (CheckSubKey(in registry, in subKey, out IntPtr outkey, mode) == false || outkey == IntPtr.Zero)
            return false;


        if (SetRegistryValue<T>(in outkey, kind, keyName, value) == false)
            return false;

        return true;
#else
        //File Stream으로
        return false;
#endif
    }


    //이미 여기서 타입 체크를 함 + T인자가 Type값으로 정해질 수가 없음
    public static bool SetRegistryValue<T>(in IntPtr inputKey, RegistryValueKind kind, in string name, in T value)
    {
        try
        {
            if (value == null || value is not T) //Null Check, Type Check
                return false;
            else switch (kind)
                {
                    //안정성을 위해 2중확인
                    case RegistryValueKind.String:      //as 참조형식
                    case RegistryValueKind.ExpandString:
                        if (typeof(T) != typeof(string) || value is not string)
                            return false;
                        var _s = value as string;   // 참조형식이라 _v는 4(x86) or 8(x64) 바이트
                        return SetRegistryValue(inputKey, name, _s, (uint)kind);
                    case RegistryValueKind.DWord:       //값 형식이라 Nullable 사용
                        {
                            //uint로 
                            if (typeof(T) != typeof(uint))
                                return false;
                            var uintValue = value as uint?;
                            if (uintValue.HasValue == false)
                                return false;
                            return SetRegistryValue(inputKey, name, uintValue.Value);
                        }
                    case RegistryValueKind.QWord:
                        {
                            //18446744073709551615 ulong 최대값
                            if (typeof(T) != typeof(ulong))
                                return false;
                            var ulongValue = value as ulong?;
                            if (ulongValue.HasValue == false)
                                return false;
                            return SetRegistryValue(inputKey, name, ulongValue.Value);
                        }
                    case RegistryValueKind.Binary:      //as 참조형식
                        {
                            //new byte[3] { 0x31, 0x32, 0x33 }
                            if (typeof(T) != typeof(byte[]) || value is not byte[])
                                return false;
                            var _b = value as byte[];
                            //_b.Length는 SetRegistryValue에서 처리
                            //_b이 null이거나 길이가 0인 경우 false반환, 1 이상인 경우 true반환
                            // && 연산자 : 앞의 condition이 true면 && 뒤를 수행
                            return (_b?.Length > 0 == true) && SetRegistryValue(inputKey, name, _b);
                        }

                    case RegistryValueKind.None:
                    default:
                        return false;
                }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return false;
        }
    }

    //Task로 전환
    public static bool SetRegistryValue<T>(RegistryValueKind kind, in string name, in T value)
    {
#if UNITY_STANDALONE_WIN
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            try
            {
                if(value == null || value is not T) //Null Check, Type Check
                    return false;

                var registry = OpenBaseKey(HKEY_CURRENT_USER);
                if (OpenSubKey(registry, SUBKEY, out IntPtr outkey) == true) switch ( kind )
                    {
                        //안정성을 위해 2중확인
                        case RegistryValueKind.String:      //as 참조형식
                        case RegistryValueKind.ExpandString:
                            if (typeof(T) != typeof(string) || value is not string)
                                return false;
                            var _s = value as string;   // 참조형식이라 _v는 4(x86) or 8(x64) 바이트
                            return SetRegistryValue(outkey, name, _s, (uint)kind);
                        case RegistryValueKind.DWord:       //값 형식이라 Nullable 사용
                            {
                                //uint로 
                                if (typeof(T) != typeof(uint))
                                    return false;
                                var uintValue = value as uint?;
                                if (uintValue.HasValue == false)
                                    return false;
                                return SetRegistryValue(outkey, name, uintValue.Value);
                            }
                        case RegistryValueKind.QWord :
                            {
                                //18446744073709551615 ulong 최대값
                                if (typeof(T) != typeof(ulong))
                                    return false;
                                var ulongValue = value as ulong?;
                                if (ulongValue.HasValue == false)
                                    return false;
                                return SetRegistryValue(outkey, name, ulongValue.Value);
                            }
                        case RegistryValueKind.Binary:      //as 참조형식
                            {
                                //new byte[3] { 0x31, 0x32, 0x33 }
                                if (typeof(T) != typeof(byte[]) || value is not byte[])
                                    return false;
                                var _b = value as byte[];
                                //_b.Length는 SetRegistryValue에서 처리
                                //_b이 null이거나 길이가 0인 경우 false반환, 1 이상인 경우 true반환
                                // && 연산자 : 앞의 condition이 true면 && 뒤를 수행
                                return (_b?.Length > 0 == true) && SetRegistryValue(outkey, name, _b);  
                            }

                        case RegistryValueKind.None: 
                            return false;
                        default: 
                            return false;
                    }
                else        //OpenSubKey을 했으니 SubKey가 존재하지 않을 때
                {

                }
                return false; 
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return false;
            }
        }
        else
            return false;

#else
        return false;
#endif
    }
#endif
