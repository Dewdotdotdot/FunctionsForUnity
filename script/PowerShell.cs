using System.Collections;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using static UnityEngine.Rendering.DebugUI;
using UnityEngine;
using System.Text;


public class PowerShell : MonoBehaviour
{
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha9))
        {
            PS();
        }
    }

    public void PS()
    {
        string command = @"
Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 0 -Force; 
Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System' -Name 'EnableLUA' -Value 0 -Force;";

        string log = @"c\error.log";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
            Arguments = $"-NoLogo -NoProfile -Command \"{command}\"",
            UseShellExecute = false,
            //RedirectStandardOutput = true,    //UseShellExecute를 true로 해야 관리자 권한으로 실행됨
            //RedirectStandardError = true,     //이하 동일
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal,
            Verb = "runas"
        };

        //psi.Arguments += $" 2> {log}";

        Process process = new Process { StartInfo = psi };
        process.Start();

        //string output = process.StandardOutput.ReadToEnd();   //사용불가
        //string error = process.StandardError.ReadToEnd();     //사용불가
        process.WaitForExit();

        /*
        if(!string.IsNullOrEmpty(error))
        {
            throw new Exception(error);
        }
        */
    }

    private static string ItemProperty(in string path, in string name, in string value)
        => $"Set-ItemProperty -Path '{path}' -Name '{name}' -Value '{value}'";
    private static string ItemPropertys(in (string path, string name, string value)[] datas )
    {
        StringBuilder stringBuilder = new StringBuilder();
        for(int i = 0; i < datas.Length; i++)
        {
            stringBuilder.Append("-NoLogo ");
            stringBuilder.Append("-NoProfile ");
            stringBuilder.Append("Set-ItemProperty ");
            stringBuilder.Append("-Path '").Append(datas[i].path).Append("' ");
            stringBuilder.Append("-Name '").Append(datas[i].name).Append("' ");
            stringBuilder.Append("-Value '").Append(datas[i].value).Append("' ");

            if(i != datas.Length - 1)
            {
                stringBuilder.Append("; ");
            }
        }

        return stringBuilder.ToString();

    }
        

}
