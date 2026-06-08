---
title: "Register 엔드포인트 및 회원가입 페이지"
phase: 10
wave: 1
depends_on: []
files_modified:
  - Aristokeides.Api/Controllers/AuthController.cs
  - Aristokeides.Api/Components/Pages/Register.razor
autonomous: true
requirements:
  - "새로운 사용자는 웹 브라우저를 통해 직관적으로 회원가입을 할 수 있어야 한다."
---

# Plan 10A: Register 엔드포인트 및 회원가입 페이지

## Objective

SSR 정적 폼 패턴(Login.razor와 동일)을 사용하여 회원가입 페이지(`Register.razor`)를 구현하고, 이를 위한 `cookie-register` 엔드포인트를 `AuthController.cs`에 추가한다.

## Tasks

<task id="10A-1">
<title>cookie-register 엔드포인트 추가</title>
<read_first>
- `Aristokeides.Api/Controllers/AuthController.cs` — 기존 `cookie-login` 엔드포인트 패턴 참조 (L67-86)
- `Aristokeides.Api/Controllers/AuthController.cs` — 기존 `register` 엔드포인트 검증 로직 참조 (L29-51)
- `Aristokeides.Api/Models/User.cs` — User 모델 구조 확인
</read_first>
<action>
`AuthController.cs`에 `POST /api/auth/cookie-register` 엔드포인트를 추가한다.

**구현 상세:**

1. 메서드 시그니처:
   ```csharp
   [HttpPost("cookie-register")]
   public async Task<IActionResult> CookieRegister(
       [FromForm] string email,
       [FromForm] string username,
       [FromForm] string password)
   ```

2. 검증 로직 (기존 `Register` 메서드의 검증 패턴 재사용):
   - 이메일 중복 확인: `_db.Users.AnyAsync(u => u.Email == email)`
     - 중복 시 → `Redirect("/register?error=duplicate_email")`
   - 사용자명 중복 확인: `_db.Users.AnyAsync(u => u.Username == username)`
     - 중복 시 → `Redirect("/register?error=duplicate_username")`

3. 사용자 생성:
   ```csharp
   var user = new User
   {
       Email = email,
       Username = username,
       PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
       Role = "Reader"
   };
   _db.Users.Add(user);
   await _db.SaveChangesAsync();
   ```

4. 성공 시 → `Redirect("/login?registered=true")`

**위치:** 기존 `Logout()` 메서드(L88) 바로 위에 삽입.
</action>
<acceptance_criteria>
- `POST /api/auth/cookie-register` 엔드포인트가 `[FromForm]` 파라미터(email, username, password)를 수신한다.
- 이메일 중복 시 `/register?error=duplicate_email`로 리다이렉트한다.
- 사용자명 중복 시 `/register?error=duplicate_username`로 리다이렉트한다.
- 성공 시 BCrypt 해시된 비밀번호와 Role="Reader"로 User를 생성하고 `/login?registered=true`로 리다이렉트한다.
- 기존 `register`, `cookie-login`, `logout` 엔드포인트가 영향받지 않는다.
</acceptance_criteria>
</task>

<task id="10A-2">
<title>Register.razor 회원가입 페이지 생성</title>
<read_first>
- `Aristokeides.Api/Components/Pages/Login.razor` — SSR 정적 폼 패턴 및 인라인 스타일 참조 (전체 파일)
- `Aristokeides.Api/wwwroot/css/app.css` — CSS 변수 확인 (`--dominant`, `--accent`, `--destructive`)
- `Aristokeides.Api/Components/Pages/Settings.razor` L40-51 — 에러/성공 메시지 UI 패턴 참조
</read_first>
<action>
`Aristokeides.Api/Components/Pages/Register.razor` 파일을 새로 생성한다.

**구현 상세:**

1. 페이지 선언:
   ```razor
   @page "/register"
   @attribute [Microsoft.AspNetCore.Authorization.AllowAnonymous]
   ```

2. SSR 모드: `@rendermode` 없음 (Login.razor와 동일한 순수 SSR 패턴)

3. 폼 구조:
   ```html
   <form method="post" action="/api/auth/cookie-register">
       <AntiforgeryToken />
       <!-- 필드들 -->
   </form>
   ```

