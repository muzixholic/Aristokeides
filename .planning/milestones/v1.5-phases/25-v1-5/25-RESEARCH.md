# Phase 25: 마일스톤 v1.5 통합 검증 및 문서화 - Research

## 1. 개요 및 연구 목적
본 연구는 마일스톤 v1.5(UI 테스트 자동화 및 SSH 호환성 개선)의 최종 통합 검증 및 문서화 단계(Phase 25)를 계획(Plan)하고 실행(Execute)하기 위해 필요한 기술적 배경, 기존 테스트 인프라 메커니즘, SSH 호환성 개선 현황 및 문서화 요구사항을 분석하는 데 목적이 있습니다.

---

## 2. 테스트 자동화 구조 및 메커니즘 분석
### 2.1 bUnit 단위 테스트 구조
- **기본 클래스 (`BunitTestBase.cs`)**:
  - `BunitContext`를 상속하며, 격리된 단위 테스트를 위해 실행마다 고유한 Guid를 사용하여 인메모리 데이터베이스(`UseInMemoryDatabase`)를 생성합니다.
  - 테스트에 필요한 의존성 주입(DI)으로 `AppDbContext`, `RepositoryCreationChannel`, 그리고 테스트용 대역(Fake)인 `FakeSetupService`, `FakeIssueService`를 등록하여 실제 외부 서비스 기동 없이 UI 컴포넌트의 렌더링 및 이벤트를 신속하게 검증할 수 있도록 지원합니다.

### 2.2 Playwright E2E 브라우저 테스트 인프라 (`PlaywrightHostHelper.cs`)
- **포트 바인딩 규칙**:
  - Kestrel 웹 호스트 포트를 `5002`로 고정(`Port = "5002"`)하여 `http://localhost:5002` 주소에서 서빙합니다.
  - 최상위 문(Top-level statement)으로 컴파일된 `Program` 클래스의 EntryPoint를 리플렉션(Reflection)으로 획득하여 백그라운드 태스크(`Task.Run`)로 실행하며, 인수 인젝션을 통해 `--urls http://localhost:5002`를 전달합니다.
- **격리 환경 및 데이터베이스 생명주기**:
  - 테스트 격리를 위해 매 실행마다 고유한 파일명(`e2e_test_{Guid:N}.db`)의 SQLite 데이터베이스를 자동 생성합니다.
  - 환경 변수(`Database__Provider`="SQLite", `Database__ConnectionString`="Data Source={DbName}", `IsInstalled`= true/false, `GitSettings__BasePath`="GitRepos")를 주입하여 기존 설정값을 오버라이드하고 기동합니다.
  - Kestrel 서버 기동 후 포트 바인딩 안정화를 위해 `3초`(`Task.Delay(3000)`)의 대기 시간을 가집니다.
  - 테스트 종료(`Dispose()`) 시, 주입했던 환경 변수를 초기화하고 `Program.App.StopAsync()`를 통해 Kestrel 프로세스를 정상 종료하며, 생성되었던 임시 SQLite DB 파일 및 테스트용 Git 저장소 폴더(`GitRepos`)를 디스크에서 완전히 삭제합니다.
- **로컬 디버깅 및 헤디드(Headed) 모드 팁**:
  - Playwright는 기본적으로 헤드리스(Headless) 모드로 작동하나, 로컬 디버깅 시 UI 동작을 눈으로 확인하려면 `PlaywrightE2eTests.cs`의 `BrowserTypeLaunchOptions`에서 `Headless = false`로 수정하거나 환경 변수를 연동하도록 구성할 수 있습니다. (예: `dotnet test` 실행 전 특정 디버그 모드 플래그 활성화)

---

## 3. SSH 호환성 개선 현황 분석
### 3.1 Microsoft.DevTunnels.Ssh 도입 및 라이브러리 교체
- 기존 `FxSsh 1.3.0` 라이브러리를 완전히 제거하고 Microsoft의 오픈소스인 `Microsoft.DevTunnels.Ssh`로의 전면 교체가 수행되었습니다.
- 이를 통해 현대적인 암호화 표준인 `ed25519`, `rsa-sha2-256`, `rsa-sha2-512` 알고리즘에 대한 자체 내장 지원을 확보하였습니다.
- 기존의 ECDsa PEM 형식 호스트 키(`host.key`) 호환성을 하이브리드 형태로 지원하며, 신규 설치 시에는 자동으로 `ED25519` 호스트 키를 자동 생성하도록 개선되었습니다.

