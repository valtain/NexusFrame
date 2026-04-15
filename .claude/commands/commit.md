현재 변경사항을 분석해서 커밋을 도와줘. 아래 순서로 진행해.

## 1. 변경 파일 목록 표시

`git status --short` 로 변경된 파일 목록을 출력하고, 사용자에게 stage할 파일을 선택하게 해줘.

```
변경된 파일:
  [1]  M  Assets/.../SceneCommon.cs
  [2]  M  Assets/.../MainViewLayer.cs
  [3] ??  Assets/.../Materials/

stage할 파일 번호를 입력해줘 (예: 1 2 / all / skip)
```

## 2. 선택된 파일의 diff 읽기

선택된 파일들의 `git diff HEAD -- <file>` 및 신규 파일은 내용을 읽어 변경 내용을 파악해.

## 3. 커밋 메시지 초안 작성

아래 기준으로 한국어 커밋 메시지 초안을 작성해:

**스타일 규칙:**
- 한국어, 짧고 명사형 종결 (예: "BattleHud 추가", "SceneDirector 리팩토링")
- 설계 의도 중심 — 파일 나열 아님 (예: "PlayerController 명명 규칙 정비" O / "PlayerController.cs 수정" X)
- 마침표 없음
- 여러 관심사가 섞였으면 가장 핵심적인 것 하나로 대표

**참고 기존 스타일:**
- "Battle session 초기 구현"
- "UiSystem 기본 구현"
- "ColdStartup 모듈 refactoring"
- "naming convention 에 맞게 수정"

초안 출력:
```
커밋 메시지 초안:
  > BattleHud 승리 버튼 이벤트 연결

수정하려면 메시지를 직접 입력해줘. 그대로 진행하려면 Enter.
```

## 4. 커밋 실행

확인이 되면:
1. 선택된 파일만 `git add`
2. 입력된 메시지로 `git commit`
3. 결과 출력
