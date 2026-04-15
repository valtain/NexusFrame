프로젝트 전체 .cs 파일에서 C# 컴파일러 경고 패턴을 검사하고, 사용자 확인 후 선택적으로 수정해줘.

## 1. 검사 대상 파일 수집

`Assets/` 하위 모든 `.cs` 파일을 대상으로 한다.
생성 파일(`.Designer.cs`, `*.g.cs`) 및 `Tests/` 폴더는 제외.

## 2. 각 파일을 읽고 아래 경고 패턴 검사

### 체크 항목

**[W1] 미사용 using 지시문**
- `using Foo.Bar;` 로 선언된 네임스페이스가 해당 파일 내에서 전혀 참조되지 않는 경우
- 별칭(`using Alias = ...`) 포함
- → 안전 수정 가능: 해당 줄 삭제

**[W2] 미사용 private 필드**
- `private T _foo;` / `private T _foo = ...;` 형태로 선언된 필드가 선언부 외에 파일 내 어디서도 참조되지 않는 경우
- `[SerializeField]` 가 붙은 경우도 포함 (코드상 미참조이면 경고)
- → 수정 시 주의: `[SerializeField]` 붙은 경우 인스펙터 연결도 함께 확인 필요함을 안내

**[W3] 미사용 지역 변수**
- 메서드 내에서 선언/할당됐으나 이후 한 번도 읽히지 않는 변수
- `var result = Foo();` 이후 `result` 를 쓰지 않는 경우 등
- → 안전 수정 가능: 반환값이 필요 없으면 변수 선언 제거, 반환값 무시가 의도적이면 `_ = Foo();` 로 변경

**[W4] 빈 catch 블록**
- `catch { }` 또는 `catch (Exception) { }` 처럼 예외를 삼키는 경우
- → 수정 제안: 최소한 `Debug.LogException(e);` 추가

**[W5] 미사용 이벤트**
- `public event T Foo;` / `public UnityEvent Foo;` 형태로 선언됐으나 파일 내에서 `Invoke`, `+=`, `-=` 참조가 없는 경우

**[W6] `#pragma warning disable` 잔재**
- `#pragma warning disable CSXXXX` 가 있으나 대응하는 `#pragma warning restore` 가 없는 경우
- → 경고만 표시, 수정 안 함 (의도적일 수 있음)

## 3. 결과 출력

파일별로 그룹화하여 출력:

```
[fix-warnings] 검사 완료: N개 파일, M개 경고

━━ Runtime/Player/PlayerController.cs ━━
  #1  L14 [W1] using UnityEngine.AI → 미사용
  #2  L28 [W2] private float _jumpHeight → 코드 내 미참조 ([SerializeField] 있음 — 인스펙터 확인 필요)
  #3  L55 [W3] var angle = Vector3.Angle(...) → angle 이후 미사용

━━ Runtime/Ui/Views/BattleHud.cs ━━
  #4  L9  [W1] using System.Collections → 미사용
  #5  L77 [W4] catch { } → 예외 무시

전체 경고: 5개
```

## 4. 수정 방식 선택 요청

출력 후 사용자에게 다음과 같이 물어봐:

```
수정할 항목을 선택해줘:
  - 번호 입력 (예: 1, 3, 5)
  - 카테고리 입력 (예: W1, W2)
  - 'all' — 전체 자동 수정
  - 'skip' — 수정 없이 종료
```

## 5. 수정 적용

선택된 항목만 수정. 파일당 모든 수정을 한 번에 적용 (파일을 여러 번 쓰지 않도록).

수정 후 변경 내용을 파일별로 요약 출력:
```
✓ PlayerController.cs — 2건 수정 (L14 using 제거, L55 변수 제거)
✓ BattleHud.cs — 1건 수정 (L9 using 제거)
⚠ PlayerController.cs L28 [W2] — [SerializeField] 필드는 인스펙터 연결 확인 후 수동 제거 권장
```
