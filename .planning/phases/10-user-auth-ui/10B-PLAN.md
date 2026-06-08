---
title: "Login 페이지 개선"
phase: 10
wave: 1
depends_on: []
files_modified:
  - Aristokeides.Api/Components/Pages/Login.razor
autonomous: true
requirements:
  - "새로운 사용자는 웹 브라우저를 통해 직관적으로 회원가입을 하고 로그인/로그아웃할 수 있어야 한다."
---

# Plan 10B: Login 페이지 개선

## Objective

기존 `Login.razor` 페이지에 에러/성공 메시지 표시, 회원가입 링크, 한국어 번역을 적용하여 사용자 경험을 개선한다.

## Tasks

<task id="10B-1">
<title>Login.razor 개선</title>
<read_first>
- `Aristokeides.Api/Components/Pages/Login.razor` — 현재 구현 전체 (L1-26)
- `Aristokeides.Api/Components/Pages/Settings.razor` L40-51 — 에러/성공 메시지 UI 스타일 참조
- `Aristokeides.Api/Controllers/AuthController.cs` L67-86 — `cookie-login` 엔드포인트의 리다이렉트 쿼리 파라미터 확인
- `Aristokeides.Api/wwwroot/css/app.css` — CSS 변수 확인
</read_first>
<action>
`Login.razor`를 다음과 같이 수정한다. 기존 SSR 정적 폼 패턴(`@rendermode` 없음)을 유지한다.

**1. `@code` 블록 추가 — 쿼리 파라미터 바인딩:**

```razor
@code {
    [SupplyParameterFromQuery] public string? Error { get; set; }
    [SupplyParameterFromQuery] public string? Registered { get; set; }
}
```

- `Error`: `cookie-login` 실패 시 `?error=invalid_credentials` 수신
- `Registered`: 회원가입 성공 후 `?registered=true` 수신

**2. 에러 메시지 표시 (폼 상단, `<h2>` 아래에 삽입):**

```razor
@if (Error == "invalid_credentials")
{
    <div style="background-color: #fee2e2; color: #ef4444; padding: 12px; border-radius: 4px; margin-bottom: 16px; font-size: 14px; border: 1px solid #fca5a5;">
        이메일 또는 비밀번호가 올바르지 않습니다.
    </div>
}
```

- 스타일: Settings.razor 에러 메시지 패턴 (빨간 배경, 빨간 텍스트, 빨간 테두리)

**3. 성공 메시지 표시 (폼 상단, 에러 메시지 아래에 삽입):**

```razor
@if (Registered == "true")
{
    <div style="background-color: #d1fae5; color: #065f46; padding: 12px; border-radius: 4px; margin-bottom: 16px; font-size: 14px; border: 1px solid #6ee7b7;">
        회원가입이 완료되었습니다. 로그인해 주세요.
    </div>
}
```

- 스타일: Settings.razor 성공 메시지 패턴 (초록 배경, 초록 텍스트, 초록 테두리)

**4. 한국어 번역:**

| 기존 (영어) | 변경 (한국어) |
|---|---|
| `<PageTitle>Login</PageTitle>` | `<PageTitle>로그인</PageTitle>` |
| `<h2>Login</h2>` | `<h2>로그인</h2>` |
| `<label ...>Email</label>` | `<label ...>이메일</label>` |
| `<label ...>Password</label>` | `<label ...>비밀번호</label>` |
| `Log in` (버튼 텍스트) | `로그인` |

**5. 회원가입 링크 추가 (폼 `</form>` 아래, 카드 `</div>` 내부에 삽입):**

```html
<p style="text-align: center; margin-top: 16px; color: #6b7280; font-size: 14px;">
    계정이 없으신가요? <a href="/register">회원가입</a>
</p>
```

**최종 구조 (위→아래 순서):**
1. `@page`, `@attribute`
2. `<PageTitle>로그인</PageTitle>`
3. 카드 `<div>` 열기
4. `<h2>로그인</h2>`
5. 에러 메시지 (`@if Error`)
6. 성공 메시지 (`@if Registered`)
7. `<form>` (기존 폼 유지 — action, AntiforgeryToken, 필드, 버튼)
8. 회원가입 링크 `<p>`
9. 카드 `</div>` 닫기
10. `@code` 블록
</action>
<acceptance_criteria>
- `?error=invalid_credentials` 쿼리 파라미터가 있으면 빨간색 에러 메시지 "이메일 또는 비밀번호가 올바르지 않습니다."가 표시된다.
- `?registered=true` 쿼리 파라미터가 있으면 초록색 성공 메시지 "회원가입이 완료되었습니다. 로그인해 주세요."가 표시된다.
- 에러/성공 메시지 스타일이 Settings.razor 패턴과 일치한다.
- 모든 UI 텍스트가 한국어로 번역되어 있다 (제목, 라벨, 버튼, PageTitle).
- "계정이 없으신가요? 회원가입" 링크가 `/register`로 연결된다.
- 기존 폼 구조(action, AntiforgeryToken, input fields)가 유지된다.
- `@rendermode` 없이 SSR 정적 폼 패턴이 유지된다.
- 쿼리 파라미터가 없는 경우 에러/성공 메시지가 표시되지 않는다.
</acceptance_criteria>
</task>

## must_haves

- `@rendermode` 없는 SSR 정적 폼 패턴을 유지해야 한다.
- `[SupplyParameterFromQuery]`를 사용하여 쿼리 파라미터를 바인딩한다.
- 에러/성공 메시지 스타일은 프로젝트 내 Settings.razor 패턴과 일관성을 유지한다.
- 기존 폼 동작(POST → `/api/auth/cookie-login`)이 변경되지 않아야 한다.
- 모든 UI 텍스트는 한국어로 표시한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Components/Pages/Login.razor` | 수정 | 에러/성공 메시지, 한국어화, 회원가입 링크 추가 |
