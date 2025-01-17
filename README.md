<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>README</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            font-size: 15px;
            line-height: 1.6;
        }
        .standard {
            font-weight: bold;
            color: red;
            font-size: 18px;
        }
        .framework {
            font-weight: bold;
            color: blue;
            font-size: 18px;
        }
        .dotnet {
            font-weight: bold;
            color: purple;
            font-size: 18px;
        }
        .file {
            font-size: 22px;
            font-weight: bold;
            color: black;
            text-decoration: underline;
            cursor: pointer;
        }
        .interface {
            color: #FF8C00; /* 비주얼 스크립트 폰트 색상 */
        }
        .footer {
            color: gray;
            font-size: 12px;
            margin-top: 20px;
        }
    </style>
</head>
<body>
    <h1>README</h1>

    <h2 class="file">Registrys.cs</h2>
    <p><span class="dotnet">.NET Standard 2.1</span>에서는 <code>Registry</code> (<code>using Microsoft.Win32.Registry</code>)가 사용이 불가능합니다.</p>
    <p><span class="standard">Standard 2.1</span>를 유지하면서 레지스트리 키를 편집하기 위해 작성된 코드입니다.</p>
    <p>예제 함수는 <code>EX_Function</code>입니다.</p>
    <p><code>Float</code>과 <code>Vector3</code> (전처리됨)은 <code>string</code>으로 변환해서 사용합니다.</p>

    <h2 class="file">ThreadSafeList.cs</h2>
    <p><span class="interface">TheadSafeList</span> : <span class="interface">IList&lt;T&gt;</span>, <span class="interface">IEnumerable&lt;T&gt;</span>, <span class="interface">IDisposable</span>, <span class="interface">IEnumerable</span></p>
    <p>내부적으로 <code>List&lt;T&gt; _list</code>를 사용하며 <code>lock</code>으로 ThreadSafe 구현을 시도했습니다.</p>
    <p><span class="file">Registrys.cs</span>에서 Output으로 가져오는 레지스트리 값을 비동기로 수행하려고 만든 코드입니다.</p>

    <h2 class="file">Window.cs</h2>
    <p>현재 창 투명화 기능만 존재합니다.</p>

    <div class="footer">Work on 2022.3</div>
</body>
</html>