# Phase 4: Issue Management - Research

## Context Summary
이번 Phase 4에서는 프로젝트 요구사항인 `ISSU-01`(이슈 생성/수정/닫기)과 `ISSU-02`(기본적인 칸반 보드)를 달성하기 위한 기능을 구현합니다. 주요 결정 사항(D-01 ~ D-04)에 따라 저장소별 고유 ID(`LocalId`) 생성, 커스텀 상태(`BoardColumn`), HTML5 네이티브 Drag & Drop(InteractiveServer 활용), 그리고 담당자 지정 기능(`AssigneeId`)이 핵심 과제입니다.

## Architecture & Implementation Plan

### 1. Database Schema
다음과 같은 엔티티와 관계 모델링이 필요합니다:
- **`BoardColumn` (또는 `IssueState`) 엔티티**: `Repository`와 1:N 관계
  - `Id` (Guid, PK)
  - `RepositoryId` (Guid, FK)
  - `Name` (string) - "To Do", "In Progress", "Done" 등
  - `Order` (int) - 칸반 보드 내의 정렬 순서
- **`Issue` 엔티티**: `Repository`와 1:N 관계
  - `Id` (Guid, PK)
  - `LocalId` (int) - 저장소 내 종속적인 순차 ID
  - `RepositoryId` (Guid, FK)
  - `Title` (string)
  - `Description` (string)
  - `CreatorId` (int, FK to `User`)
  - `AssigneeId` (int?, FK to `User`)
  - `ColumnId` (Guid, FK to `BoardColumn`)
  - `CreatedAt` (DateTime), `UpdatedAt` (DateTime)

### 2. Initial Data Seeding
`RepositoriesController.cs`의 `Create` 액션에서 저장소 생성 시 기본 3개의 `BoardColumn` ("To Do", "In Progress", "Done")을 DB에 자동 등록하는 로직을 추가해야 합니다.

### 3. LocalId Generation Logic
이슈 생성 시 동시성 문제를 완화하면서 저장소 내의 순차 번호를 발급하기 위해, 동일 `RepositoryId` 내에서의 Max `LocalId`를 조회하여 +1을 하는 로직을 이슈 생성 Service 또는 Controller 단위에서 적용합니다.

### 4. Blazor Components & Routing
- `@page "/{username}/{repoName}/issues"`: 전체 이슈 리스트 및 칸반 보드 메인 뷰
- `@page "/{username}/{repoName}/issues/new"`: 새로운 이슈 작성 폼
- `@page "/{username}/{repoName}/issues/{localId:int}"`: 특정 이슈 상세 정보, 수정 및 닫기 화면

### 5. Kanban Drag and Drop (InteractiveServer)
HTML5 네이티브 D&D 이벤트를 Blazor에서 처리하기 위해 `InteractiveServer` 렌더링 모드가 필요합니다. 
- **설정 추가**: `Program.cs` 내에 `.AddInteractiveServerComponents()`와 `.AddInteractiveServerRenderMode()` 추가
- **이벤트 매핑**: 칸반 보드 컴포넌트(`@rendermode InteractiveServer`)에서 `@ondragstart`, `@ondrop`, `@ondragover:preventDefault` 등을 바인딩하여, 드래그 된 이슈 항목을 상태(Column) 이동 후 `AppDbContext`를 통해 업데이트 하도록 구현합니다.

## Validation Architecture

Nyquist 검증 요건을 충족하기 위한 엔드투엔드(E2E) 테스트 및 검증 방안은 다음과 같습니다:

1. **DB Schema & Seeding Validation**:
   - `AppDbContext`에 `Issue`, `BoardColumn` 테이블이 성공적으로 생성되는지 검증.
   - 신규 `Repository` 생성 직후 "To Do", "In Progress", "Done" 상태가 정상적으로 삽입되는지 쿼리를 통해 확인.
2. **Issue Generation Validation (ISSU-01)**:
   - 특정 저장소에서 이슈 생성 폼을 통해 새 이슈를 등록했을 때, 해당 저장소의 `LocalId`가 1부터 순차적으로 증가하는지 확인.
   - 제목, 본문, 담당자(선택) 정보가 이슈 상세 화면에 정확히 반영되는지 확인.
3. **Kanban Board Validation (ISSU-02, HTML5 D&D)**:
   - 보드 UI에 "To Do", "In Progress", "Done" 컬럼이 렌더링되고 생성한 이슈가 알맞은 위치("To Do" 등)에 표시되는지 검증.
   - Blazor Event Callback(`@ondrop`)을 통해 다른 컬럼으로 이슈를 드래그 앤 드롭했을 때, DB의 `ColumnId`가 즉시 업데이트되고 화면이 깜빡임 없이 갱신되는지 확인.
4. **Integration Testing Flow**:
   - 로그인 -> 저장소 생성 -> `/{username}/{repoName}/issues` 접속 -> 이슈 작성 -> 상태 이동(D&D) -> 이슈 상세 화면 접속 및 수정(상태 변경/할당자 지정).
