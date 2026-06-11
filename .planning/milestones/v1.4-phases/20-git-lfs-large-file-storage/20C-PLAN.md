---
title: "LFS Locks API 통합 및 UI 연동 완료"
phase: 20
wave: 3
depends_on: ["20B"]
files_modified:
  - Aristokeides.Api/Controllers/LfsApiController.cs
  - Aristokeides.Api/Services/GitBrowserService.cs
  - Aristokeides.Api/Components/Pages/RepoBlob.razor
autonomous: true
requirements:
  - "LfsApiController에서 LfsLock DB 테이블을 연동하여 LFS Locks API 규격(생성, 목록 조회, 검증, 해제)을 완전하게 구현한다."
  - "GitBrowserService를 확장하여 저장소 파일 조회 시 LFS 포인터 여부를 자동 감지하고, 포인터 메타데이터(OID, 실제 크기)를 추출해 제공하도록 한다."
  - "RepoBlob.razor 웹 UI를 수정하여 LFS 포인터 파일인 경우, 이미지는 화면에 인라인으로 미리보기(렌더링)하고, 기타 파일은 원본 다운로드 링크를 제공하도록 통합한다."
---

# Plan 20C: LFS Locks API 통합 및 UI 연동 완료

## Objective

Locks API 비즈니스 로직을 실질적인 DB 트랜잭션과 연결하여 파일 잠금 충돌 제어를 완성합니다. 또한 저장소 브라우저 서비스(`GitBrowserService`)에서 LFS 포인터를 자동 파싱하고, Blazor 파일 뷰어(`RepoBlob.razor`)에서 이를 대체 렌더링함으로써 사용자가 LFS에 등록된 대용량 이미지와 바이너리를 웹 브라우저에서 투명하게 확인하고 다운로드받을 수 있는 사용자 경험을 완성합니다.

## Tasks

<task id="20C-1">
<title>LFS Locks API 비즈니스 로직 구현</title>
<read_first>
- `Aristokeides.Api/Controllers/LfsApiController.cs` — 20A단계에서 구성한 Locks API 스켈레톤 액션 참조
- `Aristokeides.Api/Models/LfsLock.cs` — LfsLock 엔티티 정의 참조
</read_first>
<action>
1. `LfsApiController.cs`에 Locks API 내부 로직 완성:
   - **Lock 생성 (POST `/locks`):**
     - 요청 바디의 `path`에 대해 이미 다른 사용자가 걸어둔 락이 존재하는지 `LfsLocks` 테이블 조회.
     - 존재 시: 이미 존재하는 락 정보와 함께 409 Conflict 응답 반환.
     - 미존재 시: 신규 `LfsLock` 레코드를 데이터베이스에 저장하고 201 Created 응답 반환.
   - **Lock 목록 조회 (GET `/locks`):**
     - 쿼리 조건(`path`, `id` 등)이 제공되면 필터링하여 매칭되는 락 목록 및 소유자 정보를 JSON 포맷으로 반환.
   - **Lock 검증 (POST `/locks/verify`):**
     - 현재 호출자(인증된 사용자)가 소유한 락 목록은 `ours` 배열에, 타인이 소유한 락 목록은 `theirs` 배열에 나누어 담아 200 OK 응답 처리.
   - **Lock 해제 (POST `/locks/{id}/unlock`):**
     - 지정된 ID의 락을 조회하고, 현재 사용자가 락의 소유자(Owner)이거나 관리자(Admin) 권한을 가졌는지 검증.
     - `force` 옵션이 true인 경우 소유자가 달라도 강제 해제(Admin 권한 필수)할 수 있도록 허용.
     - 검증 완료 시 데이터베이스에서 해당 `LfsLock` 레코드를 삭제하고 200 OK 반환.
</action>
<acceptance_criteria>
- 동일한 경로에 대해 두 명이 락 생성을 시도할 때 두 번째 요청에서 정확히 409 Conflict 상태 코드와 함께 기존 락 정보가 리턴된다.
- 락 검증(`/verify`) 요청 시 본인의 락과 타인의 락이 정상적으로 분류되어 리턴된다.
</acceptance_criteria>
</task>

