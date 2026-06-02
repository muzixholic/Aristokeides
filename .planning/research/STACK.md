# STACK.md

## 표준 기술 스택 (Git 관리 시스템 - C# / .NET)

- **Framework**: ASP.NET Core 9.0 (고성능 웹 API 및 Blazor Server UI 구동)
- **Git Operations (Local)**: LibGit2Sharp (Native Git 구현인 libgit2의 .NET 바인딩. 로컬 커밋, 머지, 트리 비교 시 프로세스 호출 없이 직접 메모리상에서 고속 처리)
- **Database**: PostgreSQL + Entity Framework Core (GitLab 등 현대적 Git 툴의 표준 백엔드. JSONB 지원을 활용한 메타데이터 관리 최적화)
- **Frontend / Real-time UI**: Blazor Server (Razor 컴포넌트 기반 컴포넌트 재사용 및 SignalR을 통한 실시간 리뷰 스레드 자동 업데이트)
- **Auth**: ASP.NET Core Identity (JWT / Cookie 기반 세션 처리 및 데이터베이스 통합)

---

## v1.1 신규 스택 추가 사항 (Milestone v1.1 Stack Additions)

### 1. 경량 SSH Git 서버 구현 (Lightweight SSH Git Server)
Git 클라이언트(`git clone`, `git push`, `git pull`)와의 SSH 통신 및 SSH Key 검증을 처리하기 위해 .NET 프로세스 내부에 경량 SSH 서버 엔진을 통합합니다.

