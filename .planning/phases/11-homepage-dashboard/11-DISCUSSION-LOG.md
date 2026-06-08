# Phase 11: Homepage & Dashboard - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-08
**Phase:** 11-homepage-dashboard
**Areas discussed:** 비로그인 랜딩 페이지 콘텐츠 전략, 대시보드 리포지토리 뷰 형식, 루트 경로(`/`) 라우팅 처리 방식

---

## 비로그인 랜딩 페이지 콘텐츠 전략

| Option | Description | Selected |
|--------|-------------|----------|
| 간결한 디자인 | 간결한 프로젝트 제목/설명과 함께 로그인 및 회원가입 버튼을 강조하는 심플한 디자인 | |
| 마케팅 섹션 | 기능(Git 호스팅, 이슈 트래커, 리뷰 등)을 시각적으로 설명하는 마케팅 섹션 포함 | ✓ |

**User's choice:** 기능(Git 호스팅, 이슈 트래커, 리뷰 등)을 시각적으로 설명하는 마케팅 섹션 포함
**Notes:** 

---

## 마케팅 섹션 시각적 요소 구성

| Option | Description | Selected |
|--------|-------------|----------|
| 아이콘 기반 | Bootstrap Icons 등 경량화된 웹 아이콘 폰트와 텍스트 설명 위주의 깔끔한 구성 | ✓ |
| 스크린샷 활용 | 시스템의 실제 동작 화면(스크린샷 등)이나 그래픽 요소를 더 많이 활용하는 구성 | |

**User's choice:** (Recommended) Bootstrap Icons 등 경량화된 웹 아이콘 폰트와 텍스트 설명 위주의 깔끔한 구성
**Notes:** 

---

## 대시보드 리포지토리 뷰 형식

| Option | Description | Selected |
|--------|-------------|----------|
| 카드형 (Grid) | 설명과 아이콘이 돋보이는 카드 형태의 그리드 뷰 (Grid/Card View) | ✓ |
| 리스트형 | 업데이트 시간, 이슈/PR 개수 등 상세 정보를 한 줄씩 명확히 보여주는 리스트 뷰 (List View) | |

**User's choice:** (Recommended) 설명과 아이콘이 돋보이는 카드 형태의 그리드 뷰 (Grid/Card View)
**Notes:** 

---

## 대시보드 카드 부가 정보 수준

| Option | Description | Selected |
|--------|-------------|----------|
| 필수 메타데이터 | 최근 업데이트 시간과 비공개 여부, 주요 언어 정도의 필수 메타데이터를 포함 | ✓ |
| 간결한 형태 | 저장소 이름과 간단한 설명, 자물쇠 아이콘(비공개 시) 정도만 보여주는 매우 간결한 형태 | |

**User's choice:** (Recommended) 최근 업데이트 시간과 비공개 여부, 주요 언어 정도의 필수 메타데이터를 포함
**Notes:** 

---

## 루트 라우팅 및 리다이렉션 로직

| Option | Description | Selected |
|--------|-------------|----------|
| 302 리다이렉트 | 로그인 상태를 체크하여 명시적인 경로(`/home` 또는 `/dashboard`)로 302 리다이렉트 처리 | ✓ |
| 내부 분기 렌더링 | URL을 `/`로 유지한 채로 서버에서 분기하여 각각 다른 뷰(랜딩/대시보드) 렌더링 | |

**User's choice:** (Recommended) 로그인 상태를 체크하여 명시적인 경로(`/home` 또는 `/dashboard`)로 302 리다이렉트 처리
**Notes:** 

---

## the agent's Discretion

- 랜딩 페이지 카피라이팅 및 색상 테마 조정
- 그리드 뷰(Grid) 반응형 처리

## Deferred Ideas

None
