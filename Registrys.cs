using System;
using System.Collections;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.IO;
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
    // Base Keys Constants
    public static readonly IntPtr HKEY_CLASSES_ROOT = (IntPtr)0x80000000;
    public static readonly IntPtr HKEY_CURRENT_CONFIG = (IntPtr)0x80000005;
    public static readonly IntPtr HKEY_CURRENT_USER = (IntPtr)0x80000001;
    public static readonly IntPtr HKEY_LOCAL_MACHINE = (IntPtr)0x80000002;
    public static readonly IntPtr HKEY_PERFORMANCE_DATA = (IntPtr)0x80000004;
    public static readonly IntPtr HKEY_USERS = (IntPtr)0x80000003;

    private const string SUBKEY = @"Software\McAfee";
    private const string KEYNAME = "HotKey";
    public static bool CreateSubKey(byte[] value)
    {
#if UNITY_STANDALONE_WIN
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(SUBKEY);
                if (key == null)
                {
                    key = Registry.CurrentUser.CreateSubKey(SUBKEY);
                }
                else
                {
                    key.SetValue(KEYNAME, value, RegistryValueKind.Binary);
                }

                return true;
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




    // DLL Imports
    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr OpenBaseKey(IntPtr baseKey);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool OpenSubKey(IntPtr baseKey, string subKey, out IntPtr outKey);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool CreateSubKey(IntPtr baseKey, string subKey, out IntPtr outKey);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DeleteSubKey(IntPtr baseKey, string subKey);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DeleteSubKeyTree(IntPtr baseKey, string subKey);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool GetValue(IntPtr key, string valueName, out uint valueType, byte[] outValue, uint bufferSize);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool SetValue(IntPtr key, string valueName, string value, uint valueType);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DeleteValue(IntPtr key, string valueName);

    [DllImport("RegistryKey.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void CloseKey(IntPtr key);

    public static bool SetRegistryValue(IntPtr key, string valueName, object value, RegistryValueKind valueKind)
    {
        switch (valueKind)
        {
            case RegistryValueKind.String:
            case RegistryValueKind.ExpandString:
                return SetValue(key, valueName, value.ToString(), (uint)valueKind);

            case RegistryValueKind.DWord:
                return SetValue(key, valueName, value.ToString(), (uint)valueKind);

            case RegistryValueKind.Binary:
                if (value is byte[] binaryData)
                {
                    string hexString = BitConverter.ToString(binaryData).Replace("-", "").ToLower();
                    return SetValue(key, valueName, hexString, (uint)valueKind);
                }
                break;
        }

        return false; // Unsupported type or invalid input
    }

    public static bool Test()
    {
        var aaa = OpenBaseKey(HKEY_CURRENT_USER);
        IntPtr outkey;
        if(OpenSubKey(aaa, SUBKEY, out outkey))
        {
            bool success = SetRegistryValue(outkey, "Test", 1225, RegistryValueKind.DWord);
        }
        return false;
    }

    public static bool ReadSubKey()
    {
        try
        {
            return true;
        }
        catch
        {
            return false;
        }
    }
}