### 3.2 데이터베이스 감사 로깅 (`SshAuthLog`)
- 보안 감사 강화를 위해 SSH 로그인 및 인증 시도 기록을 데이터베이스에 영구 보관하는 `SshAuthLog` 테이블이 신규 추가되었습니다.
- 로그 저장 필드: 인증 시도 시각, 클라이언트 IP, SSH 키 지문(Fingerprint), 사용자명(Username), 인증 성공 여부(IsSuccess), 실패 사유(FailureReason), 키 유형(KeyType) 등이 기록됩니다.

### 3.3 테스트 호환성 우회 (Renci.SshNet 비호환 이슈 해결)
- **이슈 배경**: `Renci.SshNet` 테스트 클라이언트 라이브러리가 핸드셰이크/연결 해제 시 `DevTunnels.Ssh` 서버가 전달하는 연결 해제 메시지의 특정 길이 불일치로 인해 `ArgumentOutOfRangeException`을 발생시키는 비호환성 문제가 존재하였습니다.
- **해결 방안**: 테스트 프로젝트(`Aristokeides.Tests`) 내 SSH 관련 통합 테스트(`SshCommandPipingTests`, `SshServerAuthTests`, `SshAuthLogTests`)를 전면 리팩토링하였습니다. C# 내의 `Renci.SshNet`을 사용하는 대신, 테스트 코드에서 **실제 운영체제의 `ssh` 프로세스 CLI 명령(`Process.Start("ssh", ...)`)을 직접 호출**하여 백그라운드 서버와 통신하는 방식을 구축하였습니다.
- **결과**: 이 적응적 설계를 통해 실제 Git/SSH 클라이언트가 접속하는 것과 완벽히 동일한 환경에서 인증 성공, 잘못된 유저네임 차단, 미등록 키 차단, 경로 탐색 공격 차단, 일반 쉘 실행 차단 등의 시나리오를 검증하였으며, 결과적으로 전체 104개 테스트 스위트가 모두 성공적으로 작동함을 확인하였습니다.

---

## 4. Phase 25 문서화 계획
### 4.1 TESTING.md 신설 (프로젝트 루트)
- **환경 구성 가이드**: `dotnet build`, `playwright install` (또는 `pwsh bin/Debug/net8.0/playwright.ps1 install`), `dotnet test` 명령어 사용법 제시.
- **인프라 설명**: `PlaywrightHostHelper.cs`를 통한 Kestrel 백그라운드 실행 방식 및 `e2e_test.db` SQLite 파일의 자동 생성/삭제 격리 라이프사이클 투명하게 공개.
- **트러블슈팅(Troubleshooting) 섹션**: 
  - Playwright 드라이버 설치 누락 시 대처 방법.
  - Kestrel 포트 5002번 충돌 발생 시 해결법.
  - SSH 테스트 시 로컬 머신에 `ssh` 클라이언트 도구(OpenSSH) 미설치 혹은 미등록 상황에 대한 자가 진단 및 대체 방법 가이드.
- **디버깅 팁**: 테스트 코드를 헤디드(Headed) 모드로 실행하는 방법 소개.

### 4.2 README.md 링크 제공
- 기존 `README.md` 내에 테스트 가이드 관련 항목을 언급하고, 신설된 `TESTING.md`로 이동할 수 있는 마크다운 링크 제공.

### 4.3 마일스톤 회고록 (RETROSPECTIVE.md) 및 마일스톤 감사 (v1.5-MILESTONE-AUDIT.md) 작성
- **성과 지표 표 작성**: 104개 전체 테스트 성공 내역 시각화.
- **아키텍처 및 보안 영향 분석**:
  - `DevTunnels.Ssh` 도입에 따른 최신 암호 알고리즘 지원 확보 현황.
  - 데이터베이스 감사 로깅(`SshAuthLog`) 적용에 따른 DB 트랜잭션 성능 영향 및 비동기적 감사 처리 설계 타당성 분석.
  - 로컬 테스트 환경과 CI 빌드 파이프라인의 일치성 평가.

---

## 5. Phase 25 실행 계획 제안 (PLAN.md 구성 요소)
- **Task 25-01-01**: `TESTING.md` 문서 신설 및 `README.md` 링크 업데이트.
- **Task 25-01-02**: 마일스톤 회고록(`RETROSPECTIVE.md`) 및 마일스톤 감사 보고서(`.planning/v1.5-MILESTONE-AUDIT.md`) 작성.
- **Task 25-01-03**: 전체 테스트 스위트(`dotnet test`) 최종 통합 실행 및 검증 수행.
- **Task 25-01-04**: GSD 스킬(`gsd-audit-milestone` 및 `gsd-complete-milestone`)을 구동하여 마일스톤 v1.5 종료 및 아카이빙 처리.
