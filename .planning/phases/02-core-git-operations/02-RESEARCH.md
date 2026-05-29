# Phase 2: Core Git Operations - Research

## 1. 개요 (Overview)
본 연구는 Phase 2의 핵심 요구사항인 "LibGit2Sharp를 이용한 Git Smart HTTP 및 저장소 생성 (REPO-01, REPO-02)"을 구현하기 위한 기술적 접근 방식을 정리합니다. D-01~D-03 결정 사항을 준수하며, ASP.NET Core 환경에서 `git-http-backend`를 이용한 Smart HTTP 프록시 구현과 `LibGit2Sharp`를 활용한 비동기 저장소 생성에 초점을 맞춥니다.

## 2. 요구사항 분석 (Requirements Analysis)
- **REPO-01**: 사용자가 새로운 빈 Git 저장소를 생성할 수 있어야 함. (D-03: 생성 시 DB 상태를 '생성 중'으로 기록하고 백그라운드에서 디렉토리 생성)
- **REPO-02**: 사용자가 Git Smart HTTP를 통해 clone, push, pull 할 수 있어야 함. (D-02: Git 클라이언트 인증은 이메일/비밀번호를 사용하는 Basic Auth 유지)
- **D-01 (구조)**: `{username}/{repo_name}.git` 형태로 파일 시스템에 저장.

## 3. 기술적 구현 방안 (Technical Approach)

### 3.1. User 모델 변경 및 Repository 엔터티 추가
- **Username 필드 추가 검토**: 저장소 경로 패턴(`{username}/{repo_name}.git`)을 지원하기 위해 `User` 엔터티에 `Username` 필드를 추가하고 Migration을 진행합니다. (현재 Phase 1에는 Email 필드만 존재)
- **Repository 엔터티**: `Id`, `Name`, `Description`, `OwnerId`, `Status` (Creating, Ready, Error), `CreatedAt` 등을 포함하는 엔터티를 구성합니다.

### 3.2. 저장소 생성 로직 (LibGit2Sharp & Background Task)
- **비동기 큐 (Channel)**: ASP.NET Core의 `Channel<T>` 기반 `IHostedService`(`BackgroundService`)를 사용하여 디렉토리 생성(I/O) 작업을 백그라운드로 오프로드합니다.
- **API 흐름**:
  1. `POST /api/repositories` 호출.
  2. DB에 'Creating' 상태로 레코드 저장 후 큐에 Task 전달, API는 `202 Accepted` 응답 반환.
  3. 백그라운드 워커가 큐에서 항목 추출.
  4. `LibGit2Sharp.Repository.Init("절대경로/username/repo.git", isBare: true)` 호출하여 bare 저장소 생성.
  5. 처리 성공 시 DB 상태를 'Ready'로 업데이트.

### 3.3. Git 클라이언트 인증 (Basic Auth Middleware)
- ASP.NET Core는 기본적으로 Basic Auth 핸들러가 없으므로, `AuthenticationHandler<AuthenticationSchemeOptions>`를 상속받은 `BasicAuthenticationHandler`를 구현합니다.
- Git 클라이언트가 전송하는 `Authorization: Basic [base64(email:password)]` 헤더를 파싱하여, 기존 Phase 1에서 구현된 BCrypt 로직으로 DB와 대조 및 인증을 수행합니다.

### 3.4. Git Smart HTTP 미들웨어 및 CGI 파싱 (git-http-backend)
Git 클라이언트의 요청을 처리하기 위해, ASP.NET Core 미들웨어(또는 Controller 기반)를 구축하여 `git.exe http-backend` 명령을 CGI 형태로 프록시합니다.
- **라우팅 대상**: `/{username}/{repo}.git/info/refs`, `git-upload-pack`, `git-receive-pack`
- **프로세스 실행 (ProcessStartInfo)**: 
  - `FileName`: `git.exe`
  - `Arguments`: `http-backend`
  - 환경변수 주입:
    - `GIT_PROJECT_ROOT`: 모든 저장소의 루트 경로 (예: `C:\GitRepos`)
    - `PATH_INFO`: HTTP Request Path (예: `/{username}/{repo}.git/info/refs`)
    - `GIT_HTTP_EXPORT_ALL`: `1` (앱에서 인증을 마쳤으므로 모두 허용)
    - `REMOTE_USER`, `REQUEST_METHOD`, `QUERY_STRING`, `CONTENT_TYPE` 등.
- **세부 파싱 로직 (CGI 스트림 처리)**:
  - `Request.Body`를 `Process.StandardInput`으로 복사.
  - **헤더 파싱**: `Process.StandardOutput`을 라인 단위로 읽어 첫 빈 줄(`\r\n\r\n`)이 나오기 전까지 `Content-Type`, `Status` 등의 CGI HTTP 헤더를 ASP.NET `Response.Headers`에 매핑합니다. (이 과정이 '세부 파싱 로직'의 핵심입니다)
  - **본문 스트리밍**: 빈 줄 이후의 나머지 바이너리 데이터(packfile 등)를 `Response.Body`로 바로 스트리밍합니다.

## 4. 리스크 및 고려사항 (Risks & Considerations)
- **의존성 (git.exe)**: 배포 서버에 Git이 설치되어 있어야 하며 환경 변수 등을 통해 `git.exe` 실행 경로에 접근 가능해야 합니다.
- **Path Traversal 보안**: 악의적인 `{username}`이나 `{repo}` 경로 입력(예: `../`)을 방어하기 위해 미들웨어 진입 시 엄격한 경로 문자열 검증을 수행해야 합니다.
- **동시성 처리**: CGI 스트림 복사 과정에서 서버 리소스 고갈을 막기 위해 `CopyToAsync`와 적절한 버퍼 처리를 사용해야 합니다.

## 5. 결론 (Conclusion)
Phase 2 계획 수립을 위한 조사가 완료되었습니다. 저장소의 논리적 생성 및 로컬 파일시스템(bare 저장소) 구성은 LibGit2Sharp과 Background Task를 활용하며, 실제 Git 네트워크 프로토콜(Smart HTTP) 통신은 `git-http-backend`와 커스텀 프록시 미들웨어를 통해 안정적으로 구현할 수 있습니다.