#### **추천 라이브러리 및 접근 방식**
- **FxSsh (추천 - Pure C# SSH Server)**
  - **특징**: 경량의 오픈소스 Pure C# SSH 서버 구현체로, .NET 8/9 환경에서 가볍게 구동 가능합니다.
  - **선정 이유**: `EXEC` 명령어 채널을 완벽하게 가로챌 수 있어 Git 클라이언트가 던지는 원격 명령(`git-upload-pack`, `git-receive-pack`)을 분석하고 가로채기에 가장 최적화되어 있습니다.
  - **대안**:
    - *Rebex File Server* (상용): 완성도 높은 엔터프라이즈 솔루션이나 라이선스 비용 문제로 제외.
    - *Hexa.NET.Libssh2* (P/Invoke Wrapper): 성능과 최신 암호화 호환성은 우수하나 플랫폼 종속성 및 메모리 관리 복잡도가 증가하여 보류.
    - *Smart HTTP (대안 아키텍처)*: 만약 SSH 프로토콜의 복잡도로 인해 개발 기간 단축이 필요한 경우, ASP.NET Core 미들웨어에서 Git Smart HTTP 사양(POST `/git-upload-pack`, GET `/info/refs?service=git-upload-pack` 등)을 직접 구현하는 방식도 충분히 고려할 수 있습니다.

#### **Git I/O 파이핑 아키텍처**
- SSH.NET 또는 LibGit2Sharp 자체는 Git 원격 트랜스포트 프로토콜(Smart Protocol)의 서버 역할을 기본 제공하지 않습니다.
- 따라서 FxSsh 서버 내에서 클라이언트 접속 및 인증이 완료되면, 클라이언트가 요청한 명령어(`git-upload-pack '<repo-path>'` 등)를 파싱합니다.
- `System.Diagnostics.Process`를 통해 호스트의 `git` 실행 파일을 구동하고, **SSH 채널 스트림의 입력/출력**을 **Git 프로세스의 StandardInput / StandardOutput**과 비동기적으로 상호 파이핑(`Stream.CopyToAsync`)하여 프록시 역할을 수행합니다.

#### **SSH 키 관리 (SSH Key Management)**
- **DB 스키마**: PostgreSQL에 `SshKey` 엔티티(ID, UserID, Title, Fingerprint, PublicKeyContent, CreatedAt)를 설계하여 관리합니다.
- **인증 핸들링**: 사용자가 업로드한 OpenSSH 포맷의 Public Key(`ssh-rsa ...`, `ssh-ed25519 ...`)를 등록할 때 유효성을 검증하고 Fingerprint를 추출하여 DB에 저장합니다. FxSsh의 `UserAuth` 이벤트 발생 시 클라이언트가 제공한 공개키 서명을 대조하여 사용자를 인증합니다.

---

### 2. 라인별 코드 리뷰 및 Diff 뷰 (Line-by-line Code Review & Diff)
PR(Pull Request) Diff 화면에서 각 라인별로 스레드화된 코멘트를 달고, 실시간으로 리뷰를 동기화하기 위한 텍스트 비교 및 하이라이팅 스택입니다.

#### **추천 라이브러리 및 접근 방식**
- **TextDiff.Sharp + LibGit2Sharp (Diff 생성 및 파싱)**
  - **LibGit2Sharp**: 두 Git 커밋 또는 브랜치 트리 사이의 차이점(Patch) 데이터를 Native 성능으로 생성합니다.
  - **TextDiff.Sharp (추천 - Unified Diff Parser)**: LibGit2Sharp에서 추출된 Unified Diff 포맷 및 Git extended headers를 고성능 스트리밍 방식으로 파싱하여 구조화된 C# 객체(Hunk, Line Number, Line Content, Line Type 등)로 제공합니다.
  - **DiffPlex (비교 엔진)**: 텍스트 수준에서의 세부 비교(Character-level Diff)가 필요한 파일 비교 뷰어 개발 시 활용합니다.

- **UI 렌더링 및 문법 강조 (Blazor UI & Client-side Highlight)**
  - **BlazorTextDiff (Blazor Wrapper Component)**: DiffPlex 기반의 사이드-바이-사이드(Side-by-side) 및 인라인(Inline) UI를 쉽게 렌더링하도록 돕는 오픈소스 UI 컴포넌트입니다.
  - **Highlight.js (JS Interop)**: Diff가 렌더링된 뒤, 소스 코드 본래의 문법을 아름답게 강조(Syntax Highlighting)하기 위해 Blazor JS Interop을 통해 Highlight.js 라이브러리를 클라이언트 측에서 동적 로드 및 적용합니다.

#### **인라인 코멘트 스레드 매핑 아키텍처**
- **데이터베이스 영속성**: `PullRequestComment` 테이블을 설계합니다.
  - `FilePath` (대상 파일 경로)
  - `LineType` (Old / New / Unchanged 상태)
  - `OriginalLineNumber` (이전 파일 기준 라인 번호, 삭제/변경 라인 추적용)
  - `NewLineNumber` (새 파일 기준 라인 번호, 추가/변경 라인 추적용)
  - `Content` (마크다운 기반 코멘트 본문)
  - `ParentCommentId` (대댓글/스레드 구조를 구현하기 위한 자가 참조 키)
- **UI 동기화**: Blazor Server의 SignalR 커넥션을 통해, 한 리뷰어가 특정 라인에 댓글을 입력하고 저장하면, 해당 PR을 보고 있는 다른 리뷰어들의 화면에 페이지 리로드 없이 실시간으로 댓글 스레드가 렌더링됩니다.

---

## 도입 시 금지 / 제한 사항 (What NOT to use)

- **원시적인 `git.exe` 호출을 통한 로컬 저장소 변경**:
  - 로컬 Git 제어(Commit, Branch, Tag)에는 반드시 **LibGit2Sharp**을 사용하십시오. `Process.Start("git.exe")`는 호출 비용이 크고 플랫폼 종속적이며 인젝션 위험이 큽니다.
  - *예외*: SSH Git 서버 서비스 동작을 위한 `git-upload-pack`, `git-receive-pack` 등 I/O 스트림 파이핑 동작 시에만 엄격한 경로 검증(Sanitizer)과 샌드박싱 하에 로컬 `git` 실행 파일을 보조적으로 사용합니다.
- **클라이언트 사이드에서의 전체 Diff 연산**:
  - 대용량 파일 또는 다량의 커밋 비교 시 Diff 생성 및 파싱 연산은 서버 백엔드(Blazor Server의 C# 메모리 또는 백그라운드 태스크)에서 수행한 후, 가공된 스니펫 구조만 클라이언트로 송출하여 브라우저 메모리 고갈과 UI 블로킹을 방지해야 합니다.
- **무거운 단일 페이지 프레임워크(React, Angular 등) 도입**:
  - Gitea처럼 경량 지향의 Git 웹 포털을 유지하기 위해 Blazor Server를 기본으로 가져가며, 추가적인 복잡성을 발생시키는 무거운 SPA 연동 프레임워크 도입은 지양합니다.

