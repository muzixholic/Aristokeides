---
title: "Git LFS Batch 비즈니스 로직 완성 및 파일 전송 API 구현"
phase: 20
wave: 2
depends_on: ["20A"]
files_modified:
  - Aristokeides.Api/Services/LfsService.cs
  - Aristokeides.Api/Controllers/LfsApiController.cs
  - Aristokeides.Api/Controllers/LfsTransferController.cs
autonomous: true
requirements:
  - "LfsService를 구현하여 단기 파일 업로드/다운로드용 JWT 토큰 생성/검증 로직과 로컬 글로벌 LFS 스토리지 입출력을 캡슐화한다."
  - "LfsApiController의 Batch API에 실제 비즈니스 로직을 연결하여, 파일이 이미 서버에 존재하면 Actions를 생략하고, 없는 경우 업로드/다운로드/검증 액션을 임시 JWT 토큰과 함께 반환한다."
  - "LfsTransferController를 신규 구현하여 파일 업로드(PUT), 다운로드(GET), 검증(Verify, POST) 엔드포인트를 구현하고 실제 파일을 로컬 스토리지에 저장/조회한다."
---

# Plan 20B: Git LFS Batch 비즈니스 로직 완성 및 파일 전송 API 구현

## Objective

LFS 클라이언트와 실제 대용량 데이터를 주고받는 전송(Transfer) 단계와 토큰 인증을 통합 구현합니다. 단기 JWT 토큰을 발행하여 파일 업로드/다운로드 작업을 인가하며, 중복 방지가 가능한 로컬 스토리지(`GitRepos/lfs/objects/...`) 백엔드 시스템을 연동하고, 임시 파일 쓰기 및 원자적(Atomic) 파일 이동 규칙을 적용해 데이터 신뢰성을 확보합니다.

## Tasks

<task id="20B-1">
<title>LfsService 구현</title>
<read_first>
- `Aristokeides.Api/Program.cs` — 기존 JWT 설정 키 로드 및 인증 패턴 참조
</read_first>
<action>
1. `Aristokeides.Api/Services/LfsService.cs` 신규 구현:
   - **토큰 관리:** `JwtSecurityTokenHandler`를 사용해 단기(예: 1시간 유효) LFS 전용 JWT 토큰을 생성합니다. 클레임에는 `repoId` (Guid), `oid` (string), `action` ("upload" | "download")을 포함합니다.
   - **토큰 검증:** 파일 전송 API 호출 시 인계받은 Bearer 토큰의 서명 및 유효 기간을 검증하여 요청된 `repoId`, `oid`, `action`이 맞는지 대조하고 검증 결과를 반환합니다.
   - **로컬 글로벌 스토리지 파일 경로 추출:** OID를 토대로 `GitRepos/lfs/objects/{oid[0..1]}/{oid[2..3]}/{oid}` 파일 저장 경로를 결정하는 헬퍼 메서드를 만듭니다.
   - **파일 저장소 확인:** 지정된 OID의 파일이 로컬 스토리지에 존재하고 크기가 일치하는지 확인하는 메서드를 제공합니다.
2. `Program.cs`에 `LfsService`를 Scoped 서비스로 등록합니다.
</action>
<acceptance_criteria>
- `LfsService`를 통한 단기 토큰 생성 및 검증이 단위 테스트 수준에서 문제없이 동작한다.
- OID에 따라 로컬 스토리지 폴더 트리가 정확한 디렉토리 분할 규칙(예: `ab/cd/abcdef...`)으로 추출된다.
</acceptance_criteria>
</task>

<task id="20B-2">
<title>LfsApiController의 Batch API 핵심 로직 구현</title>
<read_first>
- `Aristokeides.Api/Controllers/LfsApiController.cs` — 20A단계에서 작성한 스켈레톤 참조
</read_first>
<action>
1. `LfsApiController.cs`에 `LfsService` 및 `AppDbContext` 주입.
2. `POST /objects/batch` 액션의 바디 데이터 분석 및 처리:
   - 클라이언트 요청의 `operation` ("upload" 또는 "download") 판별.
   - 각 `objects` 내 OID 및 Size에 대하여:
     - **Upload 시:** `LfsService`를 통해 해당 파일이 로컬 스토리지에 이미 완전히 업로드되어 존재하고 크기가 일치하는지 확인.
       - 존재 시: `actions` 블록을 생성하지 않고 전달 (업로드 생략 유도).
       - 미존재 시: `LfsService`로 `action = "upload"`, `action = "verify"` 토큰을 발급하여 `actions` 아래 `upload` 및 `verify` 액션 정보(href, Authorization 헤더, expires_in)를 응답 데이터로 채움.
     - **Download 시:** 파일 존재 여부를 체크.
       - 존재 시: `LfsService`로 `action = "download"` 토큰을 발급하여 `actions` 아래 `download` 액션 정보를 응답에 포함.
       - 미존재 시: 응답 개체에 `error` 정보를 기입 (404 Object Not Found 응답 처리).
   - 최종 응답 포맷 헤더를 `application/vnd.git-lfs+json`으로 설정하여 반환.
