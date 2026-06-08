# Project Roadmap

## Previous Milestones

- [v1.0 Milestone Archive](milestones/v1.0-ROADMAP.md) - Initial Release (Auth, Git Smart HTTP, Repo Browser, Issues, PRs)
- [v1.1 Milestone Archive](milestones/v1.1-ROADMAP.md) - SSH & Advanced Code Review (SSH Key Server, Commit Signature, PR Inline & Batch Review)

## Current Milestone: v1.2 - Web UI Completion

이전 마일스톤에서 확보한 백엔드 핵심 기능을 바탕으로, 웹 사용자 인터페이스(홈페이지, 회원가입, 저장소 관리 등)의 빈 부분을 채우고 완성도를 높입니다.

- [x] **Phase 10: User Authentication UI**
  - 회원가입, 로그인, 로그아웃 웹 페이지 템플릿 및 동작 구현
- [x] **Phase 11: Homepage & Dashboard**
  - 비로그인 시 프로젝트 랜딩 페이지 구현
  - 로그인 시 내 리포지토리 목록 뷰(Dashboard) 구현
- [x] **Phase 12: Repository Management UI**
  - 신규 저장소 생성 폼(Create Repository) 구현
  - 저장소 설정 관리 및 삭제 UI 추가
- [ ] **Phase 13: Layout & Navigation Polish**
  - 글로벌 네비게이션 바, 푸터, 레이아웃 다듬기 및 일관성 강화

### Phase 11: Homepage & Dashboard
**Goal:** 비로그인 시 랜딩 페이지와 로그인 시의 대시보드를 구축하여 초기 진입 경험 제공
- 비로그인 사용자를 위한 프로젝트 소개(Landing Page) 구현
- 로그인 사용자를 위한 자신이 접근 가능한 리포지토리 목록(Dashboard) 구현
- 루트 경로(`/`) 라우팅 처리 수정 (404 문제 해결)

### Phase 12: Repository Management UI
**Goal:** 저장소 생성 및 설정 관리를 위한 웹 화면 구축
- 새 저장소 생성 폼 구현
- 저장소 기본 설정(이름, 설명, 가시성 등) 변경 페이지 구현
- 저장소 삭제 기능 및 UI 추가

### Phase 13: Layout & Navigation Polish
**Goal:** 프로젝트 전반적인 레이아웃 다듬기 및 완성도 증진
- 글로벌 네비게이션 바 링크 구성 및 활성화 상태 표시
- 푸터(Footer) 추가
- UI 통일성 확보 및 반응형 레이아웃 점검
