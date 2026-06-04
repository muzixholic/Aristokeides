---
phase: 8
slug: pr-inline-comments
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-04
---

# Phase 8 — PR Inline Comments - Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 10.0) |
| **Config file** | Aristokeides.Tests/Aristokeides.Tests.csproj |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~InlineComment"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~InlineComment"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green (except known external/flaky integration tests)
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 08-01-01 | 01 | 0 | CODE-04 / CODE-08 | — | — | unit | `dotnet test --filter "FullyQualifiedName~InlineComment"` | ❌ W0 | ⬜ pending |
| 08-01-02 | 01 | 0 | CODE-06 | T-08-01 | — | integration | `dotnet test --filter "FullyQualifiedName~InlineComment"` | ❌ W0 | ⬜ pending |
| 08-02-01 | 02 | 1 | CODE-04 | T-08-02 | 마크다운 프리뷰 시 악성 HTML이 차단되어 출력됨 | unit | `dotnet test --filter "FullyQualifiedName~InlineComment"` | ❌ W0 | ⬜ pending |
| 08-02-02 | 02 | 1 | CODE-04 / CODE-08 | T-08-03 | 리포지토리 조회 권한이 있는 사용자만 댓글 조회 및 편집 가능 | unit | `dotnet test --filter "FullyQualifiedName~InlineComment"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Aristokeides.Tests/Services/InlineCommentTests.cs` — CODE-04 및 CODE-08 비즈니스 로직 테스트 껍데기(Stub) 생성
- [ ] `Aristokeides.Tests/Data/InlineCommentDbTests.cs` — CODE-06 및 DB 연동 매핑 테스트 껍데기(Stub) 생성

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| PR Diff 변경 라인 옆에 마우스 호버 시 `+` 버튼 노출 및 클릭 시 인라인 댓글 폼 노출 | CODE-04 | 마우스 호버 및 동적 컴포넌트 삽입 등의 브라우저 DOM 동작 검증 | 1. PR 상세 페이지 진입<br>2. Diff 영역의 특정 행에 마우스 호버<br>3. `+` 버튼이 파란색으로 작게 나타나는지 확인<br>4. 버튼 클릭 시 행 아래에 에디터 폼이 생성되는지 확인 |
| 에디터에서 Write/Preview 탭을 전환하여 실시간 마크다운 프리뷰 검증 | CODE-04 | Blazor Server 동적 바인딩 및 탭 컨트롤 UI 요소 검증 | 1. 인라인 댓글 작성 창에서 Write 탭에 마크다운 텍스트 입력 (`**테스트**`) <br>2. Preview 탭으로 전환 시 `<strong>테스트</strong>`가 안전하게 렌더링되는지 확인 |
| 스레드 댓글 목록 접기(Resolve) 및 재개(Reopen) UI 애니메이션 검증 | CODE-08 | Blazor 아코디언 토글 렌더링 동작 확인 | 1. 부모 댓글 옆의 `Resolve conversation` 버튼 클릭<br>2. 해당 스레드가 접히며 `Resolved` 배지가 표시되는지 확인<br>3. `Show conversation` 링크 클릭 시 스레드가 확장되는지 확인 |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending 2026-06-04
