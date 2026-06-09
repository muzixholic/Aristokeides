---
title: "세션 DB 관리 및 원격 로그아웃 구현"
phase: 18
wave: 3
depends_on: [18B-PLAN.md]
files_modified:
  - Aristokeides.Api/Models/UserSession.cs
  - Aristokeides.Api/Data/AppDbContext.cs
  - Aristokeides.Api/Controllers/AuthController.cs
  - Aristokeides.Api/Middleware/SessionValidationMiddleware.cs
  - Aristokeides.Api/Program.cs
  - Aristokeides.Api/Components/Pages/Settings.razor
autonomous: true
requirements:
  - "사용자는 현재 로그인된 기기 및 세션 목록을 실시간으로 조회할 수 있어야 한다."
  - "사용자는 특정 세션을 원격으로 로그아웃(무효화)시킬 수 있어야 하며, 무효화된 세션을 가진 브라우저는 즉시 권한을 잃고 로그인 페이지로 유도되어야 한다."
---

# Plan 18C: 세션 DB 관리 및 원격 로그아웃 구현

## Objective

각 로그인 시 생성되는 세션 정보를 데이터베이스(`UserSession` 엔터티)에 기록하고, 세션 유효성을 매 요청마다 체크하는 `SessionValidationMiddleware`를 도입하여 실시간 세션 관리를 가능하게 한다. 설정 화면(`Settings.razor`)의 보안 탭에 활성 세션 목록 조회 및 개별 무효화(원격 로그아웃) 기능을 연동한다.

## Tasks

<task id="18C-1">
<title>UserSession 모델 및 마이그레이션 적용</title>
<read_first>
- `Aristokeides.Api/Models/User.cs` — 외래키 관계 설정을 위해 User 엔터티 확인
- `Aristokeides.Api/Data/AppDbContext.cs` — 기존 DB 설정 및 빌더 정의 참조
</read_first>
<action>
1. `Aristokeides.Api/Models/UserSession.cs` 모델 파일을 신규 정의한다:
   ```csharp
   namespace Aristokeides.Api.Models;

   public class UserSession
   {
       public required string Id { get; set; } // 암호학적 토큰 또는 Guid
       public int UserId { get; set; }
       public string? UserAgent { get; set; }
       public string? IpAddress { get; set; }
       public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
       public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
       public bool IsRevoked { get; set; } = false;

       public User User { get; set; } = null!;
   }
   ```
2. `AppDbContext.cs`에 `DbSet<UserSession>`을 등록하고, `OnModelCreating`에서 복합 인덱스 및 외래키 제약 조건을 지정한다.
3. `EF Core` 마이그레이션(Sqlite, Postgres, Mysql 각각)을 추가하고 적용한다:
   - `dotnet ef migrations add AddUserSessions --context SqliteAppDbContext -o Migrations/Sqlite`
   - `dotnet ef migrations add AddUserSessions --context PostgresAppDbContext -o Migrations/Postgres`
   - `dotnet ef migrations add AddUserSessions --context MysqlAppDbContext -o Migrations/Mysql`
</action>
<acceptance_criteria>
- `UserSession` 모델 및 DB 컨텍스트 바인딩이 완료된다.
- DB 마이그레이션이 빌드 오류 없이 세 가지 데이터베이스 프로바이더 모두 적용된다.
</acceptance_criteria>
</task>

<task id="18C-2">
<title>로그인 시 세션 발급 및 로그아웃 시 세션 해제</title>
<read_first>
- `Aristokeides.Api/Controllers/AuthController.cs` — 기존 `CookieLogin`, `ExternalLoginCallback`, `Logout` 메서드 확인
</read_first>
<action>
`AuthController.cs`를 수정하여 세션 라이프사이클을 데이터베이스와 연동한다.

1. **로그인 완료 시 세션 레코드 추가:**
   - 일반 로그인(`CookieLogin`), 2FA 최종 로그인(`Login2Fa` 핸들러), 소셜 로그인 완료 콜백 핸들러에서 사용자의 아이덴티티를 획득한 후 신규 세션을 생성한다:
     ```csharp
     var sessionId = Guid.NewGuid().ToString("N");
     var session = new UserSession
     {
         Id = sessionId,
         UserId = user.Id,
         UserAgent = Request.Headers.UserAgent.ToString(),
         IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
     };
     _db.UserSessions.Add(session);
     await _db.SaveChangesAsync();
     ```
   - 로그인 쿠키 발급용 ClaimsIdentity 생성 시 `SessionId` 클레임(`new Claim("SessionId", sessionId)`)을 주입한다.

2. **로그아웃 시 세션 상태 업데이트:**
   - `Logout()` 호출 시, 현재 쿠키 세션 클레임에서 `SessionId`를 찾아 DB의 해당 세션 상태를 `IsRevoked = true`로 변경한 뒤 `SignOutAsync`를 수행한다.
</action>
<acceptance_criteria>
- 로그인 성공 시 데이터베이스 `UserSessions` 테이블에 레코드가 자동 추가된다.
- 로그인 세션 클레임에 고유 `SessionId`가 포함된다.
- 로그아웃 처리 시 해당하는 DB 세션 항목이 `IsRevoked = true` 상태로 안전하게 전환된다.
</acceptance_criteria>
</task>

