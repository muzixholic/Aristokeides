---
phase: 10
slug: user-auth-ui
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-08
---

# Phase 10 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET Core CLI) |
| **Config file** | [Aristokeides.Tests.csproj](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Tests/Aristokeides.Tests.csproj) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~AuthControllerTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~1-5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~AuthControllerTests"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 10A-01-T1 | 10A | 1 | 회원가입 기능 | T-10-01, T-10-02 | `cookie-register` API 바인딩, DB 사용자 추가, BCrypt 암호화 및 기본 역할 Reader 적용 검증 | unit | `dotnet test --filter "FullyQualifiedName~AuthControllerTests"` | ✅ | ✅ green |
| 10A-01-T2 | 10A | 1 | 회원가입 기능 | T-10-03 | `Register.razor` Blazor SSR 폼 마크업 및 CSRF AntiforgeryToken 검증 | human-check | (manual) | ✅ | ✅ green |
| 10B-01-T1 | 10B | 1 | 로그인/아웃 | — | `Login.razor` UI 한국어 로컬라이징 및 회원가입 페이지 링크 연동 | human-check | (manual) | ✅ | ✅ green |
| 10B-01-T2 | 10B | 1 | 로그인/아웃 | T-10-04 | `Login.razor` 쿼리 파라미터 기반 에러/성공 박스 노출 분기 검증 | human-check | (manual) | ✅ | ✅ green |
| 10C-01-T1 | 10C | 2 | 로그인/아웃 | T-10-03 | `MainLayout.razor` 네비게이션 바 회원가입 버튼 추가 및 로그아웃 404 URL 디렉션 검증 | human-check | (manual) | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 회원가입 폼 및 비밀번호 확인 일치 스크립트 | 회원가입 기능 | Blazor SSR 정적 폼 환경에서 비밀번호 불일치 시 JS 차단 확인 및 브라우저 제출 스키마 동작 | `/register`에 접속하여 비밀번호 확인에 다른 값을 입력한 뒤 가입 시도하여 에러가 즉시 표시되고 제출이 차단되는지 브라우저에서 검증 |
| 로그인/성공/에러 알림 UI 렌더링 및 번역 대조 | 로그인/아웃 | 웹 브라우저의 렌더링 결과(빨간색 경고창, 연두색 가입 완료 메시지 박스) 및 한국어화 정합성 대조 | `/login` 페이지에 접속하여 한국어 텍스트 및 상황별 쿼리 파라미터 인입 시의 Alert 박스 레이아웃 시각적 검사 |
| MainLayout 로그인/아웃 세션 연동 및 이동 | 로그인/아웃 | 웹 브라우저 상에서의 실제 세션 해제(Sign-Out) 및 리다이렉트 처리 여부 확인 | 로그인 상태에서 "로그아웃" 클릭 시 `/api/auth/logout`을 호출하고 세션 만료 후 첫 페이지로 정상 롤백되는지 관찰 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 5s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-08
