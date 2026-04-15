다음 단계로 코딩 컨벤션을 검사해줘. 수정은 하지 말고 위반 목록만 출력할 것.

## 1. 검사 대상 파일 수집

아래 두 명령어로 변경/신규 .cs 파일 목록을 수집해:
- `git diff --name-only HEAD` (변경된 파일)
- `git ls-files --others --exclude-standard` (untracked 신규 파일)

.cs 파일만 필터링. 파일이 없으면 "검사할 변경 파일 없음" 출력 후 종료.

## 2. 각 파일을 읽고 아래 항목 체크

### 체크 항목

**[A] private field 명 — `_camelCase` 규칙**
- `private` 필드(또는 `[SerializeField]` 붙은 필드)가 `_` 로 시작하지 않거나 대문자로 시작하면 위반
- `[SerializeField]` 가 붙은 필드의 rename 제안 시에는 반드시 `[FormerlySerializedAs("기존이름")]` 병기 필요함을 안내
- `[field: SerializeField]` 패턴으로의 변환은 절대 제안하지 않음 (backing field 이름 변경으로 씬/프리팹 직렬화 값 손실 위험)

**[B] public 필드 금지**
- MonoBehaviour 하위 클래스에서 `public T fieldName;` 형태 (SerializeField 없는 public 필드) → 위반
- `public T Property { get; ... }` 형태의 프로퍼티는 해당 없음

**[C] 인터페이스 prefix**
- `interface` 선언이 `I` 로 시작하지 않으면 위반

**[D] UniTask 규칙 — 코루틴 금지**
- `IEnumerator` 반환 메서드 또는 `StartCoroutine(` 사용 감지 → 위반
- `UniTask.Delay(` 대신 `yield return new WaitForSeconds(` 사용 → 위반

**[E] `.Forget()` 누락**
- `UniTask`를 반환하는 메서드 호출 결과를 `await` 하지도, `.Forget()` 하지도, 변수에 담지도 않고 버리는 경우 → 위반 (휴리스틱, 확실한 경우만 표시)

**[F] XML doc 누락**
- `public` 또는 `protected` 멤버(메서드, 프로퍼티, 이벤트)에 `/// <summary>` 가 없으면 위반
- 자동 생성 파일(`.Designer.cs`, `*.g.cs`) 제외

**[G] throw 사용**
- game-logic 코드에서 `throw new` 사용 감지 → 경고 (강제 아님, `Debug.Assert()` 권장 안내)

## 3. 출력 형식

```
[check-convention] 검사 파일: N개

⚠ Runtime/Player/PlayerController.cs
  L12 [A] [SerializeField] private field 'Speed' → '_speed' 로 rename 필요
          └ 씬/프리팹 값 보존: [FormerlySerializedAs("Speed")] 을 _speed 위에 추가 후 rename
  L34 [B] public field 'MoveSpeed' → [SerializeField] private _moveSpeed + 필요시 property 사용 권장

⚠ Runtime/Ui/Views/BattleHud.cs
  L8  [D] IEnumerator 사용 감지 → UniTask 로 교체 필요

✓ Runtime/SceneSystem/SceneDirector.cs — 위반 없음
```

위반이 하나도 없으면 마지막에 "전체 위반 없음" 출력.
