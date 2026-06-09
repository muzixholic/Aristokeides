---
status: planning
phase: 19-organization-teams
source:
  - .planning/phases/19-organization-teams/19A-PLAN.md
  - .planning/phases/19-organization-teams/19B-PLAN.md
  - .planning/phases/19-organization-teams/19C-PLAN.md
started: "2026-06-09T22:36:00Z"
updated: "2026-06-09T22:36:00Z"
---

## Current Test

[planning phase]

## Tests

### 1. 조직(Organization) 생성 및 중복 검증 테스트
expected: |
  사용자가 `/orgs/new` 페이지에서 조직 이름과 설명을 입력하고 등록했을 때, DB에 조직 레코드와 함께 본인이 `Owner` 역할로 소속된 OrganizationMember 레코드가 생성되어야 한다. 이미 존재하는 사용자 이름(Username) 또는 기존 조직 이름과 겹치는 이름을 입력하면 오류가 표시되고 생성이 차단되어야 한다.
result: pending

### 2. 조직 소유의 저장소 생성 및 파일 보존 테스트
expected: |
  `/repos/new` 화면에서 "소유자" 드롭다운을 통해 가입된 조직을 선택하고 저장소를 생성하면, 해당 리포지토리의 `OrganizationId` 필드가 할당되고 `OwnerId`는 null로 설정되어야 한다. 서버 상의 실제 저장소 보관 경로(`GitRepos/{orgname}/{reponame}.git`)에 베어(Bare) 저장소가 안전하게 초기 생성되어야 한다.
result: pending

### 3. 조직 멤버 추가 및 권한 설정 테스트
expected: |
  조직 프로필 설정의 멤버 관리 메뉴에서, 다른 사용자의 사용자명(Username) 또는 이메일을 검색하여 초대할 수 있어야 한다. 멤버의 권한 역할(Owner / Member)을 변경하고, 리스트에서 멤버를 삭제(조직 탈퇴 처리)했을 때 DB 매핑에 정상적으로 실시간 적용되어야 한다.
result: pending

### 4. 팀(Team) 생성 및 리포지토리 권한 제어 테스트
expected: |
  조직 내에 새 팀을 개설하고 팀원을 배치할 수 있어야 한다. 해당 팀에 조직 소속 특정 저장소의 권한(Read / Write / Admin)을 부여했을 때, `RepositoryPermission` 테이블에 정상적으로 매핑 관계와 등급이 반영되어야 한다.
result: pending

### 5. 조직 권한 기반 Git 접근(HTTP/SSH Push 및 Pull) 테스트
expected: |
  조직 비공개 저장소에 대해:
  - 해당 조직의 `Owner`이거나 해당 저장소에 `Read` 권한을 가진 팀에 소속된 사용자는 정상적으로 `git clone/pull`을 수행할 수 있어야 한다.
  - 저장소에 `Write` 또는 `Admin` 권한이 부여된 팀의 구성원만 `git push`를 수행할 수 있어야 하며, 권한이 없는 비소속 사용자가 푸시를 시도할 시 HTTP 403 Forbidden 또는 SSH Permission Denied로 안전하게 거부되어야 한다.
result: pending

## Summary

total: 5
passed: 0
issues: 0
pending: 5
skipped: 0

## Gaps

[none yet]
