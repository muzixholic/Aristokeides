---
phase: 11
slug: homepage-dashboard
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-08
---

# Phase 11 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET Core CLI) |
| **Config file** | [Aristokeides.Tests.csproj](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Tests/Aristokeides.Tests.csproj) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~RootControllerTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~1-5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~RootControllerTests"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green (existing core features)
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------------|-----------|-------------------|-------------|--------|
| 11-01-T1 | 11 | 1 | Repository 모델 확장 | `IsPrivate`, `PrimaryLanguage`, `UpdatedAt` 속성 추가 및 마이그레이션 생성 | compile | `dotnet build` | ✅ | ✅ green |
| 11-01-T2 | 11 | 1 | 루트 리다이렉션 라우팅 | `RootController.cs` 추가 및 로그인 여부에 따른 리다이렉션 분기 검증 | unit | `dotnet test --filter "FullyQualifiedName~RootControllerTests"` | ✅ | ✅ green |
| 11-01-T3 | 11 | 1 | Bootstrap Icons 추가 | `App.razor` 내 CDN 스타일시트 태그 유무 검증 | human-check | (manual) | ✅ | ✅ green |
| 11-01-T4 | 11 | 1 | 랜딩 페이지 구현 | `Home.razor` 비로그인 전용 화면 컴파일 및 링크 연동 검증 | human-check | (manual) | ✅ | ✅ green |
| 11-01-T5 | 11 | 1 | 대시보드 구현 | `Dashboard.razor` 리포지토리 카드 뷰, 메타데이터 연동 및 Empty State 검증 | human-check | (manual) | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 비로그인 랜딩 페이지 소개 UI 및 가입/로그인 CTA 링크 동작 | 랜딩 페이지 | 정적 SSR 컴포넌트 렌더링 및 페이지 레이아웃 시각 디자인 요소 대조 | 웹 브라우저에서 `/home`에 접속하여 주요 소개 내용(Git 호스팅, 이슈 트래커, 코드 리뷰)이 제대로 출력되는지 확인하고 "시작하기" 및 "로그인" 링크가 올바르게 작동하는지 검증 |
| 로그인 사용자 리포지토리 대시보드 그리드 뷰 및 Empty State | 대시보드 | 로그인 사용자 상태의 브라우저 세션 연동 및 동적 UI 렌더링 확인 | 1. 가입 후 대시보드 진입 시 "생성된 저장소가 없습니다" 문구 및 CTA 버튼 노출 확인. 2. 저장소 생성 후 자물쇠 아이콘, 수정 시간, 사용 언어 배지가 적절하게 표시되는지 검증 |
| 루트 경로 `/`로의 접근 시 세션 상태별 자동 전환 | 루트 리다이렉션 | 브라우저 세션 유무에 따른 HTTP 302 리다이렉트 최종 플로우 검증 | 세션이 없는 브라우저 창에서 `/` 접속 시 `/home`으로 리다이렉트되고, 로그인된 브라우저 창에서 `/` 접속 시 `/dashboard`로 즉시 연결되는지 확인 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 5s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-08
