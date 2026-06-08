# Phase 13: Layout & Navigation Polish - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-08
**Phase:** 13-Layout-Navigation-Polish
**Areas discussed:** 글로벌 네비게이션 바 링크 구성, 네비게이션 활성화 상태 표시 스타일, 푸터(Footer) 구성 및 포함할 링크 범위, 반응형 모바일 대응 방식

---

## 1. 글로벌 네비게이션 바 링크 구성

| Option | Description | Selected |
|--------|-------------|----------|
| 대시보드 및 새 저장소 링크 배치 | 로그인 시 로고 옆에 '대시보드' 링크 배치, 우측 영역에는 '새 저장소' 버튼/링크 상시 노출 | ✓ |
| 심플 로고 링크 | 로고 외에 추가 링크를 두지 않고, 로고 클릭 리다이렉트에만 의존 | |

**User's choice:** 대시보드 및 새 저장소 링크 배치
**Notes:** 로그인 상태에서 주요 기능으로의 빠른 접근을 위해 대시보드 링크와 새 저장소 생성 버튼을 상시 노출하는 방안을 채택함.

---

## 2. 네비게이션 활성화 상태 표시 스타일

| Option | Description | Selected |
|--------|-------------|----------|
| Accent 글자색 + 밑줄 | 글자색을 Accent(#2563EB)로 변경하고 하단에 2px 굵기 밑줄 활성화 | ✓ |
| Accent 글자색만 변경 | 글자색만 Accent 컬러로 변경 | |
| 배경 캡슐 하이라이트 | 가벼운 배경 캡슐 형태의 백그라운드 칩 효과 적용 | |

**User's choice:** Accent 글자색 + 밑줄
**Notes:** 현재 활성화된 화면 상태를 가장 뚜렷하고 직관적으로 보여주기 위해 밑줄과 글자색 변화를 함께 사용하기로 함.

---

## 3. 푸터(Footer) 구성 및 포함할 링크 범위

| Option | Description | Selected |
|--------|-------------|----------|
| 하이브리드 푸터 | 저작권 표시와 함께 Swagger API 문서(/swagger) 및 주요 바로가기 링크 포함 | ✓ |
| 최소형 푸터 | 텍스트 저작권("© 2026 Aristokeides") 정보만 한 줄로 최소화 노출 | |

**User's choice:** 하이브리드 푸터
**Notes:** 저작권 문구만 노출하는 대신 개발자가 신속하게 스웨거 API 문서나 주요 경로에 접속할 수 있도록 하이브리드 형태의 링크 구조를 도입함.

---

## 4. 반응형 모바일 대응 방식

| Option | Description | Selected |
|--------|-------------|----------|
| Flex Wrap 반응형 레이아웃 | CSS Flex-Wrap을 이용해 좁은 화면에서 개행 및 콤팩트 여백 처리 (JS 의존성 배제) | ✓ |
| 토글형 햄버거 메뉴 | 모바일 화면에서 숨김 처리 후 클릭 시 메뉴 펼침/토글형 구현 | |

**User's choice:** Flex Wrap 반응형 레이아웃
**Notes:** Blazor Server SSR 렌더링에 적합하고 가벼운 CSS 기반의 Flex Wrap 레이아웃을 통해 화면 너비에 맞게 자연스럽게 개행 및 축소 처리하기로 결정함.

---

## the agent's Discretion

- 네비게이션 바와 푸터의 테두리 선 굵기 및 그림자(Shadow) 적용 여부
- 브라우저 크기에 따른 패딩(Padding) 및 여백(Margin)의 미세 조정 방식
- 활성화 상태를 감지하기 위해 Blazor `NavLink` 컴포넌트의 `Match="NavLinkMatch.Prefix"` 또는 라우트 주소 매칭 로직의 상세 구현

---

## Deferred Ideas

- 모바일 전용 토글형 슬라이드 햄버거 메뉴
