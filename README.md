# README

## **Registrys.cs**
.NET **Standard 2.1**에서는 `Registry` (`using Microsoft.Win32.Registry`)가 사용이 불가능해서
.NET Framework를 사용하지 않고 **Standard 2.1**을 유지하면서 레지스트리 키를 편집하기 위해 작성된 코드

`EX_Function`는 예제 함수
`Float`과 `Vector3` (전처리됨)은 `string`으로 변환해서 사용
ReadOnlyList를 써서 Input은 Serializable 안됨
RegistryBlock의 Property도 readonly로 선언되어 있어서 Serializable 안됨

---

## **ThreadSafeList.cs**
**ThreadSafeList** : `IList<T>`, `IEnumerable<T>`, `IDisposable`, `IEnumerable`  
내부적으로 `List<T> _list`를 사용하며 `lock`으로 ThreadSafe 구현을 시도 

**Registrys.cs**에서 Output으로 가져오는 레지스트리 값을 비동기로 수행하려고 만든 코드

---

## **Window.cs**
현재 창 투명화 기능만 존재

---


---

#### _Work on 2022.3_