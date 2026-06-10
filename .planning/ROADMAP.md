# Project Roadmap

## Milestones

- ✅ **v1.0 MVP** — (shipped)
- ✅ **v1.1 SSH & Advanced Code Review** — (shipped)
- ✅ **v1.2 Web UI Completion** — (shipped)
- ✅ **v1.3 Deployment & Setup** — (shipped 2026-06-09)
- ✅ **v1.4 웹훅, LFS, 조직 및 보안 기능 강화** — Phases 18-21 (shipped 2026-06-10)

## Phases

### Phase 18: 보안 및 인증 기능 강화
- [x] OAuth2 소셜 로그인 연동 (GitHub, Google 등)
- [x] TOTP 기반 2단계 인증(2FA) 활성화 및 다단계 인증 로그인 흐름 구현
- [x] 사용자별 활성 세션 목록 조회 및 세션 만료, 특정 기기 원격 로그아웃 구현

### Phase 19: 조직 및 팀 기능
- [x] 조직(Organization) 엔티티 정의, 생성 및 프로필 설정 UI 추가
- [x] 조직 내 팀(Team) 생성 및 사용자 추가/제거 기능 구현
- [x] 조직 저장소에 대한 팀/개별 사용자 권한 설정 및 접근 제어(Read/Write/Admin) 적용

### Phase 20: Git LFS (Large File Storage) 지원
- [x] Git LFS API 규격(LFS Batch API, Locks API) 엔드포인트 구현
- [x] LFS 포인터 처리 및 대용량 바이너리 파일을 보관하는 로컬 스토리지 백엔드 연동
- [x] LFS 파일 업로드/다운로드 인증 구현 및 웹 UI에서 LFS 파일 감지/다운로드 처리

### Phase 21: 웹훅 및 외부 연동
- [x] 저장소 이벤트 발생 시 웹훅 페이로드 전송 및 개별 저장소 웹훅 관리 기능 구현
- [x] 웹훅 전송 상세 기록(Delivery Log) 관리 및 수동 재전송(Redelivery) 기능 구현
- [x] 슬랙(Slack), 디스코드(Discord) 템플릿 연동 및 알림 기능 확장

## Previous Milestones

- [v1.0 Milestone Archive](milestones/v1.0-ROADMAP.md) - Initial Release (Auth, Git Smart HTTP, Repo Browser, Issues, PRs)
- [v1.1 Milestone Archive](milestones/v1.1-ROADMAP.md) - SSH & Advanced Code Review (SSH Key Server, Commit Signature, PR Inline & Batch Review)
- [v1.2 Milestone Archive](milestones/v1.2-ROADMAP.md) - Web UI Completion (Auth, Dashboard, Repo UI, Layout)
- [v1.3 Milestone Archive](milestones/v1.3-ROADMAP.md) - Deployment & Setup (Multi-DB, Setup Wizard, Docker)
