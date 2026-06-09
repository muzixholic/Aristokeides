---
title: "조직 관리 화면 및 저장소 권한 설정 UI 구현"
phase: 19
wave: 3
depends_on: [19B-PLAN.md]
files_modified:
  - Aristokeides.Api/Components/Pages/OrgDashboard.razor
  - Aristokeides.Api/Components/Pages/OrgSettings.razor
  - Aristokeides.Api/Components/Pages/OrgTeams.razor
  - Aristokeides.Api/Components/Pages/TeamDetails.razor
  - Aristokeides.Api/Components/Pages/RepositorySettings.razor
  - Aristokeides.Api/Components/Pages/Dashboard.razor
  - Aristokeides.Api/Components/Layout/MainLayout.razor
autonomous: true
requirements:
  - "조직 홈 대시보드 및 멤버 관리, 팀 관리, 팀별 저장소 접근 제어 UI를 각각 설계 및 연동해야 한다."
  - "개별 저장소 설정 페이지에서 협업자(개인) 및 팀별 접근 등급(Read/Write/Admin)을 직관적으로 관리할 수 있어야 한다."
  - "사용자 메인 대시보드와 상단 레이아웃에 소속 조직 목록과 조직별 저장소가 정상 연동되어 노출되어야 한다."
---

# Plan 19C: 조직 관리 화면 및 저장소 권한 설정 UI 구현

## Objective

조직 활성화를 위한 프론트엔드 Blazor 페이지(조직 대시보드, 조직 멤버/팀 설정, 팀 상세 관리)를 신규 구축하고, 개별 저장소의 설정 화면에 멤버/팀 권한을 제어할 수 있는 "협업자 관리" UI를 통합한다. 메인 대시보드 및 네비게이션 레이아웃을 확장하여 사용자가 소속된 모든 조직 정보가 매끄럽게 연결되도록 완성한다.

## Tasks

<task id="19C-1">
<title>조직 대시보드 및 설정 관리 화면 구현</title>
<read_first>
- `Aristokeides.Api/Components/Pages/Settings.razor` — 기존 탭 제어 및 인터랙티브 바인딩 참조
</read_first>
<action>
1. **OrgDashboard.razor 신규 작성 (`/orgs/{OrgName}`):**
   - 조직에 속한 저장소 목록과 조직 멤버 목록, 팀 목록을 요약하여 표시한다.
   - 조직 내 멤버 등급에 따라 "조직 설정" 버튼 노출 제어.
2. **OrgSettings.razor 신규 작성 (`/orgs/{OrgName}/settings`):**
   - 조직 소유자(Owner) 권한이 있는 사용자만 접근 허용.
   - **조직원 초청:** 다른 사용자 아이디 또는 이메일 검색 입력 폼을 두어 검색 후 조직원으로 추가 (기본 "Member" 권한 부여).
   - **조직원 권한 갱신:** 리스트에서 각 멤버의 Role을 "Owner" 또는 "Member"로 토글 전환하는 드롭다운 API 연동.
   - **조직원 방출:** 조직원 삭제 버튼을 두어 확인 후 목록 및 DB 매핑에서 제거.
</action>
<acceptance_criteria>
- `/orgs/{OrgName}` 접근 시 조직의 저장소, 팀, 멤버 현황이 깔끔하게 표시된다.
- 조직의 `Owner`만 `/orgs/{OrgName}/settings`에 접근하여 멤버를 초청하거나 등급 수정, 방출 처리를 완료할 수 있다.
- 존재하지 않는 사용자 이름 초대 시 적절한 유효성 에러 메시지가 화면에 노출된다.
</acceptance_criteria>
</task>

<task id="19C-2">
<title>팀 관리 및 팀 상세 설정 UI 구현</title>
<read_first>
- `Aristokeides.Api/Components/Pages/OrgDashboard.razor` — 작성된 조직 정보 바인딩 참조
</read_first>
<action>
1. **OrgTeams.razor 신규 작성 (`/orgs/{OrgName}/teams`):**
   - 조직 내 생성된 팀 목록을 출력한다.
   - 신규 팀 생성 기능 (팀 이름, 설명 입력) 연동.
2. **TeamDetails.razor 신규 작성 (`/orgs/{OrgName}/teams/{TeamName}`):**
   - **팀원 관리:** 팀원에 속한 사용자 목록을 보여주고, 조직원 중 원하는 사용자를 선택해 팀에 추가하거나 제거하는 기능 제공.
   - **저장소 권한 관리:** 조직 소유의 저장소를 선택하고 접근 등급("Read", "Write", "Admin")을 지정하여 `RepositoryPermission`으로 팀 단위의 매핑 권한을 부여 및 삭제하는 기능 구현.
</action>
<acceptance_criteria>
- 조직의 팀 관리 탭에서 새로운 팀이 오류 없이 생성된다.
- 팀 상세 페이지에서 팀원 추가/제거 및 팀 단위의 저장소 권한 설정(Read/Write/Admin) 목록 추가/삭제가 DB에 실시간 동기화된다.
</acceptance_criteria>
</task>

