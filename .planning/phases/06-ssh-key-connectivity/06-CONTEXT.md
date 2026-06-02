# Phase 6 Context: SSH Key & Connectivity

이 문서는 **Phase 6 (SSH Key & Connectivity)** 진행을 위해 결정된 사용자 설계 선호도 및 제약 사항을 기술합니다. 이 정보는 이후 리서치 및 세부 계획 수립의 강제 규칙으로 작용합니다.

---

## 🎯 Domain Boundary (도메인 경계)
이 단계는 SSH 공개키 등록/삭제를 관리하는 기능과 FxSsh 기반의 독자적인 경량 임베디드 SSH 서버 모듈을 구축하는 데 집중합니다. 사용자가 SSH 프로토콜로 리포지토리를 안전하게 클론 및 푸시/풀할 수 있도록 하는 것이 목표입니다.

---

## 🔒 Decisions (설계 결정 사항)

### 1. SSH 포트 설정 및 Clone URL 형식
- **기본 포트**: `2222`번 포트를 기본으로 사용하되, `appsettings.json` 설정에 따라 가변적으로 바인딩 포트를 조절할 수 있도록 설계한다.
- **Clone URL 형식**: 저장소 메인 화면의 UI에 노출되는 복사용 URL은 포트 번호가 명시적으로 기입된 `ssh://git@domain:2222/{username}/{repo}.git` 표준 포맷으로 제공한다.

### 2. SSH 키 관리 및 이름(Label) 부여 방식
- **키 자동 파싱**: 공개키 등록 시 사용자가 직접 라벨을 입력하지 않아도 공개키 텍스트 끝에 명시된 주석(Comment, 예: `user@hostname`)을 자동 추출하여 라벨 필드의 기본값으로 세팅해 준다.
- **수정 허용**: 자동 제안된 키 이름은 사용자가 등록하기 전에 자유롭게 원하는 형태로 수정하여 등록할 수 있도록 지원한다.

### 3. 'ssh -T' 터미널 연결 검증 메시지
- **인증 피드백**: 사용자가 로컬 콘솔에서 `ssh -T git@domain` 명령을 실행해 서버 연결을 테스트할 때 다음 규격화된 표준 환영 메시지만 간단히 응답한다:
  ```text
  Hi [Username]! You've successfully authenticated, but Aristokeides does not provide shell access.
  ```

### 4. 웹 UI (Blazor) 설정 메뉴 구성
- **UI 배치**: 새로운 페이지를 추가하여 사이드바 메뉴를 늘리기보다는, 기존의 프로필/계정 설정 화면(User Profile) 내에 'SSH Keys' 탭을 추가하여 인라인으로 등록 폼과 공개키 리스트 목록을 렌더링한다. (기존 레이아웃 및 스타일 코드 재사용 극대화)

---

## 🔗 Canonical Refs (참조 문서 규격)
아래 문서들은 Phase 6 구현을 위해 반드시 참고해야 하는 명세입니다:
- [REQUIREMENTS.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/REQUIREMENTS.md#L4-L20) — SSH-01부터 SSH-06까지의 세부 요건 명세
- [research/SUMMARY.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/research/SUMMARY.md) — 임베디드 SSH 데몬 연동 및 Git 명령어 상호 파이핑에 대한 통합 리서치 결과
- [research/STACK.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/research/STACK.md) — FxSsh 라이브러리 선정 및 CLI git 바이너리 연동 세부 스택
- [research/ARCHITECTURE.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/research/ARCHITECTURE.md) — SshKeys 테이블 모델링 및 FxSsh IHostedServiceBackground 서비스 연동 구조

---

## 🛠 Code Context (재사용 가능한 코드 및 패턴)
- **AppDbContext**: 유저 테이블(`Users`)과 1:N 관계를 가질 신규 `SshKey` 모델의 마이그레이션 추가 시 기존 DbContext 설정 양식을 준수한다.
- **Account Settings CSS**: 기존 프로필 페이지의 탭 구조 및 버튼 스타일을 재사용하여 일관된 룩앤필(Look and Feel)을 유지한다.
