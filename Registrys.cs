using System;
using System.Collections;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Security.Principal;
using System.Buffers;
using System.Runtime.InteropServices;


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
public class Registrys
{
    public enum KeyCreateMode
    {
        Skip,
        Create
    }

    // Base Keys Constants
    public static readonly IntPtr HKEY_CLASSES_ROOT = (IntPtr)0x80000000;
    public static readonly IntPtr HKEY_CURRENT_CONFIG = (IntPtr)0x80000005;
    public static readonly IntPtr HKEY_CURRENT_USER = (IntPtr)0x80000001;
    public static readonly IntPtr HKEY_LOCAL_MACHINE = (IntPtr)0x80000002;
    public static readonly IntPtr HKEY_PERFORMANCE_DATA = (IntPtr)0x80000004;
    public static readonly IntPtr HKEY_USERS = (IntPtr)0x80000003;

    private const string SUBKEY = @"Software\McAfee";
    private const string KEYNAME = "HotKey";


    public static string GetOpenKey( string subkey)
    {
        string log = "";
        var registry = OpenBaseKey(HKEY_CURRENT_USER);

        log += registry.ToString() + "\n";
        var keys = subkey.Split(Path.DirectorySeparatorChar);
        IntPtr outkey = IntPtr.Zero;
        if (OpenSubKey(registry, keys[0], out outkey) == true)
        {
            log += outkey.ToString() + "\n";
            IntPtr outkey2;
            //값의 반환이 발생함으로, keys[1]이 존재하지 않는 경우  out outkey하면 outkey이 0이 되어버림
            if (OpenSubKey(outkey, keys[1], out outkey2) == true)   
            {
                CreateSubKey(outkey2, keys[1], out outkey2);
                log += outkey.ToString() + "\n";
            }
            else
            {
                CreateSubKey(outkey, keys[1], out outkey2);
                log += keys[1]+ " is now exists " + "\n" + outkey2;
            }
        }
        else
        {
            log += keys[0]+ " is now exists";
        }

        return log;
    }

    private static void Swap(ref IntPtr a, ref IntPtr b) => (b, a) = (a, b);
    public static bool CheckSubKey(in IntPtr inputKey, in string subKeyName, out IntPtr outkey, KeyCreateMode mode = KeyCreateMode.Create)
    {
        outkey = IntPtr.Zero;
        if (inputKey == IntPtr.Zero)
            return false;
        try
        {
            var keys = subKeyName.Split(Path.DirectorySeparatorChar);

            switch (mode)
            {
                case KeyCreateMode.Create:
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (OpenSubKey(inputKey, keys[0], out outkey) == true)
                        {

                        }
                        else
                        {

                        }
                    }
                    return true;
                case KeyCreateMode.Skip:
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (OpenSubKey(inputKey, keys[0], out outkey) == false)
                        {
                            return false;       //키가 없으니까 반환처리
                        }
                    }
                    return true;
                default:
                    return false;
            }
        }
        catch 
        { 
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




    public static bool GetRegistryKey()
    {
        var registry = OpenBaseKey(HKEY_CURRENT_USER);
        uint type = 0;
        uint valueSize = 0;
        if (!GetValue(registry, "a", ref type, IntPtr.Zero, ref valueSize))
        {
            return false;
        }
            return false;
    }

    public static bool DeleteKey()
    {
        return false;
    }
    public static bool TestDebug()
    {
        string testkey = @"Software\McAfee3";
        var registry = OpenBaseKey(HKEY_CURRENT_USER);
        if (OpenSubKey(registry, testkey, out IntPtr outkey) == true)
        {
            return true;
        }
        else
        {
            return false;
        }
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
