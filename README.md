# Aristokeides (아리스토케이데스)

C# / .NET 10 기반의 뛰어난 성능을 바탕으로 한 **경량 설치형 Git 저장소 호스팅 및 프로젝트 관리 시스템**입니다. GitLab이나 Gitea와 유사한 핵심 협업 기능(Git HTTP/SSH 호스팅, 이슈 트래커, 칸반 보드, 풀 리퀘스트 및 코드 리뷰)을 독립적이고 가볍게 제공하는 것을 목표로 합니다.

---

## 🚀 주요 기능

### 1. 사용자 인증 및 권한 관리 (Auth)
- **웹 기반 인증 시스템**: 웹 브라우저를 통해 직관적으로 회원가입, 로그인, 로그아웃을 수행할 수 있는 전용 UI를 제공합니다.
- **하이브리드 인증**: API 통신을 위한 JWT Bearer 토큰 인증과 웹 브라우저 접속을 위한 Cookie 인증 방식을 통합 제공합니다.
- **Git Basic Auth**: Git CLI 클라이언트의 HTTP `clone`, `push`, `pull` 요청 시 Basic Authentication을 지원합니다.
- **역할 기반 권한**: `Admin`, `Contributor`, `Reader` 등 3가지의 명확한 역할(Role) 구분을 지원합니다.

### 2. Git HTTP/SSH 호스팅 (Git Hosting)
- **Git Smart HTTP**: Git 표준 Smart HTTP 프로토콜(`git-receive-pack`, `git-upload-pack`)을 커스텀 미들웨어로 구현하여 Git 클라이언트를 통한 직접적인 `clone`, `push`, `pull`이 가능합니다.
- **Git Smart SSH**: `FxSsh` 기반의 경량 임베디드 SSH 서버를 내장하여, SSH 공개키 기반의 안전한 Git Clone, Push, Pull 작업을 지원합니다.
- **보안 및 제어**: SSH 접속 시 대화형 셸(Shell) 요청은 철저히 차단하며, 오직 허가된 Git 명령어(`git-upload-pack`, `git-receive-pack`)만 OS의 Git 프로세스와 비동기 스트림 파이핑을 중계하도록 제한하였습니다.
- 리포지토리 생성 작업은 백그라운드 큐 및 워커 시스템을 통해 비동기식으로 안정적으로 수행됩니다.

### 3. SSH 키 등록 및 관리 (SSH Key Management)
- **프로필 설정 통합**: 사용자는 웹 UI의 프로필 설정 화면에서 자신의 SSH 공개키(Ed25519, ECDSA, RSA-3072+ 알고리즘)를 손쉽게 등록 및 제거할 수 있습니다.
- **지문 및 유효성 검증**: 등록 시 공개키 형식을 검증하고 지문(SHA-256 Fingerprint)을 자동 추출하여 시각적으로 보여줍니다.
- **진단 기능**: 로컬 터미널에서 `ssh -T -p 2222 git@domain` 명령을 통해 SSH 연결 검증을 테스트할 수 있으며, 성공 시 해당 사용자 명의의 환영 진단 메시지를 반환합니다.

### 4. 저장소 브라우저 (Repository Browser)
- 웹 인터페이스를 통해 프로젝트 저장소를 탐색할 수 있습니다.
- 브랜치 목록 조회, 커밋 히스토리 추적, 디렉토리 구조 탐색 및 개별 파일 뷰어 기능이 포함되어 있습니다.
- `highlight.js` CDN을 연동하여 웹 브라우저 내에서 다양한 프로그래밍 언어의 소스 코드를 구문 강조(Syntax Highlighting)하여 시각화합니다.
- 저장소 메인 화면에서 HTTP와 SSH 방식의 클론(Clone) URL을 토글하여 쉽게 복사할 수 있는 UI를 탑재했습니다.

### 5. 이슈 트래커 & 칸반 보드 (Issues & Kanban)
- 각 저장소별로 독립적인 이슈 관리를 지원합니다. (생성, 상세 조회, 수정, 닫기 기능)
- HTML5 드래그 앤 드롭 API를 적용한 **실시간 대화형 칸반 보드**를 탑재하여 이슈 진행 상태를 시각적으로 조정할 수 있습니다.
- 이슈 본문 및 댓글을 통해 프로젝트 참가자들 간의 의견 조율이 가능합니다.

