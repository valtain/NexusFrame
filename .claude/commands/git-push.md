push 전 안전 검사를 수행하고 git push를 실행해줘. 아래 순서로 진행해.

## 1. 원격 동기화 (rebase)

`git fetch origin` 으로 원격 최신 상태를 가져온 후,
`git log HEAD..origin/main --oneline` 으로 원격에 새 커밋이 있는지 확인해.

**원격에 새 커밋 없으면** → 2단계로 바로 이동.

**원격에 새 커밋 있으면** → 목록 표시 후 선택:

```
원격에 N개 새 커밋이 있습니다:
  abc1234 [feat] 적 AI 기본 이동 구현
  def5678 [fix] 체력 바 갱신 버그 수정

선택해줘:
  [1] rebase 후 계속 (권장)
  [2] rebase 생략하고 계속
  [3] 취소
```

- **[1] 선택**: `git rebase origin/main` 실행
  - 충돌 발생 시: 충돌 파일 목록 표시 → 해결 후 `git rebase --continue` 안내 → 해결되면 다음 단계 진행
  - 충돌 없으면: 다음 단계 진행
- **[2] 선택**: rebase 없이 다음 단계 진행
- **[3] 선택**: 종료

## 2. 미커밋 변경사항 확인

`git status --short` 로 확인해서 미커밋 변경사항이 있으면 아래처럼 경고 표시:

```
⚠ 미커밋 변경사항이 있습니다:
   M  Assets/.../SceneCommon.cs
  ?? Assets/.../NewFile.cs

계속 push하려면 Enter, 취소하려면 'cancel' 입력.
```

## 2. C# 경고 스캔

Assets/ 하위 모든 .cs 파일을 검사 (*.Designer.cs, *.g.cs, Tests/ 폴더 제외).

검사 항목:
- **[W1]** 미사용 using 지시문
- **[W2]** 미사용 private 필드 (코드 내 미참조, [SerializeField] 포함)
- **[W3]** 미사용 지역 변수
- **[W4]** 빈 catch 블록 (`catch { }` 또는 `catch (Exception) { }`)
- **[W5]** 미사용 이벤트
- **[W6]** `#pragma warning disable` 잔재 (restore 없는 경우)

## 3. 검사 결과 처리

**경고 없으면** → 바로 push 진행.

**경고 있으면** → 목록 표시 후 선택 요청:

```
[git-push] 경고 N개 발견:

⚠ Runtime/Player/PlayerController.cs
  L14 [W1] using UnityEngine.AI → 미사용
  L28 [W2] private float _jumpHeight → 코드 내 미참조

선택해줘:
  [1] /fix-warnings 실행 후 push
  [2] 경고 무시하고 push
  [3] 취소
```

- **[1] 선택**: fix-warnings 커맨드 로직으로 수정 진행 후 push
- **[2] 선택**: 경고 무시하고 push 진행
- **[3] 선택**: 종료

## 4. push 실행

`git push $ARGUMENTS` 실행 (인자 없으면 `git push` 그대로).

결과 출력:
```
✓ push 완료
```
