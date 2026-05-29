# Phase 3: Repository Browsing - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-29
**Phase:** 3-Repository Browsing
**Areas discussed:** 프론트엔드 구조, 커밋 내역(History) 로딩 방식, 파일 내용 뷰어 및 문법 강조(Syntax Highlighting)

---

## 프론트엔드 구조

| Option | Description | Selected |
|--------|-------------|----------|
| 순수 HTML/CSS/Vanilla JS | 순수 HTML/CSS/Vanilla JS와 C# API 통신으로 구축 | |
| ASP.NET Core MVC | ASP.NET Core MVC (Razor 뷰) 방식 사용 | |
| 프론트엔드 프레임워크 | React (Vite) 등 별도의 SPA 프론트엔드 프레임워크 사용 | |
| Blazor Web App | 사용자가 직접 제안한 커스텀 답변 | ✓ |

**User's choice:** Blazor Web App
**Notes:** C# 생태계와 완벽히 통합하기 위한 선택.

---

## 커밋 내역(History) 로딩 방식

| Option | Description | Selected |
|--------|-------------|----------|
| 무한 스크롤 | 사용자가 아래로 스크롤할 때 자동으로 다음 커밋 로드 | |
| 전통적인 페이지네이션 | 이전/다음 페이지 버튼 혹은 페이지 번호 클릭 | ✓ |
| Load More 버튼 | 수동으로 '더 보기' 버튼 클릭 시 추가 로드 | |

**User's choice:** 전통적인 페이지네이션 (이전/다음 페이지 버튼 혹은 페이지 번호 클릭)
**Notes:** 무한 스크롤 대신 안정적이고 전통적인 페이징 방식으로 구현 요청.

---

## 파일 내용 뷰어 및 문법 강조(Syntax Highlighting)

| Option | Description | Selected |
|--------|-------------|----------|
| JSInterop 라이브러리 | 가볍고 빠른 정적 하이라이팅 (Prism.js 또는 Highlight.js) | |
| Monaco Editor | 고급 에디터 경험 (Monaco Editor의 Read-only 모드 사용) | |
| 기본 제공 컴포넌트 | 기본 제공되는 Blazor 코드 뷰어 컴포넌트 사용 | ✓ |

**User's choice:** 기본 제공되는 Blazor 코드 뷰어 컴포넌트 사용 (구현이 가장 간단함)
**Notes:** 가장 복잡하지 않고 구현이 쉬운 단순한 컴포넌트 선호.

---

## the agent's Discretion

None

## Deferred Ideas

None
