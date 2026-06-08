---
title: "MainLayout 네비게이션 업데이트"
phase: 10
wave: 2
depends_on:
  - 10A-PLAN
  - 10B-PLAN
files_modified:
  - Aristokeides.Api/Components/MainLayout.razor
autonomous: true
requirements:
  - "새로운 사용자는 웹 브라우저를 통해 직관적으로 회원가입을 하고 로그인/로그아웃할 수 있어야 한다."
---

# Plan 10C: MainLayout 네비게이션 업데이트

## Objective

`MainLayout.razor`의 네비게이션 바를 수정하여 회원가입 링크를 추가하고, Logout 링크의 404 버그를 수정하며, 네비게이션 텍스트를 한국어로 번역한다.

## Tasks

<task id="10C-1">
<title>MainLayout.razor 네비게이션 업데이트</title>
<read_first>
- `Aristokeides.Api/Components/MainLayout.razor` — 현재 네비게이션 구조 (L1-24)
- `Aristokeides.Api/Controllers/AuthController.cs` L88-93 — Logout 엔드포인트 경로 확인 (`GET /api/auth/logout`)
</read_first>
<action>
`MainLayout.razor`를 다음과 같이 수정한다.

**1. Logout 링크 URL 수정 (버그 픽스):**

현재 (L11):
```html
<a href="/logout" style="margin-left: 16px;">Logout</a>
```

변경:
```html
<a href="/api/auth/logout" style="margin-left: 16px;">로그아웃</a>
```

- `/logout`은 대응하는 라우트가 없어 404 발생 (10-RESEARCH.md §4 참조)
- 실제 Logout API는 `GET /api/auth/logout` (AuthController.cs L88)

**2. NotAuthorized 섹션에 Register 링크 추가:**

현재 (L13-15):
```html
<NotAuthorized>
    <a href="/login">Login</a>
</NotAuthorized>
```

변경:
```html
<NotAuthorized>
    <a href="/login">로그인</a>
    <a href="/register" style="margin-left: 16px;">회원가입</a>
</NotAuthorized>
```

**3. 네비게이션 텍스트 한국어 번역:**

| 위치 | 기존 | 변경 |
|---|---|---|
| L9 (Authorized) | `Hello, @context.User.Identity?.Name!` | `@context.User.Identity?.Name 님` |
| L10 (Authorized) | `Settings` | `설정` |
| L11 (Authorized) | `Logout` | `로그아웃` |
| L14 (NotAuthorized) | `Login` | `로그인` |

**최종 MainLayout.razor 구조:**

```razor
@inherits LayoutComponentBase

<div class="page">
    <div class="navbar">
        <a href="/">Aristokeides</a>
        <div>
            <AuthorizeView>
                <Authorized>
                    <span>@context.User.Identity?.Name 님</span>
                    <a href="/settings" style="margin-left: 16px;">설정</a>
                    <a href="/api/auth/logout" style="margin-left: 16px;">로그아웃</a>
                </Authorized>
                <NotAuthorized>
                    <a href="/login">로그인</a>
                    <a href="/register" style="margin-left: 16px;">회원가입</a>
                </NotAuthorized>
            </AuthorizeView>
        </div>
    </div>

    <main class="main-content">
        @Body
    </main>
</div>
```
</action>
<acceptance_criteria>
- Logout 링크가 `/api/auth/logout`을 가리키며 클릭 시 정상적으로 로그아웃된다 (404 버그 해소).
- 비로그인 상태에서 "로그인"과 "회원가입" 두 링크가 네비게이션 바에 표시된다.
- "회원가입" 링크가 `/register`로 연결된다.
- 모든 네비게이션 텍스트가 한국어로 표시된다.
- 로그인 상태에서 "~님", "설정", "로그아웃"이 올바르게 표시된다.
- "Aristokeides" 브랜드 텍스트는 영어를 유지한다 (프로젝트명).
- 기존 `AuthorizeView` 분기 구조가 유지된다.
</acceptance_criteria>
</task>

## must_haves

- Logout URL을 `/api/auth/logout`으로 반드시 수정해야 한다 (기존 404 버그 수정).
- 비로그인 시 로그인과 회원가입 링크가 모두 보여야 한다.
- 프로젝트명 "Aristokeides"는 번역하지 않는다.
- 기존 레이아웃 구조(`page`, `navbar`, `main-content`)를 변경하지 않는다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Components/MainLayout.razor` | 수정 | Register 링크 추가, Logout URL 수정, 한국어 번역 |
