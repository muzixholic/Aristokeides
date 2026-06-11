# Phase 19 Wave 1: 조직 및 팀 데이터 모델 구축과 생성 UI 구현 요약

## 1. 작업 개요

- **목표:** 조직(Organization), 조직원(OrganizationMember), 팀(Team), 팀원(TeamMember), 저장소 권한(RepositoryPermission)의 데이터베이스 모델 및 제약 조건을 정의하고 다중 데이터베이스 마이그레이션에 반영한다. 사용자가 브라우저를 통해 고유한 이름을 검증하며 새로운 조직을 생성할 수 있는 Blazor UI(`NewOrganization.razor`)와 비즈니스 로직을 구축한다.
- **수행일자:** 2026년 6월 9일
- **상태:** 성공적으로 완료 및 모든 테스트 검증 통과

---

## 2. 세부 구현 내용

### 1) 조직 및 팀 데이터 모델 구축
- [Organization.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/Organization.cs): 조직의 기본 정보(이름, 설명, 생성일)를 정의하고 회원, 팀, 저장소 모음 탐색 속성을 추가했습니다.
- [OrganizationMember.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/OrganizationMember.cs): 조직과 사용자의 다대다 매핑 엔터티로, 회원 역할(Owner, Member) 및 가입일을 관리합니다.
- [Team.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/Team.cs): 조직 내부에 속하는 팀 정보(이름, 설명)를 정의했습니다.
- [TeamMember.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/TeamMember.cs): 팀과 사용자의 다대다 매핑 엔터티입니다.
- [RepositoryPermission.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/RepositoryPermission.cs): 저장소와 팀/사용자 간의 접근 권한 수준(Read, Write, Admin)을 제어하기 위한 매핑 엔터티를 구현했습니다.
- [Repository.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/Repository.cs): 조직이 저장소를 소유할 수 있도록 `OrganizationId` 외래 키와 `Organization` 탐색 속성을 추가했습니다.

### 2) AppDbContext 매핑 및 데이터베이스 마이그레이션 적용
- [AppDbContext.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Data/AppDbContext.cs)에 신규 정의된 5개의 DbSet을 추가했습니다.
- `OnModelCreating` 메서드 내에 다음 제약 조건 및 복합 유니크 인덱스를 추가 정의했습니다:
  - `Organization`: 이름(`Name`) 필드 고유값 인덱스 설정
  - `OrganizationMember`: `(OrganizationId, UserId)` 복합 고유값 인덱스
  - `Team`: `(OrganizationId, Name)` 복합 고유값 인덱스
  - `TeamMember`: `(TeamId, UserId)` 복합 고유값 인덱스
  - `RepositoryPermission`: `RepositoryId`, `UserId`, `TeamId` 외래 키 구성 및 Cascade 삭제 전략 설정
- 다중 데이터베이스 공급자 지원 사양에 맞추어 Sqlite, Postgres, Mysql 각각의 마이그레이션(`AddOrganizationAndTeams`)을 추가 및 빌드했고, 로컬 SQLite 데이터베이스 개발 환경에 업데이트를 정상 적용 완료했습니다.

### 3) 조직 생성 UI (NewOrganization.razor) 구현
- [NewOrganization.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/Pages/NewOrganization.razor) Blazor 페이지를 InteractiveServer 렌더 모드로 구현했습니다.
- **주요 기능:**
  - **소유자 정보 표시:** 생성하는 사용자의 정보를 리드온리 형태로 출력합니다.
  - **입력 유효성 검사:** 조직명 입력 시 영문 소문자, 숫자, 대시(-) 문자만 허용하는 클라이언트 측 정규식 검사를 적용하였으며, 대문자 입력 시 소문자로 자동 자동 변환 처리되도록 구성했습니다.
  - **이름 중복 검사:** 제출 시 데이터베이스 조회를 통해 입력한 조직명이 기존 사용자의 `Username` 또는 다른 조직의 `Name`과 대소문자 구분 없이 충돌하지 않는지 검증합니다. 충돌 시 에러 메시지를 동적으로 화면에 로드합니다.
  - **소유자 매핑 및 트랜잭션:** 검증 통과 시 조직 엔터티와 함께 생성자를 `Owner` 역할로 등록한 `OrganizationMember` 엔터티를 단일 트랜잭션으로 저장합니다.
  - **리다이렉션:** 저장 성공 시, 어셈블리 리플렉션을 통해 `/orgs/{orgname}` 경로를 지원하는 대시보드 컴포넌트가 존재할 경우 해당 주소로 리다이렉트하고, 없을 경우 안전하게 홈 `/` 경로로 리다이렉트하도록 예외 완충 로직을 포함했습니다.
  - **UX/디자인 개선:** 깔끔한 그라디언트 버튼, 입력 필드 포커스 시의 그림자 트랜지션, 로딩 스피너 및 Glassmorphism 레이아웃을 반영하여 프리미엄 수준의 웹 미학을 제공합니다.

---

## 3. 테스트 및 검증 결과

- **신규 테스트 클래스 생성:** [OrganizationModelTests.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Tests/OrganizationModelTests.cs)
- **테스트 커버리지 항목:**
  1. 조직 이름 형식 검증용 정규식(Regex) 유효성 테스트 (유효/무효 조건 분기 검사)
  2. 조직 생성 시 Owner 멤버 자동 링키지 및 역할 데이터베이스 영속성 테스트
  3. 조직명 제안 시 기존 사용자명(Username)과의 충돌 사전 검출 테스트
  4. 조직명 제안 시 기존 조직 이름과의 중복 사전 검출 테스트
  5. 조직 내 팀(Team) 생성 및 팀 멤버(TeamMember) 다대다 흐름 테스트
  6. 저장소 권한(RepositoryPermission) 생성 및 권한 수준 지정 통합 테스트
- **테스트 구동 결과:** 전체 74개 테스트 케이스 중 **74개 전원 통과 (Passed)**

---

## 4. 산출물 목록

| 파일 경로 | 작업 구분 | 설명 |
|---|---|---|
| [Organization.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/Organization.cs) | 신규 | 조직 엔터티 데이터 모델 |
| [OrganizationMember.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/OrganizationMember.cs) | 신규 | 조직원 매핑 정보 데이터 모델 |
| [Team.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/Team.cs) | 신규 | 조직 내 팀 데이터 모델 |
| [TeamMember.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/TeamMember.cs) | 신규 | 팀 멤버 매핑 데이터 모델 |
| [RepositoryPermission.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/RepositoryPermission.cs) | 신규 | 리포지토리별 팀/개인 권한 제어 데이터 모델 |
| [NewOrganization.razor](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Components/Pages/NewOrganization.razor) | 신규 | 조직 신규 생성 Blazor 인터랙티브 컴포넌트 |
| [OrganizationModelTests.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Tests/OrganizationModelTests.cs) | 신규 | 조직 및 팀 비즈니스 로직 단위/통합 테스트 |
| [Repository.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Models/Repository.cs) | 수정 | OrganizationId 외래 키 및 탐색 속성 추가 |
| [AppDbContext.cs](file:///Users/muzixholic/Projects/Aristokeides/Aristokeides.Api/Data/AppDbContext.cs) | 수정 | DbSet 선언 및 OnModelCreating 제약 조건 정의 |
| [19A-SUMMARY.md](file:///Users/muzixholic/Projects/Aristokeides/.planning/phases/19-organization-teams/19A-SUMMARY.md) | 신규 | Phase 19 Wave 1 요약 보고서 (본 파일) |
