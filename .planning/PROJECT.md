# Aristokeides (Git 기반 프로젝트 관리 시스템)

## What This Is

GitLab이나 Gitea와 유사한 설치형 Git 기반 프로젝트 관리 시스템입니다. 개인적인 학습 및 새로운 기술 스택(C# / .NET) 실험을 목적으로 하며, 경량화되고 빠른 저장소 호스팅 및 협업 환경을 제공하는 것을 목표로 합니다.

## Core Value

C# / .NET 기반의 뛰어난 성능을 바탕으로, Git 저장소 호스팅, 이슈 트래커, 코드 리뷰(Pull Request) 등 협업에 필수적인 핵심 기능들을 가볍고 안정적으로 제공하는 것.

## Requirements

### Validated

<!-- Shipped and confirmed valuable. -->

- [x] (v1.0) Git 저장소 호스팅 및 기본 관리 기능 (Push/Pull, Clone 등)
- [x] (v1.0) 이슈 트래커 및 칸반 보드 (프로젝트 관리용)
- [x] (v1.0) 풀 리퀘스트(Pull Request) 및 코드 리뷰 시스템
- [x] (v1.1) SSH Git 호스팅 및 SSH 서명 푸시 검증 (FxSsh 기반)
- [x] (v1.1) PR 인라인 코멘트 및 고급 리뷰 워크플로우 (일괄 제출, 라인 보정, 병합 제한)
- [x] (v1.2) 홈페이지 및 로그인 후 대시보드 화면 구현 (Phase 11)
- [x] (v1.2) 웹 기반 회원가입, 로그인, 로그아웃 페이지 구현 (Phase 10)
- [x] (v1.2) 웹을 통한 Git 저장소 생성, 설정 관리 및 삭제 UI 추가 (Phase 12)
- [x] (v1.2) 전체 레이아웃 및 네비게이션(UI/UX) 개선 (Phase 13)
- [x] (v1.3) 다중 데이터베이스 지원 (SQLite 기본, PostgreSQL, MariaDB, MySQL)
- [x] (v1.3) 최초 실행 설치 관리자 (DB 및 관리자 계정 초기 설정)
- [x] (v1.3) 시스템 설정 화면 (이후 DB 및 설정 변경 기능)
- [x] (v1.3) Docker 및 Podman 컨테이너 배포 이미지 및 환경 구축
- [x] (v1.4) OAuth2 소셜 로그인, TOTP 기반 2단계 인증(2FA), 활성 세션 제어 구현 (Phase 18)
- [x] (v1.4) 조직(Organization) 생성 및 소속 관리, 조직 내 팀(Team) 구성, 팀별 저장소 권한 관리 구현 (Phase 19)
- [x] (v1.4) Git LFS (Large File Storage) API 구현, 로컬 바이너리 스토리지 연동 및 웹 UI 감지 (Phase 20)
- [x] (v1.4) 저장소 이벤트 기반 웹훅(Webhooks) 전송, 배달 이력 로그 및 재시도, 슬랙/디스코드 템플릿 연동 구현 (Phase 21)

### Active

<!-- Current scope. Building toward these. -->

- [ ] (v1.5) bUnit을 활용한 Blazor UI 컴포넌트 단위 테스트 구현
- [ ] (v1.5) Playwright for .NET 기반 로그인, 저장소 관리, PR 및 이슈 트래커 핵심 시나리오 E2E 테스트 자동화
- [ ] (v1.5) SSH 서버의 현대적 호환성 개선 (C# 라이브러리 교체 또는 FxSsh ed25519 및 rsa-sha2 지원 패치)

### Current State

**Shipped v1.4**: OAuth2 소셜 로그인과 TOTP 기반 2FA로 인증/보안을 강화하였으며, 조직(Organization) 및 팀 구조를 통한 세분화된 권한 관리를 도입했습니다. 또한 Git LFS API 지원으로 대용량 바이너리 저장 기능을 구축하고, 저장소 이벤트 기반 웹훅(Webhooks) 시스템(배달 이력, 슬랙/디스코드 연동 포함)을 구현 완료했습니다.
**Active v1.5**: UI 테스트 자동화(bUnit 단위 컴포넌트 테스트 및 Playwright 기반 E2E 시나리오 검증) 환경을 마련하고, SSH 서버 모듈을 현대화하여 다양한 키 및 암호 알고리즘(ed25519, rsa-sha2) 지원 범위를 넓히는 작업을 진행 중입니다.

<details>
<summary>이전 릴리즈 내역 (Shipped v1.3, v1.2, v1.1, v1.0)</summary>

**Shipped v1.3**: 다중 데이터베이스 환경 지원 및 동적 프로비저닝(Setup Wizard)을 구현하였고 관리자 설정 UI를 추가했습니다. 또한 Docker 환경에서의 손쉬운 배포를 위해 컨테이너 이미지화 및 `docker-compose.yml` 구성을 완료했습니다.

**Shipped v1.2**: 웹 사용자 인터페이스(홈페이지, 회원가입, 대시보드, 저장소 관리 등)를 완성하고, 글로벌 레이아웃과 네비게이션을 다듬어 웹 브라우저만으로도 전체 Git 기반 협업 워크플로우를 처리할 수 있게 되었습니다.

**Shipped v1.1**: FxSsh 기반 임베디드 SSH 서버와 키 관리, SSH 서명 푸시 검증을 탑재하여 완벽한 SSH Git 호스팅을 지원합니다. 또한, PR 인라인 마크다운 댓글, 일괄 리뷰 제출, 커밋 갱신 시 기존 댓글의 라인 자동 보정 및 승인 자동 리셋, 미해결 토론 존재 시 머지 엄격 차단 및 관리자 우회 등 고도화된 코드 리뷰 시스템이 완성되었습니다.

**Shipped v1.0**: 사용자 인증 및 권한(Admin/Contributor/Reader) 체계, HTTP Git Smart 호스팅, 리포지토리 파일 브라우저, 칸반 보드가 연동된 이슈 관리 및 기본 코드 리뷰/병합 시스템이 완성되었습니다.
</details>

### Out of Scope

<!-- Explicit boundaries. Includes reasoning to prevent re-adding. -->

- [CI/CD 파이프라인] — 초기 MVP의 범위를 핵심 Git 기능과 코드 리뷰에 집중하기 위해 보류
- [위키(Wiki) 및 문서화 도구] — 최소 기능 제품을 가볍게 유지하기 위해 현재 범위에서 제외
- [엔터프라이즈급 클러스터링] — 개인 학습 및 소규모 설치형을 목표로 하므로 초기 고려 대상이 아님

## Context

- C# / .NET 에코시스템을 활용하여 바닥부터 구축(Greenfield)하는 개인 프로젝트입니다.
- Git 서버와 웹 기반 프로젝트 관리 도구의 내부 동작 방식을 깊이 이해하기 위한 학습 목적을 겸하고 있습니다.
- Gitea의 효율성을 참고하여 빠르고 가벼운 설치형 시스템을 지향합니다.

## Constraints

- **[Tech Stack]**: C# / .NET — 프로젝트의 핵심 목표 중 하나가 새로운 기술 스택 학습 및 실험이기 때문.
- **[Environment]**: Self-Hosted (설치형) — 클라우드 의존성 없이 독립적으로 구동 가능해야 함.

## Key Decisions

<!-- Decisions that constrain future work. Add throughout project lifecycle. -->

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| C# / .NET 사용 | 개인적인 학습 목표 달성 및 생태계 실험 | ✓ Good |
| MVP에서 CI/CD 제외 | 핵심 저장소 호스팅 및 코드 리뷰 기능의 빠른 검증을 위해 | ✓ Good |
| Swashbuckle 7.3.2 사용 (D-01) | 10.x OpenApi v3 호환 문제 예방 | ✓ Good |
| User.Role은 string 타입 (D-02) | Admin, Contributor, Reader 등의 역할 구분 단순화 | ✓ Good |
| OwnerId 변경 (Guid -> int) (D-03) | User.Id 타입과의 불일치 해소 및 매핑 최적화 | ✓ Good |
| User.Username 추가 (D-04) | /{username}/{repo}.git 형태의 Git 경로 분석 기능 지원 | ✓ Good |
| Blazor Server(SSR) 사용 (D-05) | SPA 대비 직관적인 Git 데이터 시각화 및 개발 시간 단축 | ✓ Good |
| CDN 및 highlight.js 사용 (D-06) | 글로벌하고 경량화된 코드 구문 강조(Syntax Highlighting) 기능 제공 | ✓ Good |
| JWT + Cookies 혼합 인증 (D-07) | 백엔드 API와 프론트엔드 UI 뷰의 인증 수단 단일화 | ✓ Good |
| CreatorId Restrict 설정 (D-08) | 기존 이슈가 있는 사용자가 실수로 삭제되는 것 방지 | ✓ Good |
| AssigneeId SetNull 설정 (D-09) | 담당 사용자가 삭제될 때 이슈를 안전하게 할당 해제 | ✓ Good |
| (RepositoryId, LocalId) 복합 인덱스 (D-10) | 저장소 단위의 독립적인 이슈 번호 매김 및 고유성 보장 | ✓ Good |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-06-11 after milestone v1.4 complete*
