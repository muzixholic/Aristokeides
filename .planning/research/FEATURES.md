# FEATURES.md

이 문서는 Aristokeides 시스템의 주요 기능 요구사항, 차별점, 그리고 개발 범위에서 제외되는 항목들을 정의합니다. 특히 **Milestone v1.1**의 핵심 기능인 **SSH Git 지원** 및 **라인 단위 코드 리뷰(인라인 코멘트)**에 집중하여 세부 항목을 기술합니다.

## Table Stakes (Must-haves)

### 1. 기존 핵심 기능 (v1.0 완료)
*   **Git 저장소 호스팅**: HTTP/HTTPS 프로토콜 기반의 Git Clone/Push/Pull 지원 (Git Smart HTTP).
*   **사용자 인증 및 권한 관리**: 회원가입, 로그인 및 역할 기반(Admin, Contributor, Reader) 권한 제어.
*   **이슈 트래커 및 칸반 보드**: 이슈 생성/조회/수정/삭제, 담당자 지정, 라벨링, HTML5 드래그 앤 드롭 기반 칸반 보드 상태 관리.
*   **기본 코드 리뷰 / 풀 리퀘스트(PR)**: 브랜치 간 비교를 통한 Diff 뷰어, 전체 PR 코멘트 작성 및 기본적인 머지(Merge) 처리.
*   **웹훅(Webhooks)**: 저장소 이벤트 발생 시 외부 시스템으로 이벤트를 전달하는 기본 웹훅 기능.

### 2. SSH Git 지원 (Milestone v1.1 대상)
*   **SSH 키 관리 (SSH Key Management)**
    *   **공개키 CRUD**: 사용자가 자신의 SSH 공개키(`id_rsa.pub`, `id_ed25519.pub` 등)를 프로필 설정 화면에서 등록하고 삭제할 수 있는 기능.
    *   **지원 알고리즘 유효성 검증**: 보안성이 낮은 알고리즘(예: DSA, 2048비트 미만의 RSA)은 등록 단계에서 차단하고, `Ed25519`, `ECDSA`, `RSA` (3072비트 이상) 키 형식만 허용.
    *   **지문(Fingerprint) 자동 계산**: 등록된 공개키의 SHA-256 및 MD5 지문을 자동 계산하여 UI에 표시하여 키 구분을 용이하게 함.
    *   **전역 고유성 검증**: 시스템 전체에서 동일한 SSH 공개키가 중복 등록되지 않도록 고유성 인덱스 및 비즈니스 로직 적용.
*   **SSH Clone/Push/Pull UX**
    *   **SSH Clone URL 제공**: 저장소 페이지에서 SSH 프로토콜 형식의 주소(예: `git@aristokeides.com:username/repo.git` 또는 `ssh://git@aristokeides.com:port/username/repo.git`) 복사 기능 제공.
    *   **SSH 연결 테스트 및 안내**: 사용자가 터미널에서 `ssh -T git@aristokeides.com` 명령을 입력했을 때, 성공적인 인증 메시지(`Hi username! You've successfully authenticated...`)를 리턴하여 연동 성공 여부를 확인할 수 있는 진단 터미널 피드백 제공.
    *   **Git 스마트 SSH 통신 백엔드**: .NET Core 내부 프로세스에서 SSH 서버를 구동(포트 22 또는 커스텀 포트)하여, 공개키 인증을 데이터베이스 사용자 DB와 연동하여 수행. 인증 성공 시 일반 Shell 실행은 금지하고 오직 `git-upload-pack` 및 `git-receive-pack` 명령만 허용하여 입력/출력 스트림을 양방향 중계.
    *   **SSH 접근 권한 제어**: 해당 저장소에 대한 접근 권한(Reader/Contributor)이 없는 사용자가 SSH 접근 시 `Permission denied` 또는 `Repository not found` 에러를 명확하게 반환.

### 3. 라인 단위 코드 리뷰 (Milestone v1.1 대상)
*   **PR Diff 인라인 코멘트 (Line-by-line Comments)**
    *   **Diff 라인 오버 및 작성 폼 활성화**: PR의 파일 변경 비교(Unified/Split Diff) 화면에서 개별 코드 라인에 마우스를 올릴 때 `+` 버튼 표시. 버튼 클릭 시 해당 라인 바로 아래에 에디터 폼 렌더링.
    *   **Markdown 및 실시간 프리뷰**: 코멘트에 마크다운 서식을 지원하며, 작성 중 화면 분할 또는 탭 전환을 통한 실시간 렌더링 프리뷰 제공.
    *   **댓글 작성 워크플로우**:
        *   *단일 댓글 추가 (Add single comment)*: 작성 즉시 즉각 저장 및 알림 전송.
        *   *리뷰 시작 (Start a review)*: 피드백을 임시(Pending) 상태로 보관하며, 리뷰어가 PR 전체 분석을 마친 후 일괄 제출.
    *   **Diff Context 데이터 모델링**: 댓글은 `PullRequestId`, `CommitSha`(작성 당시 Head Commit SHA), `FilePath`, `LineNumber`(또는 `StartLine`, `EndLine`), `Side`(Left/Right), 그리고 댓글이 달린 소스코드 주변 컨텍스트(`DiffHunk`)를 데이터베이스에 보존하여 정확한 위치 매칭 보장.
    *   **코드 변경 시 Outdated 처리**: 새 커밋 푸시로 코멘트가 달린 대상 코드 라인이 수정되거나 삭제될 경우, Git Diff 알고리즘을 사용해 위치를 재추적하고 해당 코멘트 스레드를 자동으로 "아웃데이트됨(Outdated)"으로 상태 변경 후 접힘(Collapse) 표시.
