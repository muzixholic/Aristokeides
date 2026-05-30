# Phase 05 Discussion Log

> **Note:** This log is for human reference only (audits, retrospectives). Downstream agents consume `05-CONTEXT.md`.

## 1. PR 번호 체계 (ID Sequence)
- **Options Presented:** 
  - GitHub처럼 Issue와 PR이 같은 번호 대역을 공유
  - Issue와 완전히 독립적인 번호 대역 사용
- **Selected:** GitHub처럼 Issue와 PR이 같은 번호 대역을 공유 (추천)
- **Notes:** Phase 4에서 구성한 RepositoryId+LocalId 기반 발급 시스템을 Issue와 PR이 공유할 수 있도록 구조화하기로 결정됨.

## 2. PR 병합 전략 (Merge Strategy)
- **Options Presented:** 
  - 가장 기본인 Merge Commit만 먼저 지원 (MVP 단순화)
  - Merge Commit, Squash, Rebase 모두 지원
- **Selected:** 가장 기본인 Merge Commit만 먼저 지원 (MVP 단순화)
- **Notes:** 

## 3. Diff 뷰어 방식 (Diff Rendering)
- **Options Presented:** 
  - Unified(인라인) View만 우선 지원 (개발 복잡도 최소화)
  - Unified(인라인) 및 Split(좌우 분할) View 모두 지원
- **Selected:** Unified(인라인) View만 우선 지원 (개발 복잡도 최소화)
- **Notes:** 

## 4. 충돌 처리 (Conflict Handling)
- **Options Presented:** 
  - 웹에서는 병합 충돌 경고만 표시하고 로컬 해결 후 푸시 유도 (MVP 최소화)
  - 웹에서 텍스트 에디터를 통해 충돌을 직접 해결할 수 있는 기능 제공
- **Selected:** 웹에서는 병합 충돌 경고만 표시하고 로컬 해결 후 푸시 유도 (MVP 최소화)
- **Notes:** 
