using System.Collections;
using System;
using System.Runtime.InteropServices;

//STYLE에 관하여
// OVERLAPPED = CAPTION | SYSMUNU | THICKFRAME | MINIMIZEBOX | MAXIMIZEBOX
// POPUPWINDOW = POPUP | BORDER | SYSMENU


using System.Drawing;
using System.Windows;
using System.Numerics;

#if UNITY_2021_3_OR_NEWER
using UnityEngine;
public class UnityFunction
{
    //OpenCV는 BGR
    //C#은 ARGB 
    public static uint ColorToUInt(UnityEngine.Color32 color)
    {
        return (uint)((color.a << 24) | (color.r << 16) | (color.g << 8) | (color.b));
    }
    public static UnityEngine.Color32 RandomColor()
    {
        return UnityEngine.Random.ColorHSV();
    }
}

#endif

public static class WindowHandle
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetActiveWindow();
    [DllImport("User32.dll")]
    public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public static IntPtr now_hWnd;


#if UNITY_2017_1_OR_NEWER
    [RuntimeInitializeOnLoadMethod]
    public static void RuntimeInitializeOnLoad()
    {
        now_hWnd = GetForegroundWindow();
    }
#else
    static WindowHandle()
    {
        now_hWnd = GetForegroundWindow();
    }
#endif
}


public class Window
{

    public static readonly int GWL_STYLE = -16;
    public static readonly int GWL_EXSTYLE = -20;


    //STYLE
    public static readonly uint WS_BORDER = 0x00800000;
    public static readonly uint WS_DLGFRAME = 0x00400000;
    public static readonly uint WS_SIZEBOX = 0x00040000;
    public static readonly uint WS_VISIBLE = 0x10000000;           //바로 화면출력
    public static readonly uint WS_MINIMIZE = 0x20000000;          //최초 최소화
    public static readonly uint WS_MAXIMIZE = 0x01000000;          //최초 최대화
                                                                   //EXSTYLE
    public static readonly uint WS_EX_TRANSPARENT = 0x00000020;    // 클릭 관통
    public static readonly uint WS_EX_LAYERED = 0x00080000;        // 투명
    public static readonly uint WS_EX_TOPMOST = 0x00000008;        // 최상위 윈도우


    //SWP(SetWindowPos) FLAGS
    public static readonly uint SWP_NOMOVE = 0x00000002;
    public static readonly uint SWP_NOSIZE = 0x00000001;
    public static readonly uint SWP_NOOWNERZORDER = 0x00000200;

    //LWA
    public static readonly uint LWA_ALPHA = 0x00000002;
    public static readonly uint LWA_COLORKEY = 0x00000001;

    //
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    public static readonly IntPtr HWND_TOP = new IntPtr(0);
    public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);


    [DllImport("user32.dll")]
    public static extern int FindWindow(string lpClassName, out IntPtr hWnd);

    [DllImport("User32.dll")]
    public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);


    [DllImport("user32.dll")]
    public static extern IntPtr GetActiveWindow();

    [DllImport("User32.dll")]
    public static extern uint GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("User32.dll")]
    public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("User32.dll")]
    public static extern int SetWindowLongPtr(IntPtr hWnd, int nIndex, uint dwNewLong);
    // 윈도우 투명 부분 clickthrough 처리
    [DllImport("User32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);   //-20, 0x80000


    //Window의 크기와 위치 변경     x : xPos, y : yPos, cx : Width, cy : Height
    [DllImport("User32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);



    // 검정색이 완전 투명처리, 상호작용이 가능한 색상 범위 설정
    [DllImport("User32.dll")]
    public static extern int SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

    public struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [DllImport("Dwmapi.dll")]
    public static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    [DllImport("Dwmapi.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);       //nCmdShow가 2이면 최소화, 3이면 최대화    ShowWindow function참조


    public class TestBlock
    {
        public IntPtr hWnd;     //Handled Windows
        public uint oldWindowLong;

        public void Test()
        {
            hWnd = GetActiveWindow();
            oldWindowLong = GetWindowLong(hWnd, GWL_EXSTYLE);

        }
    }
}


public class Display
{
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0; // 화면 너비
    private const int SM_CYSCREEN = 1; // 화면 높이

    public (int, int) Main()
    {
        // 기본 디스플레이 해상도 가져오기
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);

        return (screenWidth, screenHeight);
    }
}


public static class EnumConverter
{
    struct Shell<T> where T : struct
    {
        public int IntValue;
        public T Enum;
    }

    //https://www.slideshare.net/slideshow/enum-boxing-enum-ndc2019/142689361
    public static int EnumToInt32<T>(T e) where T : struct
    {
        Shell<T> s;
        s.Enum = e;
        unsafe
        {
            int* p = &s.IntValue;
            p += 1;
            return *p;
        }
    }

    public static T IntToEnum32<T>(int value) where T : struct
    {
        Shell<T> s = new Shell<T>();
        unsafe
        {
            int* p = &s.IntValue;
            p += 1;
            *p = value;
        }
        return s.Enum;
    }
}

//(ryzen7 5800x3d, ram 32gb) and (inten pentium g5400, ram 8gb) both (byte+implicit casting) > int
public enum Keys : byte    
{
    A = 0x41,

}

public class KeyBoard
{
    //핫키등록
    [DllImport("user32.dll")]        
    private static extern int RegisterHotKey(int hwnd, int id, int fsModifiers, int vk);        
    
    //핫키제거
    [DllImport("user32.dll")]       
    private static extern int UnregisterHotKey(int hwnd, int id);

    // Modifier 키 정의
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;


    private const int HOTKEY_ID = 125475;
    private static void Test(int hWnd)
    {

    }

}

public class Registry
{
    
}

