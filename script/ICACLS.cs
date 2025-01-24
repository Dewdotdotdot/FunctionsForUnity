using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.IO;


public class ICACLS
{
    /*  string argument
 *  Regular Literal         Verbatim Literal            Resulting string
 *  "Hello"                 @"Hello"                    Hello
 *  "Backslash: \\"         @"Backslash: \"             Backslash: \
 *  "Quote: \""             @"Quote: """                Quote: "
 *  "CRLF:\r\nPost CRLF"    @"CRLF:                     CRLF:   
                            Post CRLF"                  Post CRLF
 * \b : backspace       \n : newline        \r : Carriage return        \t : hotizontal tab     \v : vertical tab
 */

    /*  ICACLS
     *  %SystemRoot%\* system32 이하의 모든 파일
     *  SID를 사용하면 SID앞에 * 붙이기 EX) *S-1-1-0
     *  /T  하위에 있는 모든 파일에도 적용
     *  /C  오류가 있어도 계속 진행
     *  /L  대상 대신 바로 가기 링크에서 작업 수행
     *  /Q  작업 성공 메시지를 표시하지 않음
     *  N : 권한 없음       F : 모든 권한       M : 수정권한
     *  RX : 읽기 및 실행   R : 읽기 전용       W : 쓰기 전용
     *  D : 삭제 권한
     */

    private const string EVERYONE = "Everyone";
    public string PermissionBlock(params char[] permission)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append('"');
        for (int i = 0; i < permission.Length; i++)
        {
            sb.Append(',');
        }

        sb.Append('"');
        return sb.ToString();
    }
    public string ArgumentBlock(string filePath, string user, string permision)
    {
        string arguments = $"\"{filePath}\" /grant \"{user}:{permision}\"";
        return arguments;
    }
    public Task<string> Authority(string arguments)
    {

        try
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "icacls",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas"
            };

            // 프로세스 실행
            using (Process process = Process.Start(processInfo))
            {
                process.WaitForExit();

                // 출력 결과 확인
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (string.IsNullOrEmpty(error))
                {
                    return Task.FromResult(output);
                }
                else
                {
                    return Task.FromResult(error);  //명령 실행 실패
                }
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(ex.Message);
        }

    }
}
