# Phase 20: Git LFS (Large File Storage) 지원 - Validation Strategy

**Date:** 2026-06-10
**Status:** Completed

## 1. Verification Dimensions

### Dimension 1: Functional Correctness (기능적 정확성)
- LFS Batch API, Locks API 및 파일 전송 API가 Git LFS v1 스펙 규격을 정상적으로 준수하는지 통합 테스트 수준에서 검증합니다.
- LFS 포인터 파일의 메타데이터(OID, 크기)가 정상 파싱되어 웹 UI에 전달되는지 검사합니다.

### Dimension 2: Security & Isolation (보안 및 격리)
- LFS JWT 토큰 검증 로직이 불법적이거나 만료된 접근에 대해 401/403/404를 정상 반환하는지 체크합니다.
- 허용되지 않은 타인이 리포지토리의 파일 락을 강제 해제하거나 조작하는 일이 없는지 검사합니다.

### Dimension 3: Robustness & Data Integrity (복구력 및 무결성)
- 파일 업로드 중 예기치 못한 중단 시, 쓰기 중이던 임시 파일이 정리되고 원래 보관된 파일의 깨짐이 없는지 확인합니다.
- 대용량 파일 업로드 시 OID SHA-256 값이 불일치할 경우, 파일이 스토리지로 마이그레이션되지 않고 적절한 오류 상태를 리턴하는지 테스트합니다.

---

## 2. Test Execution Plan

### 2.1. Integration Test Cases (`Aristokeides.Tests/LfsTests.cs`)
- `Test_Lfs_Batch_Upload_NewFile`: 신규 OID 업로드 요청 시 Actions(Href, Header)가 리턴되는지 검사.
- `Test_Lfs_Batch_Upload_ExistingFile`: 이미 업로드된 OID 요청 시 Actions 블록이 배제되어 반환되는지 검사.
- `Test_Lfs_Transfer_Upload_And_Verify`: 단기 JWT를 발급해 PUT `/upload/{oid}`로 데이터 전송 후 POST `/verify/{oid}` 호출을 수행하여 최종적으로 `LfsObject` DB 테이블에 등재되는 전 흐름 테스트.
- `Test_Lfs_Locks_Conflict`: 동일 경로 파일에 대하여 다중 사용자 잠금 요청 시 409 Conflict 응답 구조 대조 검증.
- `Test_Lfs_Locks_Unlock_Permissions`: 타인의 락 해제 시 권한 제어 오류(403)가 정상 작동하는지 대조 검증.

### 2.2. Unit Test Cases
- `Test_Lfs_Token_Generation_And_Validation`: `LfsService`에서 생성된 토큰이 지정된 만료 시간(1시간) 이내에 올바르게 복호화 및 유효성 검증을 마쳐야 함.
- `Test_Lfs_Pointer_Detection`: `GitBrowserService` 가상의 LFS 포인터 텍스트 포맷을 받아 OID 및 Size를 정확히 파싱해내는지 확인.
