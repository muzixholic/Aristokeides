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

### Active

<!-- Current scope. Building toward these. -->

*다음 마일스톤 시작 시 `/gsd-new-milestone`을 통해 새로운 개발 목표가 할당됩니다.*

### Current State

**Shipped v1.1**: FxSsh 기반 임베디드 SSH 서버와 키 관리, SSH 서명 푸시 검증을 탑재하여 완벽한 SSH Git 호스팅을 지원합니다. 또한, PR 인라인 마크다운 댓글, 일괄 리뷰 제출(Comment/Approve/Request Changes), 커밋 갱신 시 기존 댓글의 라인 자동 보정(Line Shift) 및 승인 자동 리셋, 미해결 토론 존재 시 머지 엄격 차단 및 관리자 우회 등 고도화된 코드 리뷰 시스템이 완성되었습니다.

<details>
<summary>이전 릴리즈 내역 (Shipped v1.0)</summary>

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
| C# / .NET 사용 | 개인적인 학습 목표 달성 및 생태계 실험 | — Pending |
| MVP에서 CI/CD 제외 | 핵심 저장소 호스팅 및 코드 리뷰 기능의 빠른 검증을 위해 | — Pending |

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
*Last updated: 2026-06-04 after milestone v1.1 complete*
