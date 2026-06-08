# Phase 12: Repository Management UI - Research Notes

## 1. 현재 상태 및 코드 분석

- **저장소 생성 기전:**
  - 현재 `RepositoriesController.cs`는 `Create` API를 제공합니다.
  - 이 API는 DB에 `Repository`와 기본 칸반 보드 컬럼(`To Do`, `In Progress`, `Done`)을 생성한 후 `RepositoryCreationChannel`을 통해 비동기 생성 큐에 등록합니다.
  - 백엔드 서비스 `RepositoryCreationBackgroundWorker`가 이 큐를 읽어 실제 물리 경로 `GitRepos/{Username}/{RepoName}.git` 폴더에 베어(Bare) 저장소를 초기화(`LibGit2Sharp.Repository.Init`)하고 상태를 `Ready`로 전환합니다.
- **의존성 & 인프라:**
  - Blazor Server 환경에서 DbContext를 직접 주입받아 데이터베이스 제어가 가능합니다.
  - 저장소 삭제 및 이름 변경 시, DB 변경뿐 아니라 실제 로컬 파일 시스템(`GitRepos` 디렉토리) 내의 물리 Git 베어 리포지토리 폴더도 함께 조작해야 합니다.

## 2. 세부 기능 구현 방안

### A. 신규 저장소 생성 (`/repositories/new`)
- **페이지 위치:** `Aristokeides.Api/Components/Pages/NewRepository.razor` (신규 생성)
- **접근 권한:** 로그인 필요 (`@attribute [Authorize]`)
- **구현 방식:**
  - Blazor `<EditForm>`과 `DataAnnotationsValidator`를 사용해 입력 폼(이름, 설명, 비공개 여부) 구성.
  - 이름 유효성 검증 규칙: 영문, 숫자, 대시(`-`), 언더바(`_`)만 허용하며 공백 불가.
  - `Submit` 시 DB 중복 체크 (해당 사용자의 소유 중 동명 저장소가 있는지 여부).
  - 중복이 없고 유효할 경우, `RepositoriesController`의 생성 로직을 차용하여 DB에 저장소 삽입 및 기본 보드 컬럼 삽입 후 `RepositoryCreationChannel`에 Enqueue.
  - 완료 시 생성된 리포지토리 홈(`/{Username}/{RepoName}`) 또는 대시보드(`/dashboard`)로 리다이렉트.

### B. 저장소 설정 관리 (`/{Username}/{RepoName}/settings`)
- **페이지 위치:** `Aristokeides.Api/Components/Pages/RepositorySettings.razor` (신규 생성)
- **접근 권한:** 저장소 소유자(Owner) 또는 관리자(Admin) 권한 확인 후 진입 허용.
- **구현 방식:**
  - 기본 정보(이름, 설명, 가시성 `IsPrivate`) 수정 폼 제공.
  - 이름 변경 시 처리 사항 (이름이 변경된 경우):
    - 새로운 이름의 중복성 검사.
    - 로컬 디렉토리 이름 변경: `GitRepos/{Username}/{OldName}.git` -> `GitRepos/{Username}/{NewName}.git`.
    - 디렉토리 이동 작업은 `Directory.Move`를 활용하며, 예외 상황(이동 실패 등)에 대비한 트랜잭션/복구 고려.
  - 변경 완료 시 설정 페이지 상단에 녹색 성공 박스 노출 (D-03 의사결정 준수).

### C. 저장소 삭제 (설정 페이지 하단 "Danger Zone")
- **구현 방식:**
  - 설정 페이지 하단에 빨간색 테두리의 "Danger Zone" 구역 배치.
  - "Delete Repository" 버튼 클릭 시 Blazor 내장 모달 혹은 CSS 모달 팝업 실행.
  - 모달 내에 `"삭제를 확인하려면 {Username}/{RepoName}을(를) 입력하세요"` 안내 문구 배치.
  - 입력한 텍스트가 정확히 일치할 때만 "삭제" 버튼 활성화 (D-01 의사결정 준수).
  - **삭제 실행 시 로직:**
    - 데이터베이스에서 해당 `Repository` 레코드 삭제 (이슈, PR, 보드 컬럼 등이 종속되어 있으므로 Cascade 삭제 연동 확인 필요. EF Core 스키마 설정 상 Cascade 삭제가 지정되어 있는지 확인 후, 미비 시 레코드 직접 일괄 삭제).
    - 로컬 디렉토리 `GitRepos/{Username}/{RepoName}.git` 폴더를 재귀적으로 삭제 (`Directory.Delete(path, true)`).
    - 삭제 성공 시 `/dashboard`로 리다이렉트.

## 3. 리스크 및 주의 사항 (Landmines)

- **물리 디렉토리 이동/삭제 시 권한 및 잠금 문제:**
  - Git 라이브러리나 SSH 서버가 해당 베어 저장소 파일 리소스를 잡고 있을 경우 `Directory.Move` 또는 `Directory.Delete` 시 `IOException (Access Denied)`이 발생할 수 있습니다.
  - 따라서 파일 조작 실패 시 적절히 오류 메시지를 사용자에게 노출하고 DB 롤백을 수행해야 합니다.
- **Cascade Delete 제약 조건:**
  - `Repository` 엔티티가 지워질 때 `BoardColumn`, `Issue`, `PullRequest`, `CommitSignature` 등의 테이블 레코드들이 정상적으로 삭제 제약을 타는지 검증해야 하며, 외래키 충돌을 예방해야 합니다.
