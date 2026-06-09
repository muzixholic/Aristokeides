# Project Retrospective

## Milestone: v1.3 — 배포를 위한 작업 (Deployment & Setup)

**Shipped:** 2026-06-09
**Phases:** 4 | **Plans:** 4

### What Was Built
- 멀티 데이터베이스 지원 기반 마련
- 최초 설치 관리자 (Setup Wizard) 구현
- 관리자 설정 화면 추가
- Docker/Podman 배포 환경 구축

### What Worked
- 마일스톤 단계에서 요구사항(DB, Docker, Setup)이 명확히 분리되어 있어 효율적인 병렬 처리가 가능했음.

### What Was Inefficient
- 개발 중 각 Phase마다 `SUMMARY.md` 작성을 누락하여 마일스톤 완료 시 요약 데이터를 수동으로 구성해야 했음.

### Patterns Established
- 다중 데이터베이스 지원을 위해 `appsettings.json`과 의존성 주입(DI)을 활용해 런타임에 동적으로 Provider를 결정하는 패턴 정립.

### Key Lessons
- E2E 테스트(유닛 테스트 포함 51개)가 든든하게 받쳐주었기 때문에 DB 인프라 구조를 변경하면서도 안정성을 보장할 수 있었음.

### Cost Observations
- Notable: 인프라성 작업 특성상 기능보다는 구조 변경이 많았으며, 테스트 주도 개발(TDD)이 큰 도움이 됨.
