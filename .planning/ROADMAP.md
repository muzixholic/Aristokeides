# Project Roadmap

## Active Milestone: v1.5 UI 테스트 자동화 및 SSH 호환성 개선 (Awaiting Planning)

- [x] **Phase 22: bUnit 기반 Blazor 컴포넌트 단위 테스트 환경 구축**
- [x] **Phase 23: Playwright 기반 E2E UI 테스트 자동화 환경 구축 및 시나리오 테스트**
- [x] **Phase 24: SSH 호환성 개선** - C# 라이브러리 교체 또는 FxSsh 알고리즘 확장
- [x] **Phase 25: 마일스톤 v1.5 통합 검증** - 통합 검증 및 문서화

## Phases

### Phase 22: bUnit 기반 Blazor 컴포넌트 단위 테스트 환경 구축
**Goal**: bUnit 라이브러리를 사용해 Blazor UI 컴포넌트들의 단위 테스트 케이스 및 검증 구조를 설계하고 구현합니다.
**Depends on**: Nothing
**Requirements**: bUnit 컴포넌트 단위 테스트
**Success Criteria** (what must be TRUE):
  1. bUnit 패키지가 테스트 프로젝트에 추가되어야 함.
  2. 공통 UI 컴포넌트 렌더링 검증 완료.
**Plans**: 1 plan

Plans:
- [x] 22-01: bUnit 단위 테스트 구현

### Phase 23: Playwright 기반 E2E UI 테스트 자동화 환경 구축 및 시나리오 테스트
**Goal**: Playwright for .NET 기반으로 웹 인터페이스의 핵심 4대 시나리오를 통합 검증하는 자동 E2E 테스트를 구축합니다.
**Depends on**: Phase 22
**Requirements**: Playwright E2E UI 테스트
**Success Criteria** (what must be TRUE):
  1. Playwright 드라이버 연동 및 Kestrel 로컬 테스트 호스트 기동 보장.
  2. 설치 마법사, 로그인, 저장소 생성, 이슈 등록 등 핵심 4대 시나리오의 자동화 검증 완료.
**Plans**: 1 plan

Plans:
- [x] 23-01: Playwright E2E 테스트 구현

### Phase 24: SSH 호환성 개선
**Goal**: SSH 서버의 암호 알고리즘 지원 스펙을 ed25519 및 rsa-sha2로 현대화하거나, 필요시 점진적 라이브러리 교체를 수행합니다.
**Depends on**: Phase 23
**Requirements**: SSH 서버 현대적 호환성 개선
**Success Criteria** (what must be TRUE):
  1. 현대적인 SSH 키(ed25519 등)를 통한 git clone/push 연동 정상 완료.
  2. SSH 키 서버 연동성과 command bridge의 무중단 스트림 릴레이 검증 보장.
**Plans**: 1 plan

Plans:
- [x] 24-01: SSH 서버 라이브러리 교체 + 호스트 키 하이브리드 + DB 감사 로깅

### Phase 25: 마일스톤 v1.5 통합 검증 및 문서화
**Goal**: v1.5의 신규 테스트 자동화 구조 및 SSH 호환성 개선 사항을 통합 검증하고, 마일스톤 종료를 위한 문서를 정리합니다.
**Depends on**: Phase 24
**Requirements**: v1.5 통합 검증 및 문서화
**Success Criteria** (what must be TRUE):
  1. 전체 컴포넌트 및 E2E 테스트 통과 및 SSH 연결 무결성 최종 검증 완료.
  2. 마일스톤 회고 및 가이드 문서 갱신 완료.
**Plans**: 1 plan

Plans:
- [x] 25-01: 마일스톤 v1.5 통합 검증 및 문서화

## Milestones

- ✅ **v1.5 UI 테스트 자동화 및 SSH 호환성 개선** — (shipped 2026-06-11)
- ✅ **v1.4 웹훅, LFS, 조직 및 보안 기능 강화** — (shipped 2026-06-10)
- ✅ **v1.3 Deployment & Setup** — (shipped 2026-06-09)
- ✅ **v1.2 Web UI Completion** — (shipped)
- ✅ **v1.1 SSH & Advanced Code Review** — (shipped)
- ✅ **v1.0 MVP** — (shipped)

## Previous Milestones

- [v1.0 Milestone Archive](milestones/v1.0-ROADMAP.md) - Initial Release (Auth, Git Smart HTTP, Repo Browser, Issues, PRs)
- [v1.1 Milestone Archive](milestones/v1.1-ROADMAP.md) - SSH & Advanced Code Review (SSH Key Server, Commit Signature, PR Inline & Batch Review)
- [v1.2 Milestone Archive](milestones/v1.2-ROADMAP.md) - Web UI Completion (Auth, Dashboard, Repo UI, Layout)
- [v1.3 Milestone Archive](milestones/v1.3-ROADMAP.md) - Deployment & Setup (Multi-DB, Setup Wizard, Docker)
- [v1.4 Milestone Archive](milestones/v1.4-ROADMAP.md) - Webhooks, LFS, Organizations & Security Enhancements