4. 입력 필드 (인라인 스타일은 Login.razor 패턴 준수):
   - **이메일**: `<input type="email" id="email" name="email" required />`
   - **사용자명**: `<input type="text" id="username" name="username" required />`
   - **비밀번호**: `<input type="password" id="password" name="password" required minlength="6" />`
   - **비밀번호 확인**: `<input type="password" id="passwordConfirm" name="passwordConfirm" required />`
     - 이 필드는 서버로 전송되지 않음 (name 속성 제거 또는 JS 검증 전용)

5. 비밀번호 확인 클라이언트 검증:
   ```html
   <script>
   document.querySelector('form').addEventListener('submit', function(e) {
       var pw = document.getElementById('password').value;
       var confirm = document.getElementById('passwordConfirm').value;
       if (pw !== confirm) {
           e.preventDefault();
           document.getElementById('password-mismatch-error').style.display = 'block';
       }
   });
   </script>
   ```

6. 에러 메시지 표시 (`@code` 블록으로 쿼리 파라미터 읽기):
   ```razor
   @code {
       [SupplyParameterFromQuery] public string? Error { get; set; }
   }
   ```
   - `error=duplicate_email` → "이미 등록된 이메일입니다."
   - `error=duplicate_username` → "이미 등록된 사용자명입니다."
   - 에러 메시지 스타일: Settings.razor 패턴 (`background: #fee2e2; color: #ef4444; border: 1px solid #fca5a5`)

7. 비밀번호 불일치 에러 (JS용, 기본 숨김):
   ```html
   <div id="password-mismatch-error" style="display:none; background-color: #fee2e2; ...">
       비밀번호가 일치하지 않습니다.
   </div>
   ```

8. 카드 컨테이너 스타일 (Login.razor와 동일):
   ```
   max-width: 400px; margin: 40px auto; padding: 24px;
   background: var(--dominant); border-radius: 8px;
   box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
   ```

9. 한국어 UI 텍스트:
   - 제목: "회원가입"
   - 라벨: "이메일", "사용자명", "비밀번호", "비밀번호 확인"
   - 버튼: "회원가입"
   - PageTitle: "회원가입"

10. 로그인 페이지 링크:
    ```html
    <p style="text-align: center; margin-top: 16px;">
        이미 계정이 있으신가요? <a href="/login">로그인</a>
    </p>
    ```
</action>
<acceptance_criteria>
- `/register` 경로에서 회원가입 폼이 렌더링된다.
- `[AllowAnonymous]` 적용으로 비인증 사용자가 접근 가능하다.
- `@rendermode` 없음 — 순수 SSR 정적 폼으로 동작한다.
- `<AntiforgeryToken />`이 폼에 포함되어 CSRF 방어가 적용된다.
- 폼이 `POST /api/auth/cookie-register`로 제출된다.
- 이메일, 사용자명, 비밀번호, 비밀번호 확인 4개 필드가 존재한다.
- 비밀번호와 비밀번호 확인이 불일치하면 클라이언트측 JS가 제출을 차단하고 에러 메시지를 표시한다.
- 쿼리 파라미터 `?error=duplicate_email` → "이미 등록된 이메일입니다." 에러 표시.
- 쿼리 파라미터 `?error=duplicate_username` → "이미 등록된 사용자명입니다." 에러 표시.
- 모든 UI 텍스트가 한국어이다.
- 로그인 페이지로의 링크가 존재한다.
- 인라인 스타일이 Login.razor 컨벤션과 일치한다 (CSS 변수 사용, 동일 카드 레이아웃).
</acceptance_criteria>
</task>

## must_haves

- `cookie-register` 엔드포인트는 기존 `cookie-login` 패턴과 일관성을 유지해야 한다 (`[FromForm]`, Redirect 기반).
- `Register.razor`는 Login.razor와 동일한 SSR 정적 폼 패턴을 사용해야 한다 (`@rendermode` 없음).
- `<AntiforgeryToken />` CSRF 방어 필수.
- 비밀번호는 BCrypt로 해시하여 저장한다.
- 모든 UI 텍스트는 한국어로 표시한다.
- 비밀번호 확인 필드의 클라이언트 검증이 동작해야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Controllers/AuthController.cs` | 수정 | `cookie-register` 엔드포인트 추가 |
| `Aristokeides.Api/Components/Pages/Register.razor` | 신규 | 회원가입 페이지 (SSR 정적 폼) |
