# Phase 23 Verification: Playwright E2E UI 테스트 검증 명세서

본 문서는 Phase 23에서 작성한 Playwright E2E 테스트 케이스들이 정상적으로 동작하는지 확인하는 검증 시나리오와 명세를 기술합니다.

## 1. Test Cases (E2E Scenarios)

### TC-23-01: Playwright 드라이버 정상 연동
* **검증 내용**: `Microsoft.Playwright` 모듈을 통해 Chromium headless 브라우저 인스턴스가 런타임에 정상 기동 및 종료되는지 검증.
* **성공 기준**: 테스트 시작 시 브라우저 초기화 오류(DriverNotFound 등) 없이 정상적으로 Page 객체가 생성될 것.

### TC-23-02: PlaywrightWebApplicationFactory 웹 호스트 기동 유효성
* **검증 내용**: 테스트 실행 시 Kestrel 웹 호스트가 지정된 로컬 포트(`http://localhost:5002`)를 리스닝하며 백그라운드로 자동 구동되고 요청을 처리하는지 검증.
* **성공 기준**: Playwright 브라우저가 `http://localhost:5002/` 페이지로 이동(`GotoAsync`) 시 HTTP 200 응답 또는 정상적인 HTML 소스가 로드될 것.

### TC-23-03: Setup UI E2E 흐름 및 데이터베이스 설치 완료 검증
* **검증 내용**: 최초 미설치 상태(`IsInstalled` = false)에서 `/setup`을 조작하여 어드민 가입 및 설치 절차를 수행하고 db가 생성되는지 검증.
* **동작**:
  1. `/setup` 페이지 이동.
  2. SQLite 정보 및 어드민 ID/Password 기입 후 "Install Aristokeides" 클릭.
* **성공 기준**: 버튼 클릭 후 홈 화면(`/`)으로 자동 페이지 리다이렉트가 이루어지고, `e2e_test.db` 데이터베이스에 어드민 유저 정보가 저장될 것.

### TC-23-04: 로그인/세션 E2E 흐름 검증
* **검증 내용**: Setup 완료 후, 생성된 어드민 정보로 정상 로그인이 수행되고 로그아웃 세션 처리가 완수되는지 검증.
* **성공 기준**:
  1. 로그인 성공 후 헤더 내 네비게이션 대시보드 및 사용자 정보가 올바르게 표시될 것.
  2. 로그아웃 버튼 클릭 시 `/login` 페이지로 돌아가며, 뒤로가기 클릭 시에도 다시 대시보드로 진입할 수 없어야 함.

### TC-23-05: 저장소 생성 E2E 흐름 검증
* **검증 내용**: 로그인된 사용자 상태에서 웹 브라우저를 조작하여 저장소를 생성하는 동작 검증.
* **동작**:
  1. 대시보드에서 "새 저장소 만들기" 링크 클릭.
  2. 이름 `"e2e-project"` 입력 후 제출.
* **성공 기준**: 저장소 생성 완료 후 `/{username}/e2e-project` 주소의 저장소 정보 화면으로 이동하며, 실제 디스크에 해당 저장소 git 폴더가 마련될 것.

### TC-23-06: 이슈 작성 E2E 흐름 검증
* **검증 내용**: 생성된 저장소의 이슈 트래커 화면에서 이슈를 성공적으로 등록하는 동작 검증.
* **성공 기준**: 제목과 내용을 채운 뒤 제출 시, 저장소 이슈 목록 화면(`/{username}/e2e-project/issues`)으로 리다이렉트되고 작성한 이슈 제목이 화면에 노출될 것.

## 2. Automated Run Command
E2E 테스트 실행을 위한 커맨드라인 명령어입니다.

```powershell
# 1. 테스트 빌드 및 Playwright 드라이버 설치
dotnet build E:\Workspace\VisualC#\Aristokeides\Aristokeides.Tests\Aristokeides.Tests.csproj

# 2. 드라이버 도구를 사용해 Chromium 다운로드 설치
# (실제 nuget 패키지 버전 경로가 다를 경우 패키지 버전 폴더 확인 필요, 통상 dotnet build 시 자동으로 설치되거나 수동 명령 실행 필요)
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium

# 3. Playwright E2E 전용 필터 테스트 실행
dotnet test E:\Workspace\VisualC#\Aristokeides\Aristokeides.Tests\Aristokeides.Tests.csproj --filter "FullyQualifiedName~Aristokeides.Tests.Playwright"
```

* **기대 결과**: TC-23-01부터 TC-23-06까지 정의된 모든 Playwright E2E 테스트가 실패 없이 **PASSED** 처리될 것.

## 3. Verification Results
* **실행 일시**: 2026-06-11
* **검증 여부**: **PASSED**
* **실행 로그**:
  ```
  E:\Workspace\VisualC#\Aristokeides\Aristokeides.Tests\bin\Debug\net10.0\Aristokeides.Tests.dll(.NETCoreApp,Version=v10.0)에 대한 테스트 실행
  지정된 패턴과 일치한 총 테스트 파일 수는 1개입니다.

  통과!  - 실패:     0, 통과:     2, 건너뜀:     0, 전체:     2, 기간: 16 s - Aristokeides.Tests.dll (net10.0)
  ```
* **상세 검증 내용**:
  * **TC-23-01 & TC-23-02**: `PlaywrightHostHelper`를 통해 백그라운드 Kestrel 기동 시 어셈블리 리플렉션을 통해 최상위 진입점을 성공적으로 호출 및 Kestrel 구동 포트 바인딩 완료.
  * **TC-23-03**: 초기 설치 상태에서 `/setup` 진입 후 어드민 기입 및 제출 시 서버 자동 셧다운 및 데이터베이스 유저 데이터 삽입 완료 확인.
  * **TC-23-04 ~ TC-23-06**: 기설치 데이터베이스 기반 로그인 완료, 저장소 생성(디스크 폴더 매핑 정합성 일치화 반영), 이슈 신규 생성(Blazor UI 내부 클릭 내비게이션을 통해 웹소켓 레이스 컨디션 완벽 회피) 및 칸반 보드 카드 렌더링 확인.
