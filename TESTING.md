# Aristokeides 테스트 가이드 (TESTING.md)

본 문서는 **Aristokeides (아리스토케이데스)** 프로젝트의 테스트 실행 방법, E2E 테스트 아키텍처, 디버깅 팁 및 트러블슈팅 가이드를 제공합니다.

---

## 🚀 테스트 실행 방법

Aristokeides는 xUnit, bUnit, Playwright를 사용하여 단위 테스트, Blazor 컴포넌트 테스트 및 E2E 브라우저 테스트를 수행합니다.

### 1. 전제 조건 및 빌드
테스트를 구동하기 전에 먼저 프로젝트를 빌드하고 Playwright에 필요한 브라우저 드라이버를 설치해야 합니다.

```powershell
# 1. 전체 솔루션 빌드
dotnet build

# 2. Playwright 브라우저 드라이버 설치
# Windows (PowerShell) 환경
pwsh bin/Debug/net10.0/playwright.ps1 install

# 또는 dotnet tool 또는 일반 CLI 실행(설치 경로에 따라 다를 수 있음)
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

### 2. 전체 테스트 스위트 실행
모든 테스트(bUnit 단위 테스트, Playwright E2E 테스트, SSH 통합 테스트)를 구동하려면 아래 명령을 사용합니다.

```powershell
dotnet test
```

---

## 🏗 E2E 테스트 격리 아키텍처

Aristokeides의 Playwright E2E 테스트는 독립된 실행 환경을 보장하기 위해 자동화된 데이터베이스 및 서버 포트 격리 기전을 사용합니다.

### 1. 격리 SQLite 데이터베이스 생명주기
- **동적 파일명 지정**: 테스트 클래스가 초기화될 때마다 `PlaywrightHostHelper`는 `e2e_test_{Guid:N}.db` 형식의 임시 SQLite 데이터베이스 이름을 무작위로 생성합니다.
- **환경 변수 주입**: Kestrel 서버를 띄울 때 해당 파일명을 연결 문자열로 주입하여, 다른 동시 테스트나 로컬 개발용 DB 데이터와 충돌하지 않고 완전히 빈 DB 상태에서 테스트가 실행됩니다.
- **자동 리소스 정리**: 테스트가 성공하거나 실패한 후 `Dispose` 메서드를 통해 임시로 생성된 SQLite DB 파일(`.db`)과 테스트용 Git 저장소 폴더(`GitRepos/`)가 디스크에서 완전히 삭제됩니다.

### 2. Kestrel 5002 포트 바인딩
- E2E 호스트는 프로덕션 기본 포트와의 간섭을 피하기 위해 **`5002`번 포트**를 점유하여 동작합니다.
- `PlaywrightHostHelper` 내부적으로 `Program.Main` 엔트리 포인트를 호출하며, `--urls http://localhost:5002` 아규먼트를 주입하여 웹 호스트를 로컬 바인딩합니다.

---

## 🔍 디버깅 팁

### 1. 헤디드(Headed) 모드로 브라우저 동작 확인하기
기본적으로 Playwright E2E 테스트는 백그라운드에서 브라우저 창을 띄우지 않고 실행되는 **헤드리스(Headless)** 모드로 작동합니다. UI 렌더링 과정을 눈으로 보며 디버깅하고 싶다면 아래 설정을 수정하십시오.

- **설정 변경 위치**: `Aristokeides.Tests/PlaywrightE2eTests.cs`
  - `BrowserTypeLaunchOptions` 개체에서 `Headless = true`를 **`Headless = false`**로 변경합니다.
  - 추가로 `SlowMo = 500`과 같은 대기 시간을 주면 폼 입력 및 클릭 등의 상호작용 과정을 더욱 천천히 관찰할 수 있습니다.

```csharp
// PlaywrightE2eTests.cs 예시
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions 
{ 
    Headless = false, // 헤디드 모드 활성화 (브라우저 창이 표시됨)
    SlowMo = 1000     // 동작 간 1초씩 지연하여 디버깅 용이
});
```

### 2. 테스트용 환경 변수 오버라이드
E2E 호스팅 환경에서 구동되는 서비스들의 행위를 수정하기 위해 `PlaywrightHostHelper.StartAsync()` 단계에서 다음과 같은 환경 변수를 커스터마이징할 수 있습니다.
- `Database__Provider`: SQLite
- `Database__ConnectionString`: Data Source=e2e_test_xxx.db
- `IsInstalled`: `true`로 설정 시 이미 설치가 완료된 대시보드로 즉시 리다이렉트되며, `false` 설정 시 Setup 마법사 화면이 나타납니다.
- `GitSettings__BasePath`: 테스트 중에 동적으로 생성될 Git 리포지토리의 저장 경로를 설정합니다.

---

## 🛠 트러블슈팅 가이드

### 1. `5002 포트 충돌` (Kestrel 포트가 이미 사용 중인 경우)
- **증상**: `Web host failed to start` 또는 `Address already in use` 예외가 발생하며 E2E 호스트 구동이 실패함.
- **해결책**:
  1. 다른 터미널에서 Aristokeides.Api 프로젝트가 포트 5002에서 가동 중인지 확인하고 이를 종료합니다.
  2. 다음 명령어를 실행하여 5002 포트를 점유하고 있는 프로세스를 찾아 강제 종료합니다.
     - **Windows (PowerShell)**:
       ```powershell
       Get-NetTCPConnection -LocalPort 5002 | Select-Object OwningProcess
       Stop-Process -Id <PID>
       ```
     - **macOS / Linux**:
       ```bash
       lsof -i :5002
       kill -9 <PID>
       ```

### 2. `Playwright 드라이버/브라우저 누락 에러`
- **증상**: Playwright가 실행될 때 브라우저 드라이버를 찾을 수 없다는 오류가 발생함.
- **해결책**:
  1. 테스트 프로젝트가 정상 컴파일되었는지 확인합니다.
  2. 프로젝트 출력 폴더의 `playwright.ps1`을 실행하여 브라우저 바이너리를 수동으로 다시 설치합니다.
     ```powershell
     pwsh bin/Debug/net10.0/playwright.ps1 install
     ```

### 3. `로컬 OS SSH CLI 도구 미설치 및 테스트 오류`
- **증상**: SSH Command 및 Piping 통합 테스트 중 SSH 연결 수동 테스트가 실패하거나 CLI 오류가 발생함.
- **해결책**:
  - Windows의 경우, **OpenSSH 클라이언트**가 활성화되어 있는지 확인하십시오.
    - `설정 > 앱 > 선택적 기능`에서 'OpenSSH 클라이언트'가 설치되어 있는지 확인합니다.
    - 또는 PowerShell에서 `ssh -V` 명령어가 정상 동작하는지 확인합니다.
  - Linux/macOS는 `ssh`가 기본 내장되어 있으므로 터미널 경로 검색을 점검합니다.
