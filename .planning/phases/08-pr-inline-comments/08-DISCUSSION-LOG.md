# Phase 8: PR Inline Comments - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-04
**Phase:** 08-PR Inline Comments
**Areas discussed:** 데이터 스키마 및 저장 구조, 라인 매핑 및 위치 지정 방식, 대화 스레드 및 답글 관리 방식, 인라인 댓글 작성 및 표시 UX

---

## 데이터 스키마 및 저장 구조

### Q1: 인라인 댓글을 저장할 데이터 모델을 어떻게 설계할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| (추천) 별도의 PullRequestReviewComment 테이블 생성 | 파일 경로, 라인 번호, DiffHunk, 해결 상태(IsResolved) 등 인라인 댓글에 특화된 필드를 포함하여 일반 이슈 댓글과 책임을 명확히 분리합니다. | ✓ |
| 기존 IssueComment 테이블 확장 | FilePath, LineNumber, DiffHunk, IsResolved, ParentId 등을 Nullable 필드로 추가하여 기존 테이블과 쿼리 로직을 재사용합니다. | |
| 자율 결정 (You decide) | 개발 단계에서 설계의 유연성과 유지보수성을 고려하여 적절히 판단하여 구현합니다. | |

**User's choice:** 별도의 PullRequestReviewComment 테이블 생성

### Q2: 인라인 댓글의 외래키 삭제 정책(On Delete)을 어떻게 설정할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| (추천) 작성자 삭제 시 Restrict / PR 삭제 시 Cascade | PR 삭제 시 연쇄 삭제(Cascade)하고, 작성자(User) 삭제 시에는 제한(Restrict)하여 데이터 무결성을 보장합니다. (기존 Issue.CreatorId 패턴 준수) | ✓ |
| 작성자 삭제 시 SetNull | 작성자가 삭제되면 AuthorId를 null로 변경하고 댓글 데이터 자체는 보존합니다. (댓글 작성자 관계가 Nullable이 됨) | |
| 자율 결정 (You decide) | 개발자가 기존 데이터베이스 패턴에 맞게 무결성 제약 조건을 설계합니다. | |

**User's choice:** 작성자 삭제 시 Restrict / PR 삭제 시 Cascade

---

## 라인 매핑 및 위치 지정 방식

### Q1: 인라인 댓글의 코드 위치(라인 정보)를 어떻게 데이터베이스에 저장할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| (추천) 원본/대상 라인 번호 및 라인 타입 모두 저장 | FilePath, OldLineNumber(선택), NewLineNumber(선택), LineType(추가/삭제/컨텍스트)을 모두 저장하여 향후 Phase 9의 라인 보정(Line Shift) 및 삭제 행 댓글 처리를 유연하게 지원합니다. | ✓ |
| 대상 파일의 라인 번호만 저장 | 새 변경사항(Target) 기준의 라인 번호만 단순하게 저장하여 구현을 직관적으로 유지합니다. | |
| 자율 결정 (You decide) | 개발자가 LibGit2Sharp diff 데이터 구조에 가장 적합한 라인 매핑 방식을 결정합니다. | |

**User's choice:** 원본/대상 라인 번호 및 라인 타입 모두 저장

### Q2: 인라인 댓글과 함께 저장할 Diff 컨텍스트(DiffHunk)의 저장 범위를 어떻게 정의할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| (추천) 댓글 대상 행 주변의 Hunk 컨텍스트 전체 저장 | 댓글이 달린 위치의 코드 맥락(보통 @@ header 아래의 변경 블록)을 텍스트로 보관하여, 향후 코드가 변경되더라도 당시 작성된 코드 맥락을 UI에 그대로 보여줄 수 있게 합니다. | ✓ |
| 대상 행의 코드 단일 라인만 저장 | 댓글이 달린 정확한 줄의 소스 코드만 보관하여 데이터를 간소화합니다. | |
| 자율 결정 (You decide) | 개발자가 적절한 범위의 코드 조각을 Diff Hunk 필드에 저장합니다. | |

**User's choice:** 댓글 대상 행 주변의 Hunk 컨텍스트 전체 저장

