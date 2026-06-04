---
phase: 08-pr-inline-comments
plan: 02
subsystem: ui
tags: [markdig, blazor, dotnet, xunit]

requires:
  - phase: 08-01
    provides: "인라인 댓글 기본 엔티티 및 스키마"
provides:
  - "안전한 마크다운 렌더러 MarkdownRenderer"
  - "PullRequestService 내 대댓글 추가 및 해결/재개 API"
  - "Blazor UI 내 Write/Preview 마크다운 탭 에디터, 대댓글 스레드 렌더링, 대화 해결/재개 상호작용"
affects: []

tech-stack:
  added: [Markdig]
  patterns: [안전한 마크다운 HTML 이스케이프 및 이중 탭 에디터 바인딩]

key-files:
  created:
    - Aristokeides.Api/Services/MarkdownRenderer.cs
  modified:
    - Aristokeides.Api/Aristokeides.Api.csproj
    - Aristokeides.Api/Services/PullRequestService.cs
    - Aristokeides.Api/Components/Pages/RepoPullRequestDetail.razor
    - Aristokeides.Tests/Services/InlineCommentTests.cs

key-decisions:
  - "Markdig의 DisableHtml() 파이프라인 설정을 통해 XSS 및 악성 스크립트 실행을 완벽히 격리하도록 설계함."
  - "대댓글의 ParentId를 식별하여 부모 댓글 하단에 24px 들여쓰기 처리 및 스레드 형태로 중첩 렌더링되도록 Blazor UI 구조화."

patterns-established:
  - "마크다운 입력에 대한 Write/Preview 토글 및 Blazor (MarkupString) 실시간 바인딩 패턴"

requirements-completed:
  - CODE-04
  - CODE-08

duration: 15min
completed: 2026-06-04
---

# Phase 8: PR Inline Comments - Plan 2 Summary

**Markdig 패키지를 활용한 안전한 마크다운 HTML 렌더러와 대댓글(스레드) 계층 구조 및 해결/재개 상호작용 UI 완료**

## Performance

- **Duration:** 15 min
- **Started:** 2026-06-04T11:32:03+09:00
- **Completed:** 2026-06-04T11:47:00+09:00
- **Tasks:** 3
- **Files modified:** 4

## Accomplishments
- Markdig 라이브러리 연동 및 `.DisableHtml()`을 통해 악성 스크립트 실행을 원천 차단하는 마크다운 렌더러(`MarkdownRenderer.cs`) 구현.
- `PullRequestService.cs`에 대댓글을 달기 위한 `AddReplyCommentAsync`와 스레드의 해결 상태를 관리하기 위한 `ResolveReviewCommentAsync`, `UnresolveReviewCommentAsync` 비즈니스 로직 및 xUnit 단위 테스트 구축.
- `RepoPullRequestDetail.razor` Blazor 컴포넌트에 마크다운 `Write/Preview` 탭 에디터를 지원하고, 부모-자식 계층 구조의 대댓글 스레드를 정렬하여 렌더링하며, 해결된 스레드를 아코디언식으로 접고 펼치며 재개하는 상호작용 UI 구현.

## Task Commits

1. **Task 1: NuGet 패키지 Markdig 설치** - (feat)
2. **Task 2: 안전한 마크다운 렌더러 및 대댓글/해결 로직 & 테스트 구현** - (feat/test)
3. **Task 3: Blazor UI 에디터 탭, 대댓글 스레드 및 해결/재개 UI 통합** - (feat/ui)

## Files Created/Modified
- `Aristokeides.Api/Services/MarkdownRenderer.cs` - 마크다운 안전 렌더러 구현 (신규)
- `Aristokeides.Api/Aristokeides.Api.csproj` - Markdig 패키지 참조 추가 (수정)
- `Aristokeides.Api/Services/PullRequestService.cs` - 대댓글 생성 및 상태 변경 로직 추가 (수정)
- `Aristokeides.Api/Components/Pages/RepoPullRequestDetail.razor` - Blazor UI 에디터 및 댓글 계층/해결 상태 토글 UI 수정 (수정)
- `Aristokeides.Tests/Services/InlineCommentTests.cs` - 마크다운 XSS 차단 및 로직 검증 xUnit 테스트 코드 수정 (수정)

## Decisions Made
- 마크다운 미리보기 전환 시 사용자가 임의의 HTML/CSS를 주입하여 XSS 취약점을 공격하는 STRIDE 위협 T-08-03을 차단하기 위해, Markdig 파이프라인에서 `.DisableHtml()` 설정을 강제 활성화하고 모든 HTML 날것의 태그를 단순 평문 텍스트로 안전하게 이스케이프시켰습니다.
- 대댓글이 추가될 때 부모 댓글의 파일 위치 및 라인 정보를 계승하여 올바르게 렌더링에 반영되도록 `AddReplyCommentAsync` 내에 세팅 로직을 추가하였습니다.

## Deviations from Plan
None - plan executed exactly as written

## Issues Encountered
- `User` 모델 인스턴스 생성 시 필수 구성원인 `Role` 속성이 설정되지 않아 테스트 컴파일 에러(CS9035)가 발생하였습니다. 테스트 코드 내의 `User` 개체 이니셜라이저에 `Role = "Contributor"`를 지정하여 해결하였습니다.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- 인라인 댓글의 작성, 마크다운 렌더링, XSS 방지, 대댓글 스레드 및 대화 해결/재개 동작이 백엔드와 프론트엔드 통합 완료되었습니다.
- 관련 xUnit 인라인 댓글 테스트 8건이 모두 성공하여, 다음 단계인 전체 통합 및 리뷰 단계로 진행할 수 있습니다.
