using System.Collections;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Text;
using System.Runtime.InteropServices;

using Debug = UnityEngine.Debug;

using System.Linq;
using System.Collections.Generic;


public class ProcessControl : MonoBehaviour
{
    const int WM_SETTEXT = 0X000C;

    [DllImport("user32.dll")]
    public static extern int FindWindow(string lpClassName, out IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
    [DllImport("user32.dll")]
    public static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]   //창 활성화
    private static extern bool SetForegroundWindow(IntPtr hWnd);



    private const int SW_HIDE = 0;        // 창 숨기기
    private const int SW_SHOW = 5;       // 창 보이기
    private const int SW_MINIMIZE = 6;   // 최소화
    private const int SW_MAXIMIZE = 3;   // 최대화
    [DllImport("user32.dll")]   //창 제어
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);



    [DllImport("user32.dll")]   //창 제목 가져오기
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);


    private const int WM_CLOSE = 0x0010; // 창 닫기 메시지
    private const int WM_KEYDOWN = 0x0100; // 키 입력 메시지
    private const int WM_KEYUP = 0x0101;
    [DllImport("user32.dll")]
    private static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    //SendMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero); // 닫기 메시지 전송
    //SendMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_A, IntPtr.Zero); // 키 누름
    //SendMessage(hWnd, WM_KEYUP, (IntPtr) VK_A, IntPtr.Zero);   // 키 뗌

    [DllImport("user32.dll")]
    private static extern bool SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, string lParam);
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Keypad7))
        {
            GetProcessIntPtr(Environment.GetEnvironmentVariable("SystemRoot") + "\\system32\\Notepad.exe", "", false, false);
        }
        if(Input.GetKeyDown(KeyCode.Keypad8))
        {
            Debug.Log(GetForegroundWindow().ToString());
            var a = GetForegroundWindow();
            ShowWindow(a, SW_MINIMIZE);
        }
    }

    public static async void GetProcessIntPtr(string fileName, string arguments,bool shellExcute, bool isAdmin)
    {
        //Process process = await StartProcess(fileName, arguments, shellExcute, isAdmin);
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = fileName,
            //Arguments = arguments,
            RedirectStandardOutput = !shellExcute,
            RedirectStandardError = !shellExcute,
            UseShellExecute = shellExcute,
            //CreateNoWindow = false,
            //WindowStyle = ProcessWindowStyle.Normal,
            Verb = isAdmin == true ? "runas" : ""
        };
        Process process = Process.Start(processInfo);
        process.Refresh();
        await Task.Delay(1000);

        // 출력 결과 확인
        //string output = process.StandardOutput.ReadToEnd();
        //string error = process.StandardError.ReadToEnd();

        //if (!string.IsNullOrEmpty(error))
        // {
        //  throw new UnauthorizedAccessException(error);
        // }

        Debug.Log(process.Id);
        //IntPtr hwnd = process.MainWindowHandle;     //핸들 부분이 문제가 있음 process.Handle은 hwnd이 아님
        if (GetTargetProcessId("Notepad", process.StartTime, out int gettedID, out IntPtr hWnd) == false)
            throw new ArgumentNullException("Process name is wrong");

        IntPtr hWndEdit = NotepadWindowHandle(true, in hWnd);
        SendMessage(hWndEdit, WM_SETTEXT, IntPtr.Zero, ""); //됨


        if (ShowWindow(hWnd, SW_MINIMIZE) == false)
            Debug.Log("Show Window Failed");

        var foregrounder = Task.Run(() => PinWindow(hWnd));

        //KeyBoard.keybd_event(KeyBoard.KeyConverter(Keys.VK_NUMPAD5), 0, KeyBoard.KEYEVENTF_KEYDOWN, 0);
        //KeyBoard.keybd_event(KeyBoard.KeyConverter(Keys.VK_NUMPAD5), 0, KeyBoard.KEYEVENTF_KEYUP, 0);

        await Task.Delay(1000);


        string typingMessage = "롤스는 '정의론'에서 \"정의는 타인들이 갖게 될 보다 큰 선을 위하여 소수의 자유를 뺏는 것이 정당화될 수 없다\"고 주장하며 \"다수가 누릴,보다 큰 이득을 위해 소수에게 희생을 강요해도 좋다는 것을 정의는 용납할 수 없다\"며 평등의 원칙을 제시한다";
        ReadOnlyMemory<char> mem = typingMessage.AsMemory();
        await Task.Run(() => NotepadTyping(hWndEdit, mem, 100));

        Debug.Log(process.Id);

        await Task.WhenAll(Task.Run(() => { process.WaitForExit(); }), foregrounder);
    }

    private static async Task PinWindow(IntPtr hWnd)
    {
        ShowWindow(hWnd, 9);
        while (hWnd != IntPtr.Zero && hWnd != null)
        {
            if (GetForegroundWindow() != hWnd)
            {
                SetForegroundWindow(hWnd);
                ShowWindow(hWnd, SW_SHOW);
            }
            await Task.Delay(100);
        }
        await Task.Delay(100);
    }

    private static IntPtr NotepadWindowHandle(bool isWin11, in IntPtr hWnd)
    {
        try
        {
            IntPtr hWndEdit = IntPtr.Zero;
            if (isWin11 == false)
            {
                hWndEdit = FindWindowEx(hWnd, IntPtr.Zero, "Edit", null);
                if (hWndEdit == IntPtr.Zero)
                    throw new NullReferenceException("Can nor find Notepad Edit class");
                return hWndEdit;
            }
            else
            {
                hWndEdit = FindWindowEx(hWnd, IntPtr.Zero, "NotepadTextBox", null);
                if (hWndEdit == IntPtr.Zero)
                    throw new NullReferenceException("Can not find Notepad TextBox");
                hWndEdit = FindWindowEx(hWndEdit, IntPtr.Zero, "RichEditD2DPT", null);
                if (hWndEdit == IntPtr.Zero)
                    throw new NullReferenceException("Can not find Notepad Edit class");
                return hWndEdit;
            }
        }
        catch
        {
            throw new Exception("Can not find class Name");
        }
    }

    private static async Task NotepadTyping(IntPtr hWnd, ReadOnlyMemory<char> text, int delayTime)
    {
        StringBuilder sb = new StringBuilder(text.Span.Length);

        for (int i = 0; i < text.Span.Length; i++)
        {
            sb.Append(text.Span[i]);

            SendMessage(hWnd, WM_SETTEXT, IntPtr.Zero, sb.ToString()); //됨
            await Task.Delay(delayTime);
        }
    }

    public static async Task<Process> StartProcess(string fileName, string arguments, bool shellExcute,  bool isAdmin)
    {
        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = !shellExcute,
            RedirectStandardError = !shellExcute,
            UseShellExecute = shellExcute,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal,
            Verb = isAdmin == true ? "runas" : ""
        };
        Process process = Process.Start(processInfo);
        return process ;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    // Delegate to filter which windows to include 
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    //이것만 사용하는 쪽으로
    static bool GetTargetProcessId(string processName, DateTime pStartTime, out int processID, out IntPtr hWnd)
    {
        processID = 0;
        hWnd = IntPtr.Zero;
        Process[] processes = Process.GetProcessesByName(processName);
        var query = processes.Where(x => x.ProcessName.ToLower().Contains(processName.ToLower())).ToList();
        
        for (int i = 0; i< query.Count;i++)
            Debug.Log(query[i].Id.ToString());
        if(query.Count <= 0)
            return false;


        //IntPtr이 Zero인 프로세스를 거름
        var filtered = new List<(Process, IntPtr)>();
        for (int i = 0;i< query.Count;i++)
        {
            if (FindhWndByProcessID(query[i].Id, out IntPtr ptr)==true)
            {
                filtered.Add((query[i], ptr));
            }
        }

        //시간 계산
        var shortest = (from x in filtered
                        where pStartTime.Date == x.Item1.StartTime.Date
                    orderby (Math.Abs(x.Item1.StartTime.Ticks - pStartTime.Ticks))
                    select x).First();
        Debug.Log(shortest.Item1.StartTime.TimeOfDay.ToString());
        processID = shortest.Item1.Id;
        hWnd = shortest.Item2;
        return true;

    }
    public static bool FindhWndByProcessID(int targetID, out IntPtr targethWnd)
    {
        try
        {
            IntPtr ptr = IntPtr.Zero;
            //Process targetProcess = Process.GetProcessById(targetID);
            EnumWindows((hWnd, lParam) =>
            {
                int processID = 0;
                int threadID = GetWindowThreadProcessId(hWnd, out processID);
                if (processID == targetID)//targetProcess.Id)
                {
                    ptr = hWnd;
                    Debug.Log("Find");
                    return false;
                }
                else
                    return true;

            }, IntPtr.Zero);
            
            targethWnd = ptr;
            Debug.Log(targetID + " is " + ptr.ToString());
            if(targethWnd == IntPtr.Zero)
                return false;
            else
                return true;
        }
        catch
        {
            throw new InvalidDataException();
        }
    }    
}
