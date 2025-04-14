using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class KeyBoard
{
    //핫키등록
    [DllImport("user32.dll")]
    private static extern int RegisterHotKey(int hwnd, int id, int fsModifiers, int vk);

    //핫키제거
    [DllImport("user32.dll")]
    private static extern int UnregisterHotKey(int hwnd, int id);

    // Modifier 키 정의        | 로 비트연산해야 되니까 byte로 사용 x
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;


    private const int HOTKEY_ID = 125475;


    public const uint KEYEVENTF_KEYDOWN = 0x0000;  // 키 누르기
    public const uint KEYEVENTF_KEYUP = 0x0002;    // 키 떼기

    //핫키의 저장이 byte로 이루어 지는경우 span<byte>로

    //만약 UAC같은 윈도우의 특수한 접근을 요구하면 keybd_event는 차단됨
    [DllImport("user32.dll", SetLastError = true)]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    public static void KeyInput(Keys key)
    {
        keybd_event(KeyConverter(key), 0, KEYEVENTF_KEYDOWN, 0);
        Thread.Sleep(100);  // 100ms 대기
        keybd_event(KeyConverter(key), 0, KEYEVENTF_KEYUP, 0);
    }


    public static byte KeyConverter(Keys key)
    {
        return (byte)key;
    }

    private static void Test(int hWnd)
    {

    }

}
//(ryzen7 5800x3d, ram 32gb) and (inten pentium g5400, ram 8gb) both (byte+implicit casting) > int
public enum Keys : byte
{
    VK_LBUTTON = 0x01,
    VK_RBUTTON,
    VK_CANCEL,
    VK_MBUTTON,
    VK_XBUTTON1,
    VK_XBUTTON2,
    //	0x07	Reserved
    VK_BACK = 0x08,
    VK_TAB,
    //	0x0A-0B	Reserved
    VK_CLEAR = 0x0C,
    VK_RETURN,
    //  0x0E-0F	Unassigned
    VK_SHIFT = 0x10,
    VK_CONTROL,
    VK_ALT,             //VK_MENU
    VK_PAUSE,
    VK_CAPSLOCK,        //VK_CAPITAL
                        //  0x15            //VK_KANA,VK_HANGUL 
                        //  0x16            //VK_IME_ON
                        //  0x17            //VK_JUNJA
                        //...
    VK_ESCAPE = 0x1B,
    //...
    VK_SPACE = 0x20,
    VK_PAGEUP,          //VK_PRIOR
    VK_PAGEDOWN,        //VK_NEXT
    VK_END,
    VK_HOME,
    VK_LEFT,
    VK_UP,
    VK_RIGHT,
    VK_DOWN,
    VK_SELECT,
    VK_PRINT,
    VK_EXECUTE,
    VK_PRINTSCREEN,     //VK_SNAPSHOT
    VK_INSERT,
    VK_DELETE,
    VK_HELP = 0x2F,

    Alpha0 = 0x30,
    Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8,
    Alpha9 = 0x39,
    //  0x3A-40	Undefined
    VK_A = 0x41,
    VK_B, VK_C, VK_D, VK_E, VK_F,
    VK_G, VK_H, VK_I, VK_J, VK_K,
    VK_L, VK_M, VK_N, VK_O, VK_P,
    VK_Q, VK_R, VK_S, VK_T, VK_U,
    VK_V, VK_W, VK_X, VK_Y,
    VK_Z = 0x5A,

    VK_LWIN = 0x5B, VK_RWIN = 0x5C, VK_APPS = 0x5D,
    //  0x5E	Reserved
    VK_SLEEP = 0x5F,
    VK_NUMPAD0,
    VK_NUMPAD1,
    VK_NUMPAD2,
    VK_NUMPAD3,
    VK_NUMPAD4,
    VK_NUMPAD5,
    VK_NUMPAD6,
    VK_NUMPAD7,
    VK_NUMPAD8,
    VK_NUMPAD9,
    VK_MULTIPLY,
    VK_ADD,
    VK_SEPARATOR,
    VK_SUBTRACT,
    VK_DECIMAL,
    VK_DIVIDE = 0x6F,

    VK_F1 = 0x70,
    VK_F2, VK_F3, VK_F4, VK_F5, VK_F6, VK_F7, VK_F8, VK_F9, VK_F10, VK_F11, VK_F12,
    VK_F13, VK_F14, VK_F15, VK_F16, VK_F17, VK_F18, VK_F19, VK_F20, VK_F21, VK_F22, VK_F23,
    VK_F24 = 0x87,
    //  0x88-8F	Reserved
    VK_NUMLOCK = 0x90,
    VK_SCROLLLOCK,      //VK_SCROLL
    //  0x92-96	OEM specific
    //	0x97-9F	Unassigned
    VK_LSHIFT = 0xA0,
    VK_RSHIFT,
    VK_LCONTROL,
    VK_RCONTROL,
    VK_LMENU,
    VK_RMENU = 0xA5,
    VK_OEM_1 = 0xBA,    //:;
    VK_OEM_PLUS,        //+
    VK_OEM_COMMA,       //,
    VK_OEM_MINUS,       //-
    VK_OEM_PERIOD,      //.
    VK_OEM_2,           // /?
    VK_OEM_3,           //`~
    VK_OEM_4 = 0xDB,    //[{
    VK_OEM_5,           //\\|
    VK_OEM_6,           //]}
    VK_OEM_7 = 0xDE,    //'"
}