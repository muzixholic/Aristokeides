# Phase 9: Advanced Review Workflow - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-04
**Phase:** 09-advanced-review-workflow
**Areas discussed:** 임시 보관(Pending) 댓글 저장 방식, 라인 번호 보정(Line Shift) 작동 시점, 신규 푸시 시 승인 취소(Reset Approval) 동작 강도, 미해결 토론 병합 차단(Block Merge) 예외 정책

---

## 1. 임시 보관(Pending) 댓글 저장 방식

| Option | Description | Selected |
|--------|-------------|----------|
| Option A | DB 테이블에 IsPending(bool) 컬럼을 추가해 서버 측에 저장 (안정적인 다중 장치/세션 지원) | ✓ |
| Option B | Blazor 컴포넌트 상태 또는 세션 스토리지 등 클라이언트 측에 임시 보관 (DB 스키마 변경 불필요, 단순 구현) | |

**User's choice:** Option A
**Notes:** 브라우저 종료나 새로고침, 장치 변경 등에 영향받지 않는 안정적인 리뷰 작성을 위해 서버 영속화 방식을 채택함.

---

## 2. 라인 번호 보정(Line Shift) 및 Outdated 판단 연산의 작동 시점

| Option | Description | Selected |
|--------|-------------|----------|
| Option A | Git Push 완료 직후 백그라운드 서비스에서 즉시 연산하여 DB의 라인 번호를 보정하고 Outdated 여부를 갱신 (PR 페이지 로딩 속도 최적화) | ✓ |
| Option B | 사용자가 PR 페이지를 조회할 때 실시간으로 Git Diff를 분석하여 라인 보정값 및 Outdated 여부를 온디맨드로 계산 (DB 쓰기 없음, 구현 단순화) | |

**User's choice:** Option A
**Notes:** PR 페이지 조회 시 사용자 경험을 해치지 않고 빠른 로딩을 위해 Git Push 즉시 비동기로 백그라운드에서 미리 보정 및 상태를 계산하도록 결정함.

---

## 3. 신규 커밋 푸시 시 기존 승인 취소(Reset Approval)의 동작 강도

| Option | Description | Selected |
|--------|-------------|----------|
| Option A | 신규 커밋이 푸시되면 기존의 모든 Approve(승인) 상태를 자동으로 취소(초기화)하여 재승인을 강제 (안전성 최우선) | ✓ |
| Option B | 기존 승인 상태를 강제로 취소하지 않고 유지하되, UI 상에 '승인 이후 신규 커밋 추가됨' 안내 배지를 표시하여 수동 검토 유도 (유연성 우선) | |

**User's choice:** Option A
**Notes:** 승인 이후 악의적이거나 예상치 못한 코드가 추가되는 상황을 원천 방지하여 엄격한 품질 게이트를 구축하고자 초기화 방식을 채택함.

---

## 4. 미해결 토론이 존재할 때 병합 차단(Block Merge) 규칙의 강도를 어떻게 설정할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| Option A | 예외 없이 미해결 스레드가 하나라도 있으면 모든 사용자의 병합(Merge)을 전면 차단 (완벽한 코드 리뷰 보장) | |
| Option B | 미해결 스레드가 있더라도 관리자(Admin) 권한을 가진 사용자에게는 '강제 병합(Force Merge)' 권한을 부여 (비상 상황 대비 우회로 제공) | ✓ |

**User's choice:** Option B
**Notes:** 코드 안전성을 최우선으로 하되 긴급 패치 등 비상 상황에서의 시스템 병목을 피하기 위해 관리자 권한을 가진 사용자에게 강제 병합 권한을 예외적으로 부여하기로 함.

---

## the agent's Discretion

- 라인 번호 보정 및 Outdated 처리를 판단하기 위해 Myers Diff 알고리즘을 확장하거나 Git Diff 내역에서 Hunk 범위를 매핑하는 구체적인 수식/함수 구현 방식은 에이전트의 판단에 따라 구현함.
- UI 상의 임시 댓글(Pending)의 시각적 형태 및 리뷰 시작/일괄 제출 화면 디자인 구성은 최적의 UX 패턴을 연구하여 적용함.

## Deferred Ideas

- None — 모든 논의 항목은 Phase 9의 설계 범위 내에서 명확하게 조율되었습니다.

---

*Phase: 09-advanced-review-workflow*
*Discussion log generated: 2026-06-04*
