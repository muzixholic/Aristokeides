---
phase: 8
slug: pr-inline-comments
status: draft
shadcn_initialized: false
preset: none
created: 2026-06-04
---

# Phase 8 — UI Design Contract

> Pull Request 파일 변경 Diff 화면에서의 인라인 댓글 작성, 저장 및 대화 스레드화를 위한 시각적 및 상호작용 계약 문서입니다. 이 문서는 gsd-ui-researcher에 의해 생성되었습니다.

---

## Design System

| Property | Value |
|----------|-------|
| Tool | none |
| Preset | not applicable |
| Component library | none |
| Icon library | SVG inline icons |
| Font | system-ui, -apple-system, sans-serif |

### Visual Hierarchy & Focal Point

- **Focal Point (초점):** 코드 라인 옆에 표시되는 호버 상태의 `+` 버튼과 현재 작성 중인 active 상태의 댓글 입력창이 화면의 중심 초점입니다.
- **Visual Hierarchy (시각적 계층 구조):**
  1. **1순위 (가장 높은 시각적 강도):** active 상태의 댓글 입력창 및 주요 CTA 버튼 ("Add single comment")
  2. **2순위 (중간 시각적 강도):** 등록된 댓글 카드의 내용 및 스레드 대화 영역
  3. **3순위 (가장 낮은 시각적 강도):** 작성자 이름, 작성 시간, 라인 번호 등 메타데이터와 보조 액션 버튼 ("Cancel editing", "Discard comment", "Resolve")

---

## Spacing Scale

선언된 간격 값 (모두 4의 배수여야 함):

| Token | Value | Usage |
|-------|-------|-------|
| xs | 4px | 아이콘 간격, 인라인 패딩, 배지 내부 상하 여백 |
| sm | 8px | 입력창 내부 패딩, 버튼 간격, 라인 번호 셀 여백, 답글 목록 상하 간격 |
| md | 16px | 댓글 카드 패딩, 컴포넌트 간 여백, 대화 스레드 들여쓰기 |
| lg | 24px | Diff 뷰어 상하 패딩, 메인 컨테이너 좌우 여백 |
| xl | 32px | 스레드 그룹 간 여백, 페이지 상단 여백 |
| 2xl | 48px | 화면 하단 페이지 푸터 여백, 큰 섹션 구분선 여백 |
| 3xl | 64px | 해당 없음 (미사용) |

예외 사항:
- `+` 버튼의 크기: `20px` x `20px` (정사각형 터겟 확보 및 코드 라인 높이 정렬을 위함)
  - *접근성(Accessibility) 요구사항*: 아이콘 단독으로 동작하는 `+` 버튼은 스크린 리더 지원을 위해 `aria-label="Add comment"` 대체 텍스트를 필수적으로 정의해야 합니다.
- 변경 내역 테이블 라인 번호 열 너비: `48px` (고정, 4의 배수 규격 준수)

---

## Typography

| Role | Size | Weight | Line Height |
|------|------|--------|-------------|
| Body | 14px | 400 (Regular) | 1.5 |
| Label | 12px | 600 (Semibold) | 1.2 |
| Heading | 20px | 600 (Semibold) | 1.2 |
| Monospace / Code | 12px | 400 (Regular) | 1.5 |

*주: 폰트 웨이트는 일관성을 위해 `400 (Regular)`과 `600 (Semibold)`의 2가지로 제한합니다.*

---

## Color

| Role | Value | Usage |
|------|-------|-------|
| Dominant (60%) | `#FFFFFF` | 페이지 전체 배경, Diff 테이블 미변경 코드 영역 배경 |
| Secondary (30%) | `#F3F4F6` | 네비게이션 바, 사이드바, 댓글 입력창 헤더, 해결된(Resolved) 스레드 접기 헤더 |
| Accent (10%) | `#2563EB` | 호버 시 나타나는 `+` 버튼배경, Primary CTA 버튼, 활성화된 Write/Preview 탭 |
| Destructive | `#EF4444` | 오류 메시지 텍스트, 댓글 삭제 버튼 |

### Semantic Colors for Diff
- **Diff 추가 라인 (`+`):** 배경색 `#ECFDF5` (연한 녹색), 텍스트 `#065F46`
- **Diff 삭제 라인 (`-`):** 배경색 `#FEF2F2` (연한 적색), 텍스트 `#991B1B`
- **대화 스레드 박스 배경:** 배경색 `#F9FAFB`, 테두리 `#E5E7EB`

Accent reserved for:
- 코드 행 호버 시 노출되는 `+` 버튼 아이콘 및 배경
- "Add single comment" 버튼 (Primary CTA)
- "Resolve conversation" 및 "Unresolve conversation" 텍스트 링크 및 버튼
- 에디터 상단 "Write" 및 "Preview" 탭의 활성 상태 언더라인

---

## Copywriting Contract

| Element | Copy |
|---------|------|
| Primary CTA | "Add single comment" (댓글 추가) / "Add reply" (답글 추가) |
| Empty state heading | "No conversations yet" |
| Empty state body | "Hover over a line of code to start a discussion." |
| Error state | "Failed to save comment. Please check your connection and try again." |
| Destructive confirmation | "Delete comment": "Are you sure you want to delete this comment? This action cannot be undone." |
| Action labels | "Cancel editing" (편집 취소), "Discard comment" (댓글 작성 취소), "Resolve conversation" (해결됨 표시), "Unresolve conversation" (해결 안 됨으로 변경) |

---

## Registry Safety

| Registry | Blocks Used | Safety Gate |
|----------|-------------|-------------|
| shadcn official | none | not required |

---

## Checker Sign-Off

- [ ] Dimension 1 Copywriting: PASS
- [ ] Dimension 2 Visuals: PASS
- [ ] Dimension 3 Color: PASS
- [ ] Dimension 4 Typography: PASS
- [ ] Dimension 5 Spacing: PASS
- [ ] Dimension 6 Registry Safety: PASS

**Approval:** pending
