# Phase 05 Context: PRs & Code Review

## Domain
한 브랜치에서 다른 브랜치로 병합하기 위한 풀 리퀘스트 생성, Unified 뷰를 통한 Diff 리뷰, 그리고 Merge Commit 방식을 통한 PR 병합 기능을 구현합니다. (새로운 기능이나 범위 확장은 배제하고 HOW에 집중합니다.)

## Canonical Refs
- [.planning/ROADMAP.md](../../ROADMAP.md)
- [.planning/REQUIREMENTS.md](../../REQUIREMENTS.md)

## Decisions

### 1. PR 번호 체계 (ID Sequence)
- **Decision:** GitHub처럼 Issue와 PR이 같은 번호 대역(LocalId)을 공유합니다.
- **Context:** Phase 4에서 구현된 RepositoryId + LocalId 기반의 순차 발급 트랜잭션 로직을 재사용하여 Issue와 PR이 통합된 ID 체계를 갖도록 설계합니다.

### 2. PR 병합 전략 (Merge Strategy)
- **Decision:** 기본 Merge Commit 방식만 우선 지원합니다.
- **Context:** MVP 단계에서 복잡도를 최소화하기 위해 Squash나 Rebase 방식은 지원하지 않습니다.

### 3. Diff 뷰어 방식 (Diff Rendering)
- **Decision:** Unified View(인라인) 방식으로만 코드 변경 사항을 렌더링합니다.
- **Context:** Phase 3에서 적용한 highlight.js를 재활용하여 신택스 하이라이팅을 적용하며, 복잡한 Split View(좌우 분할)는 제외합니다.

### 4. 충돌 처리 (Conflict Handling)
- **Decision:** 웹에서는 충돌(Conflict) 발생 시 경고 메시지만 표시합니다.
- **Context:** 웹 내에서 충돌 코드를 수정하는 에디터를 제공하지 않으며, 로컬에서 충돌 해결 후 푸시하도록 안내합니다.

## Prior Decisions Carried Forward
- **[Phase 4 - D-10]** RepositoryId와 LocalId 복합 유니크 인덱스 구조 (PR에도 동일하게 적용되어야 함)
- **[Phase 3 - D-06]** highlight.js CDN을 통한 경량화된 신택스 하이라이팅 적용

## Deferred Ideas
- Squash & Merge, Rebase & Merge 지원
- Split(좌우 분할) Diff 렌더링 뷰
- 웹 상에서 코드 충돌 직접 해결 기능 (Web Conflict Editor)
- 라인별 코드 리뷰 코멘트 (Line-by-line code review, v2 요구사항)