</action>
<acceptance_criteria>
- LFS Batch API 호출 시 이미 로컬 스토리지에 존재하는 OID의 경우 Actions 정보 없이 200 OK 응답이 리턴된다.
- 존재하지 않는 OID 업로드 요청 시 적절한 JWT 토큰과 함께 `upload` 및 `verify` Actions 경로가 리턴된다.
</acceptance_criteria>
</task>

<task id="20B-3">
<title>LfsTransferController 파일 업로드, 다운로드, 검증 구현</title>
<read_first>
- `Aristokeides.Api/Services/LfsService.cs` — 앞서 구현한 토큰 및 스토리지 핸들링 기능 참조
</read_first>
<action>
1. `Aristokeides.Api/Controllers/LfsTransferController.cs` 신규 구현:
   - 라우팅: `[Route("api/lfs/{owner}/{repo}")]`
   - 헤더 검증: LFS 전송용 단기 JWT Bearer 토큰 검증 로직 구현. (요청의 `Authorization` 헤더에서 토큰 추출 후 `LfsService`를 통해 유효성 판단)
   - **Download (GET `/download/{oid}`):**
     - 토큰의 `action`이 "download" 이고 대상 `oid` 및 저장소가 맞는지 검증.
     - 해당 파일 스트림을 로컬 글로벌 스토리지에서 읽어 반환 (`FileStreamResult`).
   - **Upload (PUT `/upload/{oid}`):**
     - 토큰의 `action`이 "upload" 인지 검증.
     - 파일 깨짐 방지를 위해 임시 파일 경로(`GitRepos/lfs/temp/...`)를 생성하여 업로드되는 바디 스트림을 저장.
     - 업로드 완료 시, 파일의 실제 크기 및 SHA-256 OID 해시 계산 검증.
     - 검증 성공 시, 글로벌 스토리지 저장 경로(`GitRepos/lfs/objects/xx/yy/...`)로 파일을 안전하게 이동(Atomic Move) 및 디렉토리 생성 처리.
   - **Verify (POST `/verify/{oid}`):**
     - 토큰의 `action`이 "verify" 인지 검증.
     - 요청 바디에서 OID와 Size를 파싱하여 글로벌 스토리지에 실제 정상적으로 존재하는지 확인.
     - 확인 완료 후 `LfsObject` 메타데이터 엔터티를 DB에 등록 및 `AppDbContext` 저장. 200 OK 반환.
</action>
<acceptance_criteria>
- 유효하지 않거나 만료된 임시 토큰으로 다운로드/업로드 요청 시 401 Unauthorized 혹은 403 Forbidden이 리턴된다.
- 파일 PUT 업로드 완료 후 OID 검증을 거쳐 `LfsObject` 데이터가 데이터베이스에 정상 기입된다.
- GET 다운로드 시 파일 바이너리가 바이트 단위 유실 없이 원래대로 정상 전송된다.
</acceptance_criteria>
</task>

## must_haves

- PUT 업로드 시 스트림 바디를 받아 임시 파일에 쓰고, 전체 수신 완료 후 OID 해시(SHA-256) 및 파일 크기 무결성 검사를 강제해야 한다.
- 모든 파일 전송 엔드포인트는 단기 JWT 임시 토큰의 서명 및 세부 클레임(저장소ID, OID, 권한액션) 검증을 필수로 수행해야 한다.
- Batch API 응답의 Content-Type은 엄격하게 `application/vnd.git-lfs+json` 규격을 준수해야 한다.

## Artifacts this phase produces

| 파일 | 유형 | 설명 |
|---|---|---|
| `Aristokeides.Api/Services/LfsService.cs` | 신규 | LFS 인증 토큰 발급/검증 및 로컬 스토리지 입출력 서비스 |
| `Aristokeides.Api/Controllers/LfsApiController.cs` | 수정 | Batch API 비즈니스 로직 보강 및 LfsService 연동 |
| `Aristokeides.Api/Controllers/LfsTransferController.cs` | 신규 | LFS 업로드/다운로드/검증 파일 전송 API 컨트롤러 |