<task id="19C-3">
<title>저장소 설정 내 협업자 관리 탭 구현</title>
<read_first>
- `Aristokeides.Api/Components/Pages/RepositorySettings.razor` — 기존 저장소 설정 탭 및 폼 구조 확인
</read_first>
<action>
`RepositorySettings.razor` 내부에 "협업자 관리" 탭을 추가하고 다음 기능을 구현한다:

1. **설정 권한 탭 연동:**
   - 기존 설정 페이지 탭 리스트에 "Collaborators & Permissions" 탭 추가.
2. **권한 리스트 및 관리 기능:**
   - 해당 저장소에 할당된 모든 `RepositoryPermission` 목록(개별 사용자 및 팀 권한)을 테이블 형태로 로드한다.
   - **개인 협업자 추가:** 사용자명 검색을 통해 개별 사용자 권한("Read", "Write", "Admin")을 추가하는 폼 구성.
   - **팀 권한 추가 (조직 소유 저장소인 경우에만 노출):** 조직 내 생성된 팀 중 하나를 선택하고 권한 등급을 지정하여 팀 권한 추가.
   - **권한 제거:** 개별 권한 항목의 우측에 삭제 버튼을 배치해 즉시 무효화 및 데이터베이스에서 삭제 처리.
</action>
<acceptance_criteria>
- 저장소 관리자가 저장소 설정 페이지에서 협업자 및 팀 권한을 유연하게 추가/삭제할 수 있다.
- 저장소 소유주가 개인이면 "팀 권한 추가" 기능이 시각적으로 가려져 충돌을 미연에 방지한다.
- 할당된 권한이 정상 저장되어 즉각적으로 Git 접근 차단 정책에 연동된다.
</acceptance_criteria>
</task>

<task id="19C-4">
<title>대시보드 및 레이아웃 통합</title>
<read_first>
- `Aristokeides.Api/Components/Pages/Dashboard.razor` — 기존 개인 소유 리포지토리 리스트 바인딩 확인
- `Aristokeides.Api/Components/Layout/MainLayout.razor` — 상단 바 사용자 프로필 메뉴 및 라우팅 링크 참조
</read_first>
<action>
1. **Dashboard.razor 연동 개편:**
   - 대시보드 화면 우측 또는 좌측 패널에 "내 조직 목록(My Organizations)" 목록을 출력한다. (클릭 시 `/orgs/{orgname}`으로 이동)
   - "조직 생성" 버튼을 추가하여 `/orgs/new`로 편리하게 유도한다.
   - 메인 리포지토리 목록에 개인 소유 리포지토리뿐만 아니라, **사용자가 가입된 조직 소유의 저장소 중 사용자에게 읽기 권한이 부여된 리포지토리**도 함께 통합 표시한다 (저장소명 앞에 `{조직명}/{저장소명}` 형태로 라벨 구분).
2. **MainLayout.razor 연동 개편:**
   - 상단 네비게이션 또는 사용자 드롭다운 메뉴에 사용자가 소속된 조직 목록 링크를 바인딩하여 브라우저 어디서나 바로 조직 홈으로 점프할 수 있도록 UX를 개선한다.
</action>
<acceptance_criteria>
- 로그인 후 메인 대시보드 진입 시 본인의 가입 조직 목록이 정상 출력된다.
- 소유하거나 참여 중인 조직 리포지토리가 대시보드 저장소 목록에 병합 렌더링된다.
- 네비바 유저 메뉴 내 조직 링크 숏컷이 올바르게 로드되어 링크로 동작한다.
</acceptance_criteria>
</task>

## must_haves

- 모든 조직 설정 화면 및 팀 세부 변경 화면은 `InteractiveServer` 렌더링을 활용해 지연 없는 반응형 동작을 제공해야 한다.
- 조직 소유 저장소의 소속 설정 및 권한 매핑은 안전하게 권한이 검증된 대상 사용자에게만 허용되어야 한다.
- 권한이 없는 비소속 사용자가 다른 조직 설정 주소를 수동 타이핑하여 진입 시 에러가 표시되거나 홈으로 튕겨 나가야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Components/Pages/OrgDashboard.razor` | 신규 | 조직 정보 요약 및 저장소/팀/멤버 목록 대시보드 페이지 |
| `Aristokeides.Api/Components/Pages/OrgSettings.razor` | 신규 | 조직원 초대, 역할 수정 및 방출 기능 관리 페이지 |
| `Aristokeides.Api/Components/Pages/OrgTeams.razor` | 신규 | 조직 팀 목록 및 생성 페이지 |
| `Aristokeides.Api/Components/Pages/TeamDetails.razor` | 신규 | 팀원 등록/제거 및 팀 권한(RepositoryPermission) 맵 설정 화면 |
| `Aristokeides.Api/Components/Pages/RepositorySettings.razor` | 수정 | Collaborators 탭을 통한 개별 사용자 및 팀별 권한 설정 UI 통합 |
| `Aristokeides.Api/Components/Pages/Dashboard.razor` | 수정 | 내 조직 리스트 바인딩 및 조직 소속 참여 리포지토리 뷰 병합 |
| `Aristokeides.Api/Components/Layout/MainLayout.razor` | 수정 | 사용자 메뉴 내 소속 조직 이동 숏컷 링크 추가 |
