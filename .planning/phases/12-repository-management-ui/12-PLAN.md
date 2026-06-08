# Phase 12: Repository Management UI - Plan

**Target Phase:** 12
**Status:** executed

## Step 1: 신규 저장소 생성 페이지 구현 (NewRepository.razor)
- **File:** `Aristokeides.Api/Components/Pages/NewRepository.razor` (신규 생성)
- **Action:** 
  - `@page "/repositories/new"` 라우트를 정의하고, 로그인된 사용자만 접근할 수 있도록 `@attribute [Authorize]`를 설정합니다.
  - 저장소 이름(Name), 설명(Description), 비공개 여부(IsPrivate) 입력을 지원하는 Blazor `<EditForm>`을 만듭니다.
  - 이름 유효성 검증 규칙: 영문, 숫자, 대시(`-`), 언더스코어(`_`)만 허용하며 공백은 불가합니다.
  - `Submit` 시 DB 중복성 검사(현재 로그인한 사용자의 저장소 목록 내 동일 이름 존재 여부)를 수행합니다. 중복 발견 시 폼 상단에 오류 알림창(Red Alert Box)을 표시합니다.
  - 검증 통과 시, DB에 `Repository`와 기본 칸반 보드 컬럼(`To Do`, `In Progress`, `Done`) 레코드를 삽입하고 `RepositoryCreationChannel`에 저장소 ID를 Enqueue하여 백엔드 디렉토리 파일 시스템 초기화를 백그라운드 워커에 위임합니다.
  - 완료 시 생성된 리포지토리 페이지(`/{Username}/{RepoName}`)로 리다이렉트합니다.

## Step 2: 저장소 설정 페이지 구현 - 기본 정보 수정 및 성공 알림 (RepositorySettings.razor)
- **File:** `Aristokeides.Api/Components/Pages/RepositorySettings.razor` (신규 생성)
- **Action:** 
  - `@page "/{Username}/{RepoName}/settings"` 라우트를 매핑합니다.
  - `@attribute [Authorize]`를 설정하고, 온로드 시 현재 사용자가 리포지토리 소유자(Owner)가 아닐 경우 접근을 거부하고 "권한 없음" 화면을 렌더링합니다.
  - 이름, 설명, 가시성(`IsPrivate`)을 변경할 수 있는 설정 폼을 제공합니다.
  - 수정 저장 제출 시 다음 작업을 수행합니다:
    - 이름이 변경된 경우:
      - 동일 사용자의 다른 리포지토리와의 중복 검사.
      - 물리 베어 Git 저장소 폴더 디렉토리 이름 변경 (`GitRepos/{Username}/{OldRepoName}.git` -> `GitRepos/{Username}/{NewRepoName}.git` 이동).
      - DB의 저장소 이름을 업데이트합니다.
    - 설명 및 가시성 수정을 반영하고 `UpdatedAt`을 현재 시간으로 업데이트합니다.
    - 변경 완료 후 리다이렉트 없이 현재 페이지를 유지하며 상단에 녹색 톤의 성공 알림 상자를 출력합니다.

## Step 3: 저장소 설정 페이지 구현 - 저장소 삭제 및 안전 모달 적용
- **File:** `Aristokeides.Api/Components/Pages/RepositorySettings.razor` (추가 구현)
- **Action:** 
  - 설정 페이지 하단에 빨간색 경고 영역(Danger Zone)을 구성하고 "저장소 삭제" 버튼을 배치합니다.
  - 버튼 클릭 시 안전 확인 모달(Safe Deletion Modal)을 실행하고 사용자에게 `"삭제 확인을 위해 {Username}/{RepoName}을(를) 입력하세요"` 지침을 안내합니다.
  - 사용자가 입력한 문자열과 리포지토리 전체 경로(`{Username}/{RepoName}`)가 정확히 일치할 경우에만 "영구 삭제" 버튼을 활성화시킵니다.
  - 삭제 실행 시:
    - DB에서 `Repository` 레코드를 삭제하여 Cascade로 엮인 보드 컬럼, 이슈, PR, 댓글 등의 데이터를 함께 지웁니다.
    - 물리 베어 Git 저장소 디렉토리 `GitRepos/{Username}/{RepoName}.git`을 재귀적으로 삭제합니다.
    - 성공적으로 마무리되면 대시보드(`/dashboard`)로 리다이렉트합니다.

## Step 4: 저장소 메인 상세 뷰(RepoBrowser.razor)에 설정 탭 링크 연동
- **File:** `Aristokeides.Api/Components/Pages/RepoBrowser.razor`
- **Action:** 
  - 저장소 메인 화면 상단 헤더 메뉴 탭(Code, Issues 등)에 "Settings" (설정) 링크를 노출합니다.
  - 현재 로그인한 사용자가 해당 리포지토리의 소유자(Owner)인 경우에만 설정 탭 메뉴를 보여주도록 가시성을 제한합니다.