---

## 대화 스레드 및 답글 관리 방식

### Q1: 대화 스레드(답글)와 토론 해결(Resolve) 상태를 데이터베이스에서 어떻게 모델링할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| (추천) ParentId 기반 self-referencing 구성 | PullRequestReviewComment 테이블 내에 Nullable ParentId를 생성하여 답글이 첫 번째(부모) 댓글을 바라보게 합니다. 해결 상태(IsResolved)는 부모 댓글에서 관리하여 스레드를 통째로 접고 펼칠 수 있도록 제어합니다. | ✓ |
| 별도의 ReviewThread 테이블 도입 | 스레드(토론방)를 나타내는 별도 엔티티를 생성하고, 인라인 댓글들은 이를 참조하도록 설계합니다. 해결 상태는 이 Thread 엔티티에서 일괄 관리합니다. | |
| 자율 결정 (You decide) | 개발자가 ERD 및 쿼리 최적화 등을 고려하여 스레드 및 해결 상태 모델을 자율 설계합니다. | |

**User's choice:** ParentId 기반 self-referencing 구성

### Q2: 토론 해결(Resolve) 및 재개(Reopen)를 실행할 수 있는 권한 범위는 어떻게 설정할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| (추천) 모든 프로젝트 참여자(작성자, 리뷰어 모두) 가능 | 해당 PR을 볼 수 있는 권한이 있는 모든 사용자가 자유롭게 토론을 해결(Resolve)하거나 재개(Reopen)할 수 있도록 하여 협업 흐름을 원활하게 합니다. | ✓ |
| PR 작성자와 댓글 최초 작성자만 가능 | PR 소유자 또는 첫 댓글 작성자만 토론의 종결 권한을 갖도록 제한하여 책임 소지를 명확히 합니다. | |
| 자율 결정 (You decide) | 개발자가 적절히 판단하여 구현합니다. | |

**User's choice:** 모든 프로젝트 참여자(작성자, 리뷰어 모두) 가능

---

## 인라인 댓글 작성 및 표시 UX

### Q1: Pull Request Diff 화면에서 인라인 댓글 작성 창의 UX 및 마크다운 프리뷰를 어떻게 구성할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| (추천) Diff 라인 바로 아래 컴포넌트 삽입 및 탭 방식 마크다운 에디터+실시간 프리뷰 | 라인 호버 시 '+' 버튼이 노출되고 클릭 시 라인 아래에 작성 폼이 삽입됩니다. Write/Preview 탭을 전환하여 실시간으로 렌더링된 마크다운을 볼 수 있도록 합니다. | ✓ |
| 단순 텍스트 에어리어와 즉시 미리보기 레이아웃 | 탭 전환 없이 TextArea 바로 밑에 실시간으로 마크다운 프리뷰가 항상 표시되는 레이아웃을 제공합니다. | |
| 자율 결정 (You decide) | 개발자가 적절하고 미려한 에디터 UX를 설계합니다. | |

**User's choice:** Diff 라인 바로 아래 컴포넌트 삽입 및 탭 방식 마크다운 에디터+실시간 프리뷰

### Q2: 마크다운(Markdown) 파싱 및 HTML 렌더링을 위해 어떤 방식을 적용할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| (추천) NuGet 패키지 Markdig 사용 | C# 진영의 업계 표준 마크다운 파서인 Markdig 패키지를 추가하여, 서버에서 안전하게 HTML로 변환(MarkupString)하여 출력합니다. | ✓ |
| JS 기반 마크다운 렌더러 (예: marked.js) 활용 | 클라이언트 단에서 마크다운 렌더링을 처리하도록 JS 스크립트와 Interop을 활용합니다. | |
| 자율 결정 (You decide) | 개발자가 적절히 판단하여 구현합니다. | |

**User's choice:** NuGet 패키지 Markdig 사용

---

## Agent's Discretion

없음 — 모든 결정 사항을 사용자가 직접 선택함.

## Deferred Ideas

없음 — 논의가 Phase 범위 내에서 유지됨.