### 6. 풀 리퀘스트 & 코드 리뷰 (Pull Request & Code Review)
- **브랜치 병합**: 브랜치 간의 코드 병합을 위한 Pull Request 기능 및 병합 충돌 여부 체크 기능을 제공합니다.
- **라인 단위 인라인 댓글**: PR 파일 변경 Diff 화면에서 코드의 개별 라인에 마크다운 및 실시간 프리뷰를 지원하는 인라인 댓글을 달 수 있고, 대댓글 작성 및 토론 해결(Resolve)/재개(Unresolve)를 지원합니다.
- **임시 보관 및 일괄 리뷰 제출**: 댓글을 Pending 상태로 모아두었다가, 요약 설명글과 함께 리뷰 의견(`Comment`, `Approve`, `Request Changes`)을 지정하여 한 번에 일괄 제출(Submit review)할 수 있습니다.
- **라인 번호 자동 보정 (Line Shift)**: 소스 브랜치에 신규 커밋이 푸시될 때 기존 댓글의 줄 위치를 Myers/Hunk 매핑에 기반해 자동으로 올바르게 밀어주고(Line Shift), 코드가 수정/삭제되어 유실된 경우에는 해당 스레드를 `Outdated` 상태로 자동 전환 및 아코디언 접기 렌더링합니다.
- **병합 차단 및 관리자 우회**: PR에 해결되지 않은(Unresolved) 토론이 1개라도 존재하는 경우 일반 사용자의 병합(Merge)을 엄격히 차단하되, 관리자(Admin) 권한을 가진 사용자에게만 우회 강제 병합(Force Merge) 옵션을 체크박스를 통해 제공합니다.
- **승인 자동 리셋**: 소스 브랜치에 새 커밋이 푸시되면 기존 리뷰어들의 승인(`Approved`) 상태를 시스템에서 자동으로 취소(`Dismissed` 전환)하여 소스 변경에 따른 전면 재검토를 유도합니다.

### 7. 대시보드 및 웹 UI 저장소 관리 (Web Dashboard & Repo Management)
- **랜딩 페이지 및 대시보드**: 비로그인 시 프로젝트를 소개하는 랜딩 페이지를 제공하며, 로그인 시 자신이 접근 가능한 리포지토리 목록을 카드 뷰 형태로 보여주는 대시보드로 자동 연결됩니다.
- **웹 기반 저장소 생성/관리**: 웹 브라우저 인터페이스를 통해 직관적으로 새로운 Git 저장소를 생성할 수 있습니다.
- **설정 및 안전한 삭제**: 기존 저장소의 이름, 설명, 비공개 여부 등을 웹에서 즉시 수정할 수 있으며, 이중 확인 모달을 통해 물리적인 Git 저장소와 데이터베이스를 일괄적으로 안전하게 영구 삭제할 수 있습니다.
- **통합 네비게이션**: 글로벌 네비게이션 바와 푸터를 통해 시스템 내 어느 페이지에서든 주요 기능으로 쉽게 이동할 수 있는 일관된 UI/UX를 제공합니다.

### 8. 다중 데이터베이스 지원 (Multi-Database)
- 시스템 환경에 맞춰 **SQLite, PostgreSQL, MySQL** 중 원하는 데이터베이스를 선택하여 사용할 수 있습니다.
- 소규모 팀이나 개인 용도로는 별도의 서버 구성이 필요 없는 SQLite를, 대규모 또는 엔터프라이즈 환경에서는 PostgreSQL이나 MySQL을 선택하여 유연하게 대처할 수 있습니다.

### 9. 최초 설치 마법사 (Setup Wizard) & 시스템 설정
- 최초 실행 시 자동으로 나타나는 웹 기반 설치 마법사(`/setup`)를 통해 직관적으로 데이터베이스를 연결하고 초기 관리자(Admin) 계정을 생성할 수 있습니다.
- 관리자 권한을 가진 사용자는 웹 UI 내에서 전역 시스템 설정(Admin Settings)을 확인하고 조정할 수 있습니다.

