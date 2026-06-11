# Project Retrospective

## Milestone: v1.5 — UI 테스트 자동화 및 SSH 호환성 개선

**Shipped:** 2026-06-11
**Phases:** 2 | **Plans:** 2 (v1.5 기준 계획 및 검증)

### What Was Built
- **Blazor UI 단위 테스트 자동화**: bUnit 프레임워크를 도입하여 개별 Blazor 컴포넌트의 렌더링 무결성 및 이벤트 처리 검증.
- **Playwright 브라우저 E2E 테스트 자동화**: 실제 크로미움 브라우저 기반으로 로그인, 저장소 생성, 이슈 트래킹 등의 전체 워크플로우를 테스트하는 E2E 환경 구축.
- **SSH 서버 라이브러리 교체**: FxSsh에서 최신 현대적 SSH 라이브러리인 `DevTunnels.Ssh`로 마이그레이션하여 `ed25519`, `rsa-sha2-256`, `rsa-sha2-512` 알고리즘 완벽 지원.
- **SSH 감사 로깅 시스템**: SSH 연결의 인증 시도(성공/실패) 이력을 데이터베이스 `SshAuthLog` 테이블에 기록하고 이를 웹 UI 관리자 페이지에서 모니터링할 수 있는 대시보드 구축.
- **설치 및 테스트 가이드**: 프로젝트 루트에 `TESTING.md`를 신설하고 `README.md`에 링크를 연결하여 개발자 편의성 강화.

### What Worked
- `PlaywrightHostHelper`를 통해 SQLite 메모리/파일 DB 및 Kestrel 포트 5002를 완벽하게 격리하여 병렬적이고 재현 가능한 E2E 테스트 수행 가능.
- `DevTunnels.Ssh` 적용으로 기존 클라이언트 환경에서 호스트 키 검증 실패 및 비밀번호 상시 요구 문제를 깔끔하게 해결하였고 보안 규격을 강화함.
- bUnit과 Playwright의 역할 분담을 통해 UI 변경 사항 및 복잡한 웹 시나리오를 효과적으로 방어할 수 있었음.

### What Was Inefficient
- Playwright 구동을 위해 드라이버를 내려받고 Kestrel 서버 구동 대기 시간(3초)이 각 테스트 클래스마다 누적되어, 단위 테스트 세트 대비 전체 검증 소요 시간이 증가함.
- `DevTunnels.Ssh` 라이브러리의 비동기 스트림 중계 및 일반 셸 세션 차단 기전을 커스텀 구현하는 과정에서 스트림 디스포즈 타이밍 이슈 등으로 디버깅 시간이 지연됨.

### Patterns Established
- `PlaywrightHostHelper`를 통한 런타임 호스트 인메모리/격리 DB 바인딩 및 환경변수 일시 변경 패턴 정착.
- SSH 접속 및 키 검증 단계에서 발생하는 로그를 EF Core 엔티티(`SshAuthLog`)와 결합해 백그라운드로 안전하게 플러시하는 비동기 감사 로깅 패턴 수립.

### Key Lessons
- 시스템 레벨의 백포트 인프라(Kestrel, SSH)는 테스트 환경에서의 리소스(DB 파일명, TCP 포트) 격리 설정을 치밀하게 고려하지 않으면 flaky 테스트를 유발하기 쉬우므로 초기 인프라 설계부터 이를 반드시 반영해야 한다는 점을 체득함.

### Cost Observations
- Notable: SSH 교체 및 브라우저 드라이버 컴파일 등 초반 환경 구성 비용이 다소 높았으나, 테스트 셋 구축 이후 104개 전체 테스트 통과를 통해 수동 검증 시간을 거의 제로로 줄여 장기적인 리그레션 방어 장벽을 공고히 함.

---

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