<task id="20C-2">
<title>GitBrowserService LFS 포인터 감지 확장</title>
<read_first>
- `Aristokeides.Api/Services/GitBrowserService.cs` — 저장소 파일 읽기 및 브라우징 로직 참조
</read_first>
<action>
1. `GitBrowserService.cs`에서 리포지토리 파일의 내용(Blob)을 읽는 부분 수정:
   - 파일 크기가 300바이트 이하이고, 파일의 첫 라인이 `version https://git-lfs.github.com/spec/v1`으로 시작되는지 파싱 검사하는 `IsLfsPointer` 헬퍼 작성.
   - LFS 포인터로 인지된 경우:
     - 텍스트 파일 내용에서 `oid sha256:{64자 OID}` 및 `size {실제크기}` 필드를 정규표현식(Regex) 또는 라인 파싱을 통해 추출.
     - 파일 뷰어용 DTO 또는 모델에 `IsLfsPointer = true`, `LfsOid = {파싱된 OID}`, `LfsSize = {파싱된 크기}` 값을 세팅하여 반환하도록 확장.
</action>
<acceptance_criteria>
- LFS 포인터 텍스트 파일을 조회했을 때 `IsLfsPointer` 플래그가 true로 리턴되고 OID 해시값 및 원래 파일의 크기가 정상적으로 계산된다.
- 일반 텍스트나 바이너리 파일은 LFS 포인터로 오감지되지 않고 일반 경로로 정상 처리된다.
</acceptance_criteria>
</task>

<task id="20C-3">
<title>RepoBlob.razor 파일 뷰어 UI 연동</title>
<read_first>
- `Aristokeides.Api/Components/Pages/RepoBlob.razor` — Blazor 파일 뷰어 페이지 구성 및 렌더링 코드 참조
</read_first>
<action>
1. `RepoBlob.razor` Blazor 컴포넌트 수정:
   - 파일 정보를 바인딩할 때 `GitBrowserService`의 LFS 포인터 정보를 확인.
   - `IsLfsPointer`가 true인 경우:
     - **이미지 렌더링:** 파일 확장자가 이미지(`.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`)에 속하는 경우, 서버 내부 `LfsService`를 통해 해당 OID의 로컬 스토리지 데이터 바이트 배열을 로드하여 base64 데이터 URI(`data:image/...;base64,...`)로 변환하고 `<img src="..." />` 태그로 화면에 표시.
     - **바이너리/대용량 다운로드:** 이미지가 아니거나 기타 파일 형태인 경우, 화면에 LFS 포인터 원시 텍스트를 숨기고, "대용량 LFS 파일 ({LfsSize} bytes)" 안내와 함께 실제 원본 파일을 브라우저로 직접 다운로드받을 수 있는 파일 전송 API 경로(예: `/api/lfs/{owner}/{repo}/download/{LfsOid}`) 링크 버튼을 동적으로 생성하여 노출.
</action>
<acceptance_criteria>
- 저장소 브라우저에서 LFS 파일(예: 이미지)을 클릭하면 텍스트 포인터 정보 대신 실제 이미지가 정상 렌더링된다.
- LFS로 관리되는 압축파일 등 바이너리 클릭 시 다운로드 링크 버튼이 제공되며, 버튼을 클릭하면 원본 파일이 브라우저에서 다운로드된다.
</acceptance_criteria>
</task>

## must_haves

- LFS 포인터 판단은 LFS 규격 포인터 규칙(`version https://git-lfs.github.com/spec/v1`)을 정밀하게 준수해야 한다.
- 이미지가 아닌 LFS 파일은 텍스트 뷰어에 포인터가 노출되지 않도록 처리하고 다운로드 가이드를 명시해야 한다.
- Locks API는 `owner` 및 `repo`가 유효하지 않거나 권한이 없는 접근일 경우 401/403/404 처리를 동일하게 제공해야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Controllers/LfsApiController.cs` | 수정 | Locks API의 세부 비즈니스 로직(DB 연동 및 잠금 관리) 완성 |
| `Aristokeides.Api/Services/GitBrowserService.cs` | 수정 | LFS 포인터 여부 식별 및 OID/사이즈 파싱 로직 추가 |
| `Aristokeides.Api/Components/Pages/RepoBlob.razor` | 수정 | LFS 포인터 감지 후 인라인 이미지 렌더링 및 파일 다운로드 제공 |