### 10. Docker 및 배포 환경 (Docker Deployment)
- 애플리케이션 이미지 빌드를 위한 최적화된 `Dockerfile`과 서비스 구동을 위한 `docker-compose.yml`이 제공됩니다.
- 볼륨 마운트를 통한 데이터 영속성 유지 및 컨테이너 기반 배포를 완벽히 지원하여 손쉽게 호스팅 환경을 구성할 수 있습니다.

---

## 🛠 기술 스택

- **Backend**: C# / .NET 10.0 (ASP.NET Core)
- **Frontend**: Blazor Server (Interactive Server Render Mode), Vanilla CSS
- **Database**: Entity Framework Core (SQLite, PostgreSQL, MySQL 다중 지원)
- **Git Engine**: LibGit2Sharp 연동 및 Git Smart HTTP 프로토콜 수동 파싱
- **SSH Engine**: `FxSsh` 기반 임베디드 SSH 서버 (ECDsa 호스트 키 알고리즘 적용 및 백그라운드 호스팅)
- **API Doc**: Swagger UI (JWT Bearer Security 정의 적용)

---

## 📂 주요 코드 구조

- **진입점 및 미들웨어**:
  - [Program.cs](./Aristokeides.Api/Program.cs): 애플리케이션 파이프라인, DI 컨테이너 및 미들웨어 등록
  - [GitSmartHttpMiddleware.cs](./Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs): Git CLI HTTP 요청 처리 미들웨어
- **SSH 서버 및 연결 중계**:
  - [SshServerBackgroundService.cs](./Aristokeides.Api/Services/Ssh/SshServerBackgroundService.cs): `FxSsh` 엔진 구동 및 세션 관리 백그라운드 서비스
  - [SshCommandBridge.cs](./Aristokeides.Api/Services/Ssh/SshCommandBridge.cs): 일반 셸 제한 및 Git SSH 명령어 실행/비동기 스트림 파이핑 중계
  - [SshKeyParser.cs](./Aristokeides.Api/Services/Ssh/SshKeyParser.cs): 공개키 유효성 분석 및 지문 생성 도구
  - [SshFingerprintCalculator.cs](./Aristokeides.Api/Services/Ssh/SshFingerprintCalculator.cs): SHA-256 지문 계산 유틸리티
  - [SshSignatureVerificationService.cs](./Aristokeides.Api/Services/Ssh/SshSignatureVerificationService.cs) & [SshSignatureVerifier.cs](./Aristokeides.Api/Services/Ssh/SshSignatureVerifier.cs): SSH 키 기반 커밋 디지털 서명 분석 및 검증 처리기
- **비즈니스 서비스**:
  - [GitBrowserService.cs](./Aristokeides.Api/Services/GitBrowserService.cs): Git 저장소 데이터(커밋, 브랜치, 파일) 조회 서비스
  - [IssueService.cs](./Aristokeides.Api/Services/IssueService.cs): 이슈 상태 제어 및 칸반 정렬 서비스
  - [PullRequestService.cs](./Aristokeides.Api/Services/PullRequestService.cs): PR 생성, Diff 분석 및 병합 처리, 일괄 리뷰 제출, 커밋 푸시 후처리(라인 보정 및 승인 초기화) 서비스
- **데이터 레이어**:
  - [AppDbContext.cs](./Aristokeides.Api/Data/AppDbContext.cs): PostgreSQL 연결 및 엔티티 매핑 관계 설정 (EF Core)
  - [Models/PullRequestReview.cs](./Aristokeides.Api/Models/PullRequestReview.cs): PR 리뷰 상태 저장 엔티티 모델 (신규)
  - [Models/PullRequestReviewComment.cs](./Aristokeides.Api/Models/PullRequestReviewComment.cs): PR 라인별 인라인 댓글/답글 저장 모델
  - [Models/](./Aristokeides.Api/Models): User, Repository, Issue, PullRequest, SshKey, CommitSignature 등 주요 도메인 모델 정의