<task id="18C-3">
<title>SessionValidationMiddleware 구현 및 등록</title>
<read_first>
- `Aristokeides.Api/Middleware/SetupRedirectMiddleware.cs` — 기존 미들웨어 구조 참조
- `Aristokeides.Api/Program.cs` — 미들웨어 실행 파이프라인 흐름 파악
</read_first>
<action>
1. `Aristokeides.Api/Middleware/SessionValidationMiddleware.cs` 파일을 신규 구현한다.
   - 요청마다 HttpContext User의 `SessionId` 클레임을 가져온다.
   - 클레임이 존재하는 경우, DB `UserSessions`에서 해당 세션을 조회한다.
   - 세션이 존재하지 않거나 `IsRevoked == true`인 경우:
     - `HttpContext.SignOutAsync("Cookies")`를 실행하고 `/login?error=session_expired`로 강제 리다이렉트한다.
   - 세션이 유효하고 정상인 경우:
     - 성능 최적화를 고려해 `LastActiveAt` 필드 갱신을 스로틀링(예: 마지막 갱신 5분 이후일 경우에만 업데이트)하여 DB에 반영한다.
2. `Program.cs`에서 `app.UseAuthentication();` 바로 뒤에 `app.UseMiddleware<SessionValidationMiddleware>();`를 등록한다.
</action>
<acceptance_criteria>
- 미들웨어가 매 HTTP 요청마다 사용자 인증 세션의 만료 여부를 검사한다.
- 강제 취소(Revoked)된 세션을 가진 브라우저가 다음 요청 시 쿠키가 삭제되고 즉시 `/login`으로 튕겨 나간다.
- 유효한 세션에 대해서는 요청 주기가 일정 시점 이상 경과했을 때 `LastActiveAt` 필드가 데이터베이스에 성공적으로 업데이트된다.
</acceptance_criteria>
</task>

<task id="18C-4">
<title>Settings.razor 내 보안 세션 관리 UI 연동</title>
<read_first>
- `Aristokeides.Api/Components/Pages/Settings.razor` — 기존 HTML 및 InteractiveServer C# 바인딩 양식 참조
</read_first>
<action>
`Settings.razor`의 2FA 탭과 통합된 "Security & Sessions" 영역 하단에 활성 세션 목록 조회 및 원격 로그아웃 UI를 구성한다:

1. **활성 세션 목록 바인딩:**
   - 컴포넌트 초기화(`OnInitializedAsync`) 시 현재 로그인한 사용자의 ID로 조회된 모든 세션 목록(`IsRevoked == false`)을 DB에서 가져와 정렬하여 표시한다.
   - 각 세션마다 User-Agent 문자열(브라우저 종류 및 OS 정보 가독성 높게 포맷팅 지원), IP 주소, 그리고 마지막 활성 일시(`LastActiveAt`), "현재 세션" 여부를 화면에 출력한다.
2. **세션 종료 액션 연동:**
   - 세션 행마다 "세션 종료(로그아웃)" 버튼을 노출한다.
   - 버튼 클릭 시 해당 세션의 DB 상태를 `IsRevoked = true`로 설정하는 API를 호출하거나 서버 메서드를 직접 실행하여 목록을 갱신한다.
</action>
<acceptance_criteria>
- 설정 화면의 세션 탭에 활성 상태인 모든 본인의 기기 목록이 리스트 형태로 로드된다.
- 본인이 접근 중인 "현재 기기/세션"에는 별도의 배지(예: `현재 세션`)가 표시되고, 본인의 세션은 원격 로그아웃 대상에서 구분하거나 종료 시 경고를 띄운다.
- 특정 기기의 세션을 종료하면 해당 기기의 접속 세션이 DB에서 `IsRevoked` 처리되고 화면에서 사라진다.
</acceptance_criteria>
</task>

## must_haves

- `SessionValidationMiddleware` 도입 시 성능 보장을 위해 `LastActiveAt` 필드 업데이트는 5분 미만의 단위로는 쓰기를 스로틀링해야 한다.
- 세션 무효화 즉시 다음 요청에서 인증 쿠키의 강제 폐기 및 리다이렉트가 동작해야 한다.
- 활성 세션 목록에는 오직 로그인한 본인 계정의 세션 정보만 조회되고 타인의 데이터가 유출되지 않아야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Models/UserSession.cs` | 신규 | 세션 정보 데이터 모델 엔터티 |
| `Aristokeides.Api/Middleware/SessionValidationMiddleware.cs` | 신규 | 세션 실시간 취소 여부 검증 및 강제 로그아웃 미들웨어 |
| `Aristokeides.Api/Data/AppDbContext.cs` | 수정 | `UserSession` DB 매핑 추가 |
| `Aristokeides.Api/Controllers/AuthController.cs` | 수정 | 로그인 완료 시 세션 토큰 DB 적재 및 로그아웃 시 세션 취소 추가 |
| `Aristokeides.Api/Program.cs` | 수정 | 세션 검증 미들웨어 호출 등록 |
| `Aristokeides.Api/Components/Pages/Settings.razor` | 수정 | 설정 화면에 접속 세션 목록 출력 및 원격 종료 기능 UI 구현 |
