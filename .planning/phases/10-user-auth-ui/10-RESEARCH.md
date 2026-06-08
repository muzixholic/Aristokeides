# Phase 10: User Authentication UI — 리서치

**작성일:** 2026-06-08
**상태:** 완료

## 1. 현재 인증 아키텍처 분석

### 이중 인증 스킴 (Policy Scheme)

- `Program.cs` (L18-51): `JWT_OR_COOKIE` Policy Scheme이 기본 인증/도전 스킴
- `Authorization` 헤더에 `Bearer` 토큰 → JWT 스킴
- 없으면 → Cookie 스킴 (`"Cookies"`)
- Cookie 로그인 경로: `/login` (L39)
- `AddCascadingAuthenticationState()` (L54): Blazor에서 `AuthorizeView` 사용 가능

### API 엔드포인트 (AuthController.cs)

| 엔드포인트 | 메서드 | 입력 | 동작 |
|---|---|---|---|
| `POST /api/auth/register` | `[FromBody]` | Email, Username, Password | BCrypt 해시, Role="Reader", 201 반환 |
| `POST /api/auth/login` | `[FromBody]` | Email, Password | JWT 토큰 반환 (24h) |
| `POST /api/auth/cookie-login` | `[FromForm]` | email, password | Cookie 인증, 실패→`/login?error=invalid_credentials`, 성공→`/` |
| `GET /api/auth/logout` | 없음 | 없음 | Cookie sign-out → `/` 리다이렉트 |

### Register 검증 (AuthController.cs L32-36)
- 이메일 중복: `Conflict("이미 등록된 이메일입니다.")`
- 사용자명 중복: `Conflict("이미 등록된 사용자명입니다.")`

### User 모델 (User.cs)
- `int Id`, `string Email`(고유), `string Username`(고유), `string PasswordHash`, `string Role`(기본 "Reader"), `DateTime CreatedAt`

## 2. 기존 Login.razor 분석

- `@page "/login"` + `[AllowAnonymous]`
- SSR 정적 폼 (`@rendermode` 없음)
- `<form method="post" action="/api/auth/cookie-login">` → HTML 폼 직접 POST
- `<AntiforgeryToken />` 포함
- 필드: email (type="email"), password (type="password"), 둘 다 required
- 인라인 스타일 사용 (CSS 변수: `--dominant`, `--accent`)

### 현재 문제점
- `?error=invalid_credentials` 쿼리 파라미터 에러 메시지 UI 없음
- 회원가입 후 성공 메시지 없음
- 회원가입 페이지 링크 없음
- 텍스트가 영어

## 3. Register 페이지 (현재 미존재)

### 구현 방식 결정

**옵션 A: 순수 SSR 폼 (Login.razor 패턴) — 권장**
- HTML form POST → 새로운 `cookie-register` 엔드포인트 필요
- 장점: Login과 일관된 패턴
- 단점: 새 엔드포인트 추가 필요

**옵션 B: InteractiveServer (Settings.razor 패턴)**
- HttpClient로 `/api/auth/register` API 호출
- 장점: 기존 API 재사용
- 단점: Login과 패턴 불일치

**결론:** 옵션 A 채택 — `POST /api/auth/cookie-register` 엔드포인트 추가. 성공 시 `/login?registered=true` 리다이렉트, 실패 시 `/register?error=duplicate_email` 등 리다이렉트.

## 4. Logout 흐름

### 현재 상태 (버그)
- MainLayout.razor (L11): `<a href="/logout">Logout</a>` → 이 라우트에 대응하는 페이지 없음 (404)
- 실제 API: `GET /api/auth/logout`

### 해결 방안
- MainLayout의 Logout 링크를 `/api/auth/logout`으로 변경 (가장 간단하고 효과적)

## 5. Antiforgery (CSRF) 보호

- `AddAntiforgery()` + `UseAntiforgery()` 미들웨어 활성화
- Login.razor에서 `<AntiforgeryToken />` 사용
- 새 폼에도 반드시 `<AntiforgeryToken />` 포함 필요

## 6. Blazor SSR 패턴 (프로젝트 내 두 가지)

### 패턴 A: 순수 SSR 폼 (Login.razor)
- `@rendermode` 없음
- `<form method="post" action="...">`
- `<AntiforgeryToken />`
- 서버 리다이렉트로 결과 처리

### 패턴 B: InteractiveServer (Settings.razor)
- `@rendermode InteractiveServer`
- HttpClient → API 호출
- `errorMessage`/`successMessage` 상태 변수
- `@onsubmit`, `@bind` 이벤트 바인딩

### 에러/성공 메시지 UI 패턴 (Settings.razor L40-51)
```html
<!-- 에러: background #fee2e2, color #ef4444, border #fca5a5 -->
<!-- 성공: background #d1fae5, color #065f46, border #6ee7b7 -->
```

## 7. CSS/스타일링

### CSS 변수 (app.css)
- `--dominant: #FFFFFF` (배경)
- `--secondary: #F3F4F6` (네비바)
- `--accent: #2563EB` (파란색)
- `--destructive: #EF4444` (빨간색)

### 스타일링 컨벤션
- 인라인 스타일이 주류
- 카드: `max-width: 400px; margin: 40px auto; padding: 24px; background: var(--dominant); border-radius: 8px; box-shadow: ...`
- 입력: `padding: 8px; border: 1px solid #ccc; border-radius: 4px;`
- 버튼: `background: var(--accent); color: white; border: none; border-radius: 4px; font-weight: 600;`

## 8. MainLayout 네비게이션

### 현재 구조
- `AuthorizeView`로 인증 상태 분기
- 로그인 시: "Hello, {Name}!" + Settings + Logout
- 비로그인 시: Login 링크만

### 필요 변경
- 비로그인 시 Register 링크 추가
- Logout 링크 URL 수정

## 9. 구현 계획 요약

### 생성할 파일
1. `Register.razor` — 회원가입 페이지 (`@page "/register"`, `[AllowAnonymous]`)

### 수정할 파일
1. `Login.razor` — 에러/성공 메시지 표시, Register 링크, 한국어화
2. `MainLayout.razor` — Register 링크 추가, Logout URL 수정
3. `AuthController.cs` — `cookie-register` 엔드포인트 추가

### 핵심 결정 사항
1. Register 폼: SSR 정적 폼 (Login 패턴과 일관성 유지)
2. Logout: MainLayout 링크 URL 수정으로 해결
3. 비밀번호 확인 필드: 추가 권장 (UX 개선)

## Validation Architecture

### 검증 항목
- Register: 이메일/사용자명 중복 검증, 비밀번호 최소 길이
- Login: 잘못된 인증정보 에러 메시지 표시
- CSRF: AntiforgeryToken 포함 확인
- 로그아웃: Cookie 정리 및 리다이렉트 확인

---
*Phase: 10-user-auth-ui*
*Research completed: 2026-06-08*
