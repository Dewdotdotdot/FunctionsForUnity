# README

## **Registrys.cs**
.NET **Standard 2.1**에서는 `Registry` (`using Microsoft.Win32.Registry`)가 사용이 불가능합니다.  
.NET Framework를 사용하지 않고 **Standard 2.1**을 유지하면서 레지스트리 키를 편집하기 위해 작성된 코드입니다.  

예제 함수는 `EX_Function`입니다.  
`Float`과 `Vector3` (전처리됨)은 `string`으로 변환해서 사용합니다.  

---

## **ThreadSafeList.cs**
**ThreadSafeList** : `IList<T>`, `IEnumerable<T>`, `IDisposable`, `IEnumerable`  
내부적으로 `List<T> _list`를 사용하며 `lock`으로 ThreadSafe 구현을 시도했습니다.  

**Registrys.cs**에서 Output으로 가져오는 레지스트리 값을 비동기로 수행하려고 만든 코드입니다.  

---

## **Window.cs**
현재 창 투명화 기능만 존재합니다.  

---

### 스타일
- **ThreadSafeList** : `I`로 시작하는 인터페이스는 비주얼 스크립트 폰트 색상.
- 기본 폰트 사이즈는 15.
- **Standard 2.1** : **굵은 글씨**와 붉은색, 폰트 크기 18.
- **Framework** : **굵은 글씨**와 파란색, 폰트 크기 18.
- **.NET** : **굵은 글씨**와 보라색, 폰트 크기 18.
- `.cs` 파일은 리다이렉트 스타일로 표시하며 폰트 크기 22, **굵은 글씨**로 작성.

---

#### _Work on 2022.3_