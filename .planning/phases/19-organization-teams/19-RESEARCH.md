# Phase 19: 조직 및 팀 기능 — 리서치

## 1. DB 모델 및 테이블 스키마 설계

조직(Organization), 조직원(OrganizationMember), 팀(Team), 팀원(TeamMember), 저장소 권한(RepositoryPermission) 모델을 구성합니다.

### 1.1. Organization (조직)
- `int Id` (PK)
- `string Name` (Unique, URL 경로 세그먼트로 사용되므로 영문/숫자/대시만 허용)
- `string? Description`
- `DateTime CreatedAt`
- 관계:
  - `ICollection<OrganizationMember> Members`
  - `ICollection<Team> Teams`
  - `ICollection<Repository> Repositories`

### 1.2. OrganizationMember (조직원)
- `int Id` (PK)
- `int OrganizationId` (FK)
- `int UserId` (FK)
- `string Role` ("Owner" - 모든 관리 권한 보유, "Member" - 기본 조직원)
- `DateTime JoinedAt`
- 유니크 인덱스: `(OrganizationId, UserId)`

### 1.3. Team (조직 내 팀)
- `int Id` (PK)
- `int OrganizationId` (FK)
- `string Name` (조직 내 고유 명칭)
- `string? Description`
- 관계:
  - `ICollection<TeamMember> Members`
  - `ICollection<RepositoryPermission> Permissions`

### 1.4. TeamMember (팀 구성원)
- `int Id` (PK)
- `int TeamId` (FK)
- `int UserId` (FK)
- 유니크 인덱스: `(TeamId, UserId)`

### 1.5. RepositoryPermission (저장소 접근 권한)
개별 사용자 혹은 팀 단위의 권한 부여를 단일 테이블로 구조화합니다.
- `int Id` (PK)
- `Guid RepositoryId` (FK)
- `int? UserId` (FK, nullable)
- `int? TeamId` (FK, nullable)
- `string AccessLevel` ("Read", "Write", "Admin")
- 제약 조건: `UserId`와 `TeamId` 중 정확히 하나만 채워져 있어야 함.

---

## 2. 저장소 소유권 및 권한 검증 로직 개편

### 2.1. Repository 엔터티 변경
- `int? OwnerId`를 Nullable로 변경하여 조직 소유 저장소일 경우 `null`이 될 수 있도록 함.
- `int? OrganizationId` (FK) 필드 추가.
- 유니크 인덱스 변경:
  - 기존: `(OwnerId, Name)`
  - 신규: `(OwnerId, Name)` 및 `(OrganizationId, Name)`의 독립적인 유니크 제약 적용. (사용자별 저장소명 고유, 조직별 저장소명 고유)

### 2.2. Git HTTP 및 SSH 인증 검증 로직 업데이트
사용자가 특정 저장소에 읽기(Read) 또는 쓰기(Write/Push)를 시도할 때 다음 권한 매트릭스를 검사합니다:

1. **개인 소유 저장소 (OwnerId가 있는 경우):**
   - 저장소 소유자(OwnerId == UserId): 모든 권한(Admin) 보유.
   - 공개 저장소(`IsPrivate == false`): 누구나 읽기(Read) 가능.
   - 비공개 저장소(`IsPrivate == true`)이거나 쓰기 시도인 경우: `RepositoryPermission`에서 개별 사용자의 `AccessLevel` 확인.

2. **조직 소유 저장소 (OrganizationId가 있는 경우):**
   - 사용자가 해당 조직의 `Owner`인 경우: 모든 권한(Admin) 보유.
   - 공개 저장소(`IsPrivate == false`): 누구나 읽기(Read) 가능.
   - 비공개 저장소이거나 쓰기 시도 시:
     - 사용자의 개별 권한(`RepositoryPermission` 중 `UserId == UserId`인 건) 확인.
     - 사용자가 소속된 팀들의 권한(`RepositoryPermission` 중 `TeamId`가 사용자가 소속된 팀 ID 목록에 포함된 건) 확인.
     - 매칭되는 권한 중 **가장 높은 권한** 선택 ("Admin" > "Write" > "Read").
   - 권한 등급 확인:
     - **읽기 동작 (Clone, Fetch, Browse):** "Read", "Write", "Admin" 중 하나 필요.
     - **쓰기 동작 (Push):** "Write", "Admin" 중 하나 필요.
     - **관리자 동작 (Settings, Delete):** "Admin" 필요.

---

## 3. 웹 UI 라우팅 설계

1. **조직 생성 및 관리:**
   - `/orgs/new`: 신규 조직 생성 화면 (이름, 설명 입력).
   - `/orgs/{orgname}`: 조직 홈 대시보드 (소속 저장소 목록, 조직원 목록, 팀 목록 제공).
   - `/orgs/{orgname}/settings`: 멤버 추가/제거 및 역할 변경.
   - `/orgs/{orgname}/teams`: 팀 목록 및 신규 팀 생성.
   - `/orgs/{orgname}/teams/{teamname}`: 팀원 관리 및 팀별 저장소 접근 권한 할당.

2. **저장소 생성 시 소유자 선택:**
   - `/repos/new` (NewRepository.razor)에서 "소유자" 드롭다운 제공.
   - 옵션: `현재 사용자(Me)` 및 `소속 조직 중 Owner 권한을 가진 조직 목록`.

3. **저장소 설정 내 협업 관리:**
   - `/repos/{owner}/{repo}/settings/collaborators`: 저장소별 개별 협업자 추가 및 팀별 권한 설정 UI 제공.

---

## 4. UI/UX 구현 전략 (Blazor InteractiveServer)

- 조직 생성 및 설정은 **InteractiveServer** 모드를 활용해 동적인 사용자 검색, 드롭다운 바인딩, 실시간 권한 변경 작업을 부드럽게 구현합니다.
- 조직원 추가 시 사용자 이메일 또는 사용자명으로 자동완성(Autocomplete) 혹은 검색 방식을 도입하여 직관적인 UX를 제공합니다.
- 기존의 네비게이션 레이아웃을 확장하여 로그인 시 대시보드 사이드바나 헤더 드롭다운에 "내 조직 목록"을 추가해 조직 전환이 쉽도록 돕습니다.