*   **대화 스레드 관리 (Resolving/Unresolving Threads)**
    *   **스레드 답글 피드**: 하나의 인라인 코멘트를 시작으로 여러 사용자가 스레드 형식으로 답글(Reply)을 달 수 있는 토론 구조.
    *   **스레드 해결/미해결 기능**: 피드백이 코드에 반영되거나 토론이 종료되면 리뷰어 혹은 PR 작성자가 "Resolve conversation" 버튼을 클릭하여 스레드를 접음. 필요한 경우 "Unresolve conversation"을 통해 토론을 재개 가능.
    *   **병합 차단 연동**: 모든 대화 스레드가 해결 상태(Resolved)가 되지 않으면 PR의 병합(Merge) 버튼을 비활성화하는 기본 규칙 제공.
*   **리뷰 상태 추적 및 의사결정 (Tracking Reviews)**
    *   **리뷰 제출 및 의사결정 종류**:
        *   *Comment (단순 피드백)*: 승인/반려 없이 단순 질문이나 제안 추가.
        *   *Approve (승인)*: 변경 사항 수락 및 병합 허용.
        *   *Request Changes (변경 요청)*: 치명적 결함 발견 시 수정 요구(해당 상태에서는 병합 불가능).
    *   **리뷰 상태 추적 UI**: PR 화면 우측 영역에 각 리뷰어의 상태(배지: Approved, Changes Requested, Commented)를 일목요연하게 표시.
    *   **신규 커밋 푸시 시 자동 무효화**: 코드가 수정되어 새로운 커밋이 브랜치에 푸시되는 경우, 기존에 받았던 승인(Approve) 상태를 자동으로 취소하여 재검토를 강제하는 옵션 기능.

---

## Differentiators (차별화 요소)

*   **임베디드 형태의 경량 .NET SSH 서버**: 외부 OpenSSH 데몬 등의 OS 인프라에 의존하지 않고, .NET Core 자체 프로세스 안에서 관리되는 독립 실행형 SSH 호스팅 모듈을 구축하여 매우 작고 가벼운 배포 풋프린트 유지 (Gitea와 유사한 단일 실행 바이너리 지향).
*   **Blazor Server 실시간 인터랙션**: SignalR 백플레인을 통한 양방향 연결을 활용하여, 코멘트 추가, 스레드 해결, 리뷰 승인 상태 변경이 새로고침 없이 즉각적으로 모든 협업자의 브랜저 화면에 동기화되는 매끄운 UX 제공.
*   **SSH 연결 진단 및 사용자 셀프 헬프 가이드**: SSH 연동에 어려움을 겪는 사용자를 위해 웹 UI 상에서 현재 공개키 설정 상태를 확인하고, 연결 테스트 명령의 결과를 분석하여 문제점(예: 키 권한 문제, 불일치 등)을 짚어주는 자가 진단 스크립트 및 가이드 제공.
*   **커밋 서명 검증(Commit Signature Verification)**: 사용자의 SSH 키를 이용해 서명된 Git 커밋에 대해 서버 측에서 암호학적 검증을 수행하고 UI에 녹색 "Verified" 보안 배지를 표시하여 공급망 보안성 강화.

---

## Anti-features (도입하지 않는 기능)

*   **인터랙티브 SSH 터미널 쉘 접속 (Interactive SSH Shell)**: 보안 위협과 악성 스크립트 실행을 완벽히 차단하기 위해, 사용자가 SSH를 통해 리눅스/윈도우의 일반 쉘 환경(예: `/bin/sh`, `cmd.exe`)에 진입하는 기능은 완전히 제공하지 않습니다. 오직 Git 전용 서비스 명령어(`git-upload-pack`, `git-receive-pack`)의 터널링만을 허용합니다.
*   **실시간 다중 커서 공동 문서 편집 (Real-time Multi-cursor Doc Editing)**: 위키나 PR 설명 작성 시 동시 다발적인 협업 타이핑 및 실시간 다중 커서 렌더링은 기술적 리소스 소모가 크고 경량화 원칙에 어긋나므로 지원 범위를 벗어납니다.
*   **CI/CD 러너 인프라 및 빌드 에이전트 연동 (CI/CD Pipelines)**: 자체 빌드 및 배포 에이전트 에코시스템을 구성하는 것은 Aristokeides의 핵심 스코프를 초과하므로 대상에서 제외합니다. 대신 Webhook을 통한 외부 CI 연동으로 대체합니다.
*   **외부 OAuth/SSO 통합 로그인 및 SSH 프로비저닝**: 이번 Milestone v1.1에서는 시스템 내부 사용자 및 SSH 키 관리 시스템에 집중하며, Active Directory, LDAP, OAuth 등을 통한 외부 ID 연동은 다루지 않습니다.