- **컨트롤러**:
  - [SshKeysController.cs](./Aristokeides.Api/Controllers/SshKeysController.cs): SSH 키 목록 조회, 등록, 삭제를 제공하는 API 컨트롤러
- **사용자 인터페이스 (Blazor Components)**:
  - [Components/Pages/](./Aristokeides.Api/Components/Pages): 전체 화면 뷰 컴포넌트들
    - [Home.razor](./Aristokeides.Api/Components/Pages/Home.razor) & [Dashboard.razor](./Aristokeides.Api/Components/Pages/Dashboard.razor) : 비로그인/로그인 세션 기반의 랜딩 페이지 및 저장소 목록 뷰
    - [NewRepository.razor](./Aristokeides.Api/Components/Pages/NewRepository.razor) & [RepositorySettings.razor](./Aristokeides.Api/Components/Pages/RepositorySettings.razor) : 웹 UI 기반의 신규 저장소 생성 및 설정 변경/삭제 페이지
    - [Settings.razor](./Aristokeides.Api/Components/Pages/Settings.razor) : 프로필 정보 및 SSH 키 등록/삭제를 지원하는 사용자 설정 화면
    - [RepoBrowser.razor](./Aristokeides.Api/Components/Pages/RepoBrowser.razor) : HTTP/SSH 클론 URL 선택 및 파일 목록 표시
    - [RepoPullRequestDetail.razor](./Aristokeides.Api/Components/Pages/RepoPullRequestDetail.razor) : PR 코드 디프, 임시 댓글 작성 및 일괄 제출 UI, 머지 제어 및 관리자 우회 동의 UI
    - `RepoBlob.razor` (구문 강조 코드 뷰어), `RepoIssues.razor` (칸반 보드)
- **단위 및 통합 테스트**:
  - [AdvancedReviewTests.cs](./Aristokeides.Tests/Services/AdvancedReviewTests.cs): 일괄 리뷰 제출 및 병합 차단, 푸시 후처리 라인 보정 알고리즘 통합 검증 (신규)
  - [PushHookIntegrationTests.cs](./Aristokeides.Tests/PushHookIntegrationTests.cs) : Push 완료 후 서명 검증 테스트

---

## ⚙️ 실행 방법

### 1. 전제 조건
- .NET 10.0 SDK 설치
- 시스템 경로에 `git` 실행 파일 등록 필요
- (선택) PostgreSQL, MySQL 등 외부 데이터베이스 혹은 Docker 환경 준비 (SQLite 사용 시 기본 내장됨)

### 2. 설정 조정 (`appsettings.json`)
웹 브라우저를 통해 실행되는 **최초 설치 마법사(Setup Wizard)**를 통해 손쉽게 DB 및 초기 계정을 구성할 수 있습니다. 혹은 [appsettings.json](./Aristokeides.Api/appsettings.json)을 직접 수정하여 DB 종류(`DatabaseProvider`)와 연결 문자열을 설정할 수도 있습니다.
```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=aristokeides.db"
  },
  "Ssh": {
    "Port": 2222,
    "HostKeyPath": "ssh_host_key.pem"
  }
}
```

### 3. 애플리케이션 실행 및 마이그레이션 적용
애플리케이션 구동 시 또는 Setup 완료 직후, 선택한 DB Provider에 맞춰 자동으로 마이그레이션이 실행됩니다. 직접 로컬 환경에서 실행하려면 다음 명령어를 사용합니다.

```powershell
# API 프로젝트 실행
dotnet run --project Aristokeides.Api
```
(실행 후 `http://localhost:5000` 등으로 접속하면 초기 설치 화면이 나타납니다.)

### 4. SSH 연결 테스트 및 사용
- **연결 확인**: 서버가 실행된 상태에서 다음 명령어를 실행하여 SSH 작동을 확인합니다.
  ```powershell
  ssh -T -p 2222 git@localhost
  ```
  성공 시 `Hi {Username}! You've successfully authenticated, but Aristokeides does not provide shell access.` 와 같은 안내 메시지가 표시됩니다.
- **Git Clone 예시**:
  ```powershell
  git clone ssh://git@localhost:2222/{username}/{repo}.git
  ```
