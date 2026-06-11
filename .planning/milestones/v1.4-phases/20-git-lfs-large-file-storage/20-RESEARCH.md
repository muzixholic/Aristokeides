# Phase 20: Git LFS (Large File Storage) 지원 - Research

**Date:** 2026-06-10
**Status:** Completed

## 1. Git LFS API Specifications

Git LFS 클라이언트는 Git HTTP 전송 프로토콜의 확장으로 서버와 통신합니다. 모든 LFS 요청은 저장소 URL 뒤에 `/info/lfs/` 접두사를 붙여서 전송됩니다.
예: `https://localhost:5001/owner/repo.git/info/lfs/objects/batch`

### 1.1. Batch API (`POST /info/lfs/objects/batch`)
클라이언트가 업로드 또는 다운로드할 파일 목록(OID, 크기)을 보내면, 서버는 각 파일에 대해 수행할 수 있는 Action(업로드/다운로드 링크, 헤더, 만료 시간)을 응답합니다.

- **Request Headers:**
  - `Accept: application/vnd.git-lfs+json`
  - `Content-Type: application/vnd.git-lfs+json`
- **Request Body:**
  ```json
  {
    "operation": "upload",
    "transfers": [ "basic" ],
    "ref": { "name": "refs/heads/main" },
    "objects": [
      {
        "oid": "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824",
        "size": 12
      }
    ]
  }
  ```
- **Response Headers:**
  - `Content-Type: application/vnd.git-lfs+json`
- **Response Body (Upload - 파일이 서버에 없을 때):**
  ```json
  {
    "transfer": "basic",
    "objects": [
      {
        "oid": "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824",
        "size": 12,
        "actions": {
          "upload": {
            "href": "https://localhost:5001/api/lfs/upload/2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824",
            "header": {
              "Authorization": "Bearer <LFS_TEMP_TOKEN>"
            },
            "expires_in": 3600
          },
          "verify": {
            "href": "https://localhost:5001/api/lfs/verify",
            "header": {
              "Authorization": "Bearer <LFS_TEMP_TOKEN>"
            },
            "expires_in": 3600
          }
        }
      }
    ]
  }
  ```
- **Response Body (Upload - 파일이 이미 서버에 있을 때):**
  - `actions`를 비우거나 생략하여 업로드 단계를 건너뛰도록 지시합니다.
- **Response Body (Download):**
  ```json
  {
    "transfer": "basic",
    "objects": [
      {
        "oid": "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824",
        "size": 12,
        "authenticated": true,
        "actions": {
          "download": {
            "href": "https://localhost:5001/api/lfs/download/2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824",
            "header": {
              "Authorization": "Bearer <LFS_TEMP_TOKEN>"
            },
            "expires_in": 3600
          }
        }
      }
    ]
  }
  ```

### 1.2. Locks API
파일 충돌을 예방하기 위해 특정 바이너리 파일을 잠그는 기능입니다.

- **Lock 생성 (`POST /info/lfs/locks`):**
  - Request: `{ "path": "images/banner.png" }`
  - Response (201 Created):
    ```json
    {
      "lock": {
        "id": "123",
        "path": "images/banner.png",
        "locked_at": "2026-06-10T12:00:00Z",
        "owner": { "name": "username" }
      }
    }
    ```
- **Lock 목록 조회 (`GET /info/lfs/locks`):**
  - Response (200 OK):
    ```json
    {
      "locks": [
        {
          "id": "123",
          "path": "images/banner.png",
          "locked_at": "2026-06-10T12:00:00Z",
          "owner": { "name": "username" }
        }
      ]
    }
    ```
- **Lock 검증 (`POST /info/lfs/locks/verify`):**
  - Request: `{ "cursor": "...", "limit": 100 }`
  - Response (200 OK):
    ```json
    {
      "ours": [ { "id": "123", "path": "images/banner.png", "locked_at": "2026-06-10T12:00:00Z" } ],
      "theirs": []
    }
    ```
- **Lock 해제 (`POST /info/lfs/locks/{id}/unlock`):**
  - Request: `{ "force": false }`
  - Response (200 OK):
    ```json
    {
      "lock": {
        "id": "123",
        "path": "images/banner.png",
        "locked_at": "2026-06-10T12:00:00Z",
        "owner": { "name": "username" }
      }
    }
    ```

---

## 2. Storage & Duplication

### 2.1. 로컬 글로벌 스토리지
- 디렉토리 구조 오버헤드를 방지하고 중복을 제거하기 위해 SHA-256 OID 기반의 글로벌 디렉토리를 사용합니다.
- 경로 규칙: `GitRepos/lfs/objects/{oid[0..1]}/{oid[2..3]}/{oid}`
- 파일 쓰기 시 임시 디렉토리에 쓰고 검증한 후 목적지로 이동(Atomic Move)하여 파일 깨짐을 방지합니다.

---

## 3. Security & Authentication

- **1차 인증:** LFS Batch API 호출 시 Basic Authentication을 수행하여 유효한 사용자인지 검증합니다.
- **임시 토큰 (LFS JWT):**
  - Batch API 응답 시 업로드/다운로드 URL에 실어 보낼 Bearer 토큰을 발급합니다.
  - JWT에 `repo_id`, `oid`, `action` (upload/download), `exp` (1시간 만료) 클레임을 담아 발행합니다.
  - 파일 전송 컨트롤러(`LfsTransferController`)에서 해당 JWT를 파싱하고 서명을 검증하여 유효 기간 및 접근 권한을 최종 판단합니다.

---

## 4. UI Integration (LFS Pointer Resolution)

### 4.1. LFS Pointer 식별 조건
저장소 브라우저에서 읽어온 파일의 내용이 아래와 같을 때 LFS 포인터로 인지합니다.
- 파일 크기가 300바이트 이하
- 첫 줄이 `version https://git-lfs.github.com/spec/v1`로 시작
- 내용에 `oid sha256:abcdef...` 및 `size 12345` 형식의 메타데이터가 존재

### 4.2. UI 렌더링 대체 로직
- Blazor 저장소 뷰어(`RepoBlob.razor` 등)에서 LFS 포인터를 감지하면:
  - 텍스트 뷰어 대신, 추출한 OID를 기반으로 실제 이미지나 데이터를 로컬 글로벌 스토리지에서 찾아 읽어옵니다.
  - 이미지 타입일 경우 `data:image/...;base64,...` 포맷 또는 전용 임시 다운로드 API 링크를 통해 화면에 렌더링합니다.
  - 기타 바이너리는 "LFS 파일 다운로드" 버튼을 제공하여 사용자가 다운로드할 수 있게 유도합니다.

---

## 5. Validation Architecture (Nyquist Validation)

- **V-01 (LFS API):** `POST /info/lfs/objects/batch`에 대해 올바른 `application/vnd.git-lfs+json` 요청/응답이 보장되는지 API 단위 테스트 작성.
- **V-02 (Token Exp):** 1시간이 경과한 임시 JWT를 사용해 업로드/다운로드를 시도할 때 401 Unauthorized가 리턴되는지 시나리오 테스트.
- **V-03 (Locks API):** 이미 락이 걸린 파일에 대해 타인이 락 생성을 시도할 때 409 Conflict 오류 및 세부 정보가 리턴되는지 검증.
- **V-04 (UI Detection):** 가상 LFS 포인터 파일을 생성하여 UI 브라우저 컴포넌트가 이를 LFS 포인터로 정상 식별하는지 단위 테스트 작성.
