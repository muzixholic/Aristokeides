# Phase 22 Verification: bUnit 기반 Blazor 컴포넌트 단위 테스트 검증 명세서

본 문서는 Phase 22에서 작성한 bUnit 테스트 케이스들이 정상적으로 작동하는지 확인하는 검증 시나리오와 명세를 기술합니다.

## 1. Test Cases (UAT & Unit Tests)

### TC-22-01: bUnit 인프라 및 베이스 클래스 유효성
* **검증 내용**: `BunitTestBase.cs`를 통한 InMemory DB 생성 및 주요 모크 서비스 DI 바인딩의 정상 동작 검증.
* **성공 기준**: 테스트 실행 시 예외 없이 DI 종속성이 해결되고 각 테스트 격리 상태가 정상 유지될 것.

### TC-22-02: Login 컴포넌트 쿼리스트링 상태 렌더링 검증
* **검증 내용**: 쿼리스트링 `Error` 또는 `Registered` 유무에 따른 에러/성공 메시지 출력 여부.
* **입력값**:
  * Case A: `Error = "invalid_credentials"`
  * Case B: `Registered = "true"`
* **성공 기준**: 
  * Case A: `"이메일 또는 비밀번호가 올바르지 않습니다."` 텍스트 포함 확인.
  * Case B: `"회원가입이 완료되었습니다."` 텍스트 포함 확인.

### TC-22-03: Setup 컴포넌트 프로바이더 전환 및 폼 유효성 검증
* **검증 내용**: DB 프로바이더 변경 시 바인딩 동작 및 폼 유효성 오류 및 설치 완료 후 네비게이션 동작 검증.
* **동작**:
  * 1. 렌더링 후 `#DatabaseProvider` 셀렉터를 `"PostgreSQL"`로 변경.
  * 2. 빈 상태로 폼 서브밋 트리거.
* **성공 기준**:
  * 1. 포트가 `5432`로 바뀌어 표시되며, SQLite 파일 경로 인풋이 DOM에서 정상적으로 사라져야 함.
  * 2. `ValidationMessage` 요소 내에 각 필수값 관련 오류 정보가 렌더링되어야 함.
  * 3. 설치 완료 시 `NavigationManager`가 `"/"` 경로로 `forceLoad: true`를 받아 페이지 전환 처리를 수행할 것.

### TC-22-04: NewRepository 컴포넌트 인증 정보 및 중복 체크 검증
* **검증 내용**: 인증 컨텍스트 설정 시 소유자 바인딩 및 저장소 이름 중복 유효성 검사.
* **동작**:
  * 1. `Bunit` 인증에 `"tester"` 계정 주입 후 렌더링.
  * 2. 이미 DB에 등록된 `"tester/duplicate-repo"` 와 동일한 이름 입력 후 제출 클릭.
* **성공 기준**:
  * 1. 드롭다운 옵션에 `"tester (Me)"` 및 등록해 둔 조직명이 정상 표시되어야 함.
  * 2. 화면에 `"이미 사용 중인 저장소 이름입니다: duplicate-repo"` 에러 메시지가 표출되어야 함.

### TC-22-05: RepoIssueForm 컴포넌트 필수값 미입력 폼 에러 검증
* **검증 내용**: 제목(`Title`) 미지정 시 유효성 검증 메시지 표출 확인.
* **동작**:
  * 1. 제목 필드를 공란으로 두고 `submit` 버튼 클릭.
* **성공 기준**:
  * 1. 화면에 `"Title is required"` 유효성 메시지가 렌더링되어 표시될 것.

## 2. Automated Run Command
다음의 명령어를 사용하여 새로 구현된 테스트를 실행하고 정상 통과를 보장합니다.

```powershell
dotnet test E:\Workspace\VisualC#\Aristokeides\Aristokeides.Tests\Aristokeides.Tests.csproj --filter "FullyQualifiedName~Aristokeides.Tests"
```
* **기대 결과**: 새로 생성한 모든 UI 테스트 클래스 및 시나리오(TC-22-01 ~ TC-22-05)가 실패 없이 **PASSED** 처리될 것.
