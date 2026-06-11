# Project Retrospective

## Milestone: v1.4 — 웹훅, LFS, 조직 및 보안 기능 강화

**Shipped:** 2026-06-11
**Phases:** 4 | **Plans:** 12

### What Was Built
- OAuth2 소셜 로그인 연동 (GitHub/Google) 및 TOTP 기반 2단계 인증(2FA)
- 세션 목록 조회 및 원격 로그아웃 기능을 통한 계정 보안 강화
- 조직(Organization) 및 팀(Team) 구조 구축 및 저장소 ACL 기반 상세 접근 제어
- Git LFS (Large File Storage) API 및 락(Locks) 기능 구현, 웹 UI 연동
- 저장소 이벤트 기반 웹훅(Webhooks) CRUD 및 HMAC 서명, Slack/Discord 알림 연동

### What Worked
- 마일스톤 감사 도구(`v1.4-MILESTONE-AUDIT.md`)를 통해 구현 전후의 요구사항 커버리지를 엄밀하게 검증하여 누수 없이 최종 인도할 수 있었음.
- Git LFS와 웹훅 등 외부와 직접 통신하는 다소 복잡한 백엔드 API들을 단위 테스트 및 모의 서버 테스트를 통해 성공적으로 검증함.

### What Was Inefficient
- Phase 06(이전 마일스톤)의 UAT 검증 중 커스텀 SSH 키 매핑 시 패스워드를 요구하는 클라이언트 측 동작 이슈가 지연 발견되어, 이번 마일스톤 완료 시점에 Gap Closure 형태로 추가적인 UI 설정 가이드 조치를 수행해야 했음.

### Patterns Established
- FxSsh 인증 계층과 Blazor UI 및 API의 세션을 통합 관리하기 위해 JWT와 쿠키 결합 정책을 더욱 정교화함.
- 외부 API 배달 실패 및 재시도 요구를 수렴하기 위해 백그라운드 큐와 수동 재시도 이력 타임라인 관리 구조 정립.

### Key Lessons
- 클라이언트 환경(OS, SSH 클라이언트 종류)에 따른 동작 차이를 서버 측 코드만으로 방어할 수 없을 때는, UI 측면에 명확한 문제 해결 가이드와 config 설정 가이드를 친절히 제시하는 것이 핵심적인 해결책이 됨을 배움.

### Cost Observations
- Notable: 보안과 외부 연동이 많이 섞인 만큼 E2E 플로우 검증에 시간이 소요되었으나, CLI의 자동화 검증 체계를 이용해 피드백 주기를 단축함.

---

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
