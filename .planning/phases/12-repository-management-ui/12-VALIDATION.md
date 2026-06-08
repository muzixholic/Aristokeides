---
phase: 12
slug: repository-management-ui
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-08
---

# Phase 12 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET Core CLI) |
| **Config file** | [Aristokeides.Tests.csproj](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Tests/Aristokeides.Tests.csproj) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~RepositoriesControllerTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~1-5 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~RepositoriesControllerTests"`
- **Before `/gsd-verify-work`:** Full suite must be green (existing core features)
- **Max feedback latency:** 5 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------------|-----------|-------------------|-------------|--------|
| 12-01-T1 | 12 | 1 | 신규 저장소 생성 | `/repositories/new` 입력 및 DB 인서트, 기본 칸반 컬럼 추가, 백엔드 Git 생성 큐 등록 검증 | unit | `dotnet test --filter "FullyQualifiedName~RepositoriesControllerTests"` | ✅ | ✅ green |
| 12-01-T2 | 12 | 1 | 저장소 설정 변경 | 이름/설명/비공개 여부 편집 기능, 이름 변경 시 물리 디렉토리명 연동 이전, 완료 시 성공 박스 출력 검증 | human-check | (manual) | ✅ | ✅ green |
| 12-01-T3 | 12 | 1 | 저장소 영구 삭제 | Danger Zone 배치, `{Username}/{RepoName}` 입력 강제 안전 모달, 물리 파일 및 DB 레코드 Cascade 삭제 검증 | human-check | (manual) | ✅ | ✅ green |
| 12-01-T4 | 12 | 1 | 헤더 탭 메뉴 연동 | `RepoBrowser.razor` 및 `RepoIssues.razor`에 소유자 한정 "Settings" 탭 렌더링 검증 | human-check | (manual) | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 신규 저장소 생성 입력 값 유효성 및 중복 검사 | 신규 저장소 생성 | 정적 검증 필터 및 중복 저장소 명칭 입력 시 UI 반응 확인 | `/repositories/new`에서 비어있는 이름 제출 시 에러 발생 및 동일 이름 입력 시 중복 경고창이 상단에 정상 출력되는지 검증 |
| 설정 페이지 성공 피드백 알림 및 디렉토리명 변경 | 저장소 설정 변경 | 이름 변경 시 디렉토리 명칭 물리적 전환 확인 및 성공 박스 시각 검사 | `/settings`에서 이름 및 설명 편집 저장 후, 화면에 녹색 성공 박스가 표시되고 실제 `GitRepos/{Username}` 하위의 폴더명이 신규 이름으로 변경되었는지 확인 |
| 삭제 모달 안전 문구 입력 및 디랙션 기능 | 저장소 영구 삭제 | 정확한 저장소 전체 경로 타이핑 시에만 활성화되는 버튼 동작 검증 | 설정 페이지 하단 삭제 버튼 클릭 후 활성화 모달에서 다른 값을 쳐보고, 실제 전체 명칭(`{Username}/{RepoName}`)을 쳤을 때만 삭제 버튼이 활성화되는지 관찰 후 삭제 시 대시보드로 복귀 확인 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 5s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-08
