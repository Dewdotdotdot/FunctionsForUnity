using System.Collections;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;


using System.Text;

using Debug = UnityEngine.Debug;


public class PowerShell
{
    static string APPDATA =>   Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    static string MYDOCUMENT => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    static string PROGRAMFILE => System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    static string PROGRAMFILE32 => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    static string SYSTEM32 => Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
    static  string WINDOWS => Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    //SystemRool가 정확함
    const string cmdPath = @"%SystemRoot%\system32\cmd.exe";
    private static Process CMD(string fileName,string command, bool UseShellExecute, bool isAdmin, bool CreateNoWindow = false) 
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = UseShellExecute,
            Arguments = command,
            CreateNoWindow = CreateNoWindow,
            RedirectStandardError = UseShellExecute == false ? true : false,
            RedirectStandardInput = UseShellExecute == false ? true : false,
            RedirectStandardOutput = UseShellExecute == false ? true : false,
            StandardOutputEncoding = Encoding.UTF8,
            WindowStyle = CreateNoWindow == false ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
            Verb = isAdmin == true ? "runas" : ""
        };
        Process process = Process.Start(psi);
        return process;
    }


    private static void StreamWrite(Process process, params string[] param)
    {
        using (StreamWriter sw = process.StandardInput)
        {
            for(int i = 0; i < param.Length; i++)
            {
                sw.WriteLine(param[i]);
            }
            sw.Close();
        }
    }
    private static async void StreamWriteAsync(Process process, int delay, params string[] param)
    {
        using (StreamWriter sw = process.StandardInput)
        {
            for (int i = 0; i < param.Length; i++)
            {
                sw.WriteLine(param[i]);
                await Task.Delay(delay);
            }
            sw.Close();
        }
    }

    public static async Task<string> IPconfig()
    {
        try
        {
            string path = SYSTEM32 + @"\cmd.exe";
            Process getNetwork = CMD(path, $"/K chcp 437", false, false, false);
            if (getNetwork != null)
            {
                StreamWriteAsync(getNetwork, 10, "ipconfig -all");
                await Task.Delay(1000);
                var output = getNetwork.StandardOutput.ReadToEnd();
                if(!string.IsNullOrEmpty(output))
                {
                    throw new UnauthorizedAccessException("Read Error");
                }

                getNetwork.Close();
                getNetwork.Dispose();
                getNetwork = null;
                return output;
            }
            else
                throw new Exception("Process Error");

        }
        catch (Exception ex)
        {
            throw new Exception("Undefined Exception");
        }
    }

    public static Task<Process> AuthorityLevelDown(bool useAwait)
    {
        try
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
            return Task.FromResult(Process.Start(psi));
        }
        catch
        {
            throw new Exception("Undefined Exceptio");
        }
    }

    public static void TaskScheduler(string taskName, string filePath)
    {
        string arguments = $"/Create /F /SC ONCE /TN \"{taskName}\" /TR \"{filePath}\" /RL HIGHEST /ST 00:00";
        Process cmd = CMD("schtasks", arguments, false, true, false);

    }
    public static void RemoveTaskScedule(string taskName)
    {
        string argumeents = $"/Delete /F /TN {taskName}";
        Process cmd = CMD("schtasks", argumeents, false, true, false);
    }


    //        string filePath = "C:\\hide\\cmd";
    public static bool ExportText(string filePath, string extension, string text)
    {
        try
        {
            var path = filePath.Split("\\");
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(path[0]);
            stringBuilder.Append("\\");
            for (int i = 1; i < path.Length - 1; i++)
            {
                stringBuilder.Append(path[i]);
                stringBuilder.Append("\\");
                if (!Directory.Exists(stringBuilder.ToString()))
                {
                    Directory.CreateDirectory(stringBuilder.ToString());
                }
            }
            File.WriteAllText(filePath + extension, text);

            return true;
        }
        catch
        {
            throw new Exception("Undefined Exceptio");
        }
    }

    public static void PS()
    {
        string command = @"
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 0 -Force; 
Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'EnableLUA' -Value 0 -Force;";

        string log = @"c\error.log";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
            Arguments = $"-NoLogo -NoProfile -Command \"{command}\"",
            UseShellExecute = true,
            //RedirectStandardOutput = true,    //UseShellExecute를 true로 해야 관리자 권한으로 실행됨
            //RedirectStandardError = true,     //이하 동일
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal,
            Verb = "runas"
        };

        //psi.Arguments += $" 2> {log}";

        Process process = new Process { StartInfo = psi };
        process.Start();

        process.WaitForExit();


    }
        

}
