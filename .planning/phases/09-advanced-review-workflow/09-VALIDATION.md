---
phase: 09
slug: advanced-review-workflow
status: partial
nyquist_compliant: false
wave_0_complete: true
created: 2026-06-05
---

# Phase 9 — Validation Strategy

> Per-phase validation contract. **State B** — VALIDATION.md가 없어 PLAN/SUMMARY 아티팩트로부터 재구성함.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (dotnet test) |
| **Config file** | `Aristokeides.Tests/Aristokeides.Tests.csproj` |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~AdvancedReviewTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~15–30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~AdvancedReviewTests"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 09-01-T1 | 01 | 1 | CODE-05 | — | PullRequestReviewComment에 IsPending/IsOutdated 필드 추가; DB 마이그레이션 성공 | build | `dotnet build` | ✅ | ✅ green |
| 09-01-T2 | 01 | 1 | CODE-05, CODE-09, CODE-10 | T-09-01 | SubmitReviewAsync — Pending 댓글 일괄 활성화 + 리뷰 생성; MergePullRequestAsync — 미해결 토론 차단 + Admin forceMerge 우회 | unit | `dotnet test --filter "FullyQualifiedName~AdvancedReviewTests"` | ✅ | ✅ green |
| 09-01-T3 | 01 | 1 | CODE-07, CODE-11 | T-09-02 | OnBranchPushedAsync — Line Shift 보정 + Outdated 전환 + Approved→Dismissed 리셋; HTTP/SSH Push 연동 | unit+integration | `dotnet test --filter "FullyQualifiedName~AdvancedReviewTests"` | ✅ | ✅ green |
| 09-02-T1 | 02 | 2 | CODE-05 | — | \"Start a review\" / \"Submit review\" UI 배너 및 Pending 배지 렌더링 | human-check | (manual) | ✅ | ⚠️ manual-only |
| 09-02-T2 | 02 | 2 | CODE-09, CODE-10 | T-09-03 | 미해결 토론 시 Merge 버튼 비활성화 + Admin 전용 Force Merge 체크박스 노출 및 권한 재확인 | human-check | (manual) | ✅ | ⚠️ manual-only |
| 09-02-T3 | 02 | 2 | CODE-07, CODE-11 | — | Outdated 댓글 아코디언 접기 렌더링 + Dismissed 승인 타임라인 알림 표기 | human-check | (manual) | ✅ | ⚠️ manual-only |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ manual-only*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

- `Aristokeides.Tests/Services/AdvancedReviewTests.cs` — Phase 9 핵심 백엔드 검증 테스트 파일 (3개 xUnit 테스트 포함)
- xUnit 프레임워크는 기존 테스트 프로젝트에 이미 설치되어 있음

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| \"Start a review\" / \"Add review comment\" / \"Submit review\" 버튼 동작 및 Pending 배지 표시 | CODE-05 | Blazor Server SSR UI — 헤드리스 자동화 불가 | 로컬 서버 기동 후 PR Diff 창에서 \"+\" 클릭 → \"Start a review\"로 댓글 생성 → Pending 배지 확인 → Submit 패널에서 Approve 제출 → 배지 해제 및 승인 이력 확인 |
| 미해결 토론 시 Merge 버튼 비활성화 (일반 사용자) | CODE-09 | Blazor Server SSR UI — 역할 기반 렌더링 조건 브라우저 필요 | 미해결 댓글 있는 PR에서 Contributor 계정으로 병합 버튼 disabled 상태 + 경고 배너 확인 |
| Admin 전용 Force Merge 체크박스 노출 및 강제 병합 성공 | CODE-10 | Blazor Server SSR UI + Admin 역할 조건 분기 | Admin 계정 전환 → 강제 병합 체크박스 노출 확인 → 체크 시 \"Force merge pull request\" 버튼 활성화 → 실제 병합 완료 검증 |
| Outdated 댓글 아코디언 접기 + 클릭 펼치기 | CODE-07 | Blazor Server SSR 렌더링 조건 — 실시간 Push 이벤트 필요 | 기존 댓글이 달린 브랜치에 신규 커밋 Push → PR 새로고침 → \"Outdated conversation\" 배너 확인 → 클릭 시 펼쳐짐 확인 |
| Approved → Dismissed 이력 타임라인 표기 | CODE-11 | 실시간 Push 이벤트 기반 UI 상태 변화 | 승인 리뷰가 있는 PR의 소스 브랜치에 신규 커밋 Push → 히스토리 타임라인에 Dismissed 알림 아이콘+메시지 렌더링 확인 |

---

## Gap Analysis Summary (재구성 시 기준)

| 요구사항 | 자동화 커버리지 | 수동 검증 |
|---------|--------------|---------|
| CODE-05 (리뷰 일괄 제출) | ✅ `SubmitReviewAsync_ActivatesPendingComments_AndCreatesReview` | ⚠️ UI 배지/배너 수동 필요 |
| CODE-07 (Line Shift / Outdated) | ✅ `OnBranchPushedAsync_AppliesLineShift_AndOutdatedState_AndResetsApprovals` | ⚠️ UI 아코디언 수동 필요 |
| CODE-09 (병합 차단) | ✅ `MergePullRequestAsync_Enforces_MergeSafetyChecks` (Assert 1) | ⚠️ UI 버튼 비활성화 수동 필요 |
| CODE-10 (관리자 우회) | ✅ `MergePullRequestAsync_Enforces_MergeSafetyChecks` (Assert 2,3) | ⚠️ UI 체크박스 수동 필요 |
| CODE-11 (Dismissed 리셋) | ✅ `OnBranchPushedAsync_AppliesLineShift_AndOutdatedState_AndResetsApprovals` | ⚠️ UI 타임라인 수동 필요 |

**자동화율: 3/6 태스크 (백엔드 100% COVERED, UI 100% MANUAL-ONLY)**

---

## Validation Sign-Off

- [x] 모든 백엔드 태스크에 `<automated>` verify 명령 존재
- [x] Wave 1 태스크 간 3개 연속 비자동화 없음 (모두 자동화)
- [x] Wave 0 불필요 (기존 인프라 사용)
- [x] watch-mode 플래그 없음
- [x] Feedback latency < 30s
- [ ] `nyquist_compliant: true` — UI 태스크 수동 검증이 manual-only로 분류되어 부분 준수 상태

**Approval:** partial 2026-06-05 (백엔드 완전 자동화, UI Wave 2는 Blazor SSR 특성상 manual-only 영구 분류)

---

## Validation Audit 2026-06-05

| Metric | Count |
|--------|-------|
| Gaps found | 0 (백엔드) / 5 (UI — Blazor human-check 게이트) |
| Resolved (automated) | 3 태스크 (Plan01 전체) |
| Escalated to manual-only | 3 태스크 (Plan02 전체 — Blazor SSR UI) |
