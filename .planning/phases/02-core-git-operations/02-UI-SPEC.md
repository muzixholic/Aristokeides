---
phase: 02
slug: core-git-operations
status: draft
shadcn_initialized: false
preset: none
created: 2026-05-29
---

# Phase 02 — UI Design Contract

> 프론트엔드 단계를 위한 시각적 및 상호작용 디자인 계약. gsd-ui-researcher에 의해 생성되고 gsd-ui-checker에 의해 검증됨.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none (프론트엔드 스택 미확정 / API 중심 단계) |
| Preset | not applicable |
| Component library | none |
| Icon library | 미정 (기본값 적용) |
| Font | 시스템 기본 폰트 (기본값 적용) |

---

## Spacing Scale

선언된 값 (반드시 4의 배수여야 함):

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | 아이콘 간격, 인라인 패딩 |
| sm | 8px | 컴팩트 요소 간격 |
| md | 16px | 기본 요소 간격 |
| lg | 24px | 섹션 패딩 |
| xl | 32px | 레이아웃 간격 |
| 2xl | 48px | 주요 섹션 구분 |
| 3xl | 64px | 페이지 수준 간격 |

Exceptions: 없음 (기본값 적용)

---

## Typography

| Role | Size | Weight | Line Height |
|------|------|--------|-------------|
| Body | 16px | 400 | 1.5 |
| Label | 14px | 400 | 1.4 |
| Heading | 24px | 600 | 1.2 |
| Display | 32px | 600 | 1.2 |

---

## Color

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | #FFFFFF | 배경, 표면 영역 (기본값) |
| Secondary (30%) | #F3F4F6 | 카드, 사이드바, 네비게이션 (기본값) |
| Accent (10%) | #3B82F6 | Primary CTA 버튼 등 지정된 요소 (기본값) |
| Destructive | #EF4444 | 파괴적(Destructive) 동작에만 사용 (기본값) |

Accent reserved for: Primary CTA 버튼, 텍스트 링크, 선택된 탭 표시자

---

## Copywriting Contract

| Element | Copy |
|---------|------|
| Primary CTA | 저장소 생성 |
| Empty state heading | 생성된 저장소가 없습니다 |
| Empty state body | 새로운 Git 저장소를 생성하여 프로젝트를 시작하세요. |
| Error state | 저장소 생성 실패: 입력하신 내용을 다시 확인해 주세요. |
| Destructive confirmation | 해당 없음 (본 단계에 파괴적 동작 없음) |

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| shadcn official | 없음 | not required |

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
