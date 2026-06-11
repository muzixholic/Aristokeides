# Phase 20: Git LFS (Large File Storage) 지원 - User Acceptance Testing (UAT)

**Date:** 2026-06-10
**Status:** Completed

## 1. Acceptance Test Cases

### UAT-1: Git LFS 파일 Push 및 저장소 보관 검증
- **테스트 환경:** 로컬 개발 터미널 및 Git LFS 클라이언트가 설치된 PC
- **시나리오:**
  1. 로컬 Git 저장소에서 `git lfs install`을 실행하여 초기화합니다.
  2. `git lfs track "*.zip"` 또는 `git lfs track "*.png"`를 실행하여 대상을 지정합니다.
  3. 10MB 크기의 zip 파일을 저장소에 생성하고 `git add` 및 커밋합니다.
  4. `git push origin main` 명령을 통해 원격 서버로 푸시를 수행합니다.
- **예상 결과:**
  - Git LFS Batch API 호출 및 파일 업로드 전송이 정상 완료됩니다.
  - 서버 측 `GitRepos/lfs/objects/xx/yy/...` 스토리지 경로에 파일이 생성되고 크기가 원본과 동일하게 일치합니다.
  - DB `LfsObjects` 테이블에 해당 OID의 메타데이터 레코드가 정상 추가됩니다.

### UAT-2: LFS Locks API를 통한 잠금 및 충돌 제어 검증
- **테스트 환경:** 두 명의 서로 다른 테스트 계정 (User-A, User-B)
- **시나리오:**
  1. User-A가 LFS 명령어를 이용해 파일 잠금을 신청합니다: `git lfs lock "images/banner.png"`
  2. 서버 DB `LfsLocks`에 잠금 정보가 정상 기록되는지 확인합니다.
  3. User-B가 동일한 경로에 잠금을 시도합니다: `git lfs lock "images/banner.png"`
  4. User-B가 잠금이 걸린 파일에 대해 임의로 해제를 요청합니다: `git lfs unlock "images/banner.png"`
  5. User-A가 락을 안전하게 해제합니다: `git lfs unlock "images/banner.png"`
- **예상 결과:**
  - User-A의 락 요청은 201 Created로 즉시 성공합니다.
  - User-B의 락 시도는 409 Conflict 오류를 반환하며, 화면에 `already locked` 메시지와 User-A의 소유권 정보가 출력됩니다.
  - User-B의 락 해제 시도는 403 Forbidden으로 실패 처리됩니다.
  - User-A의 락 해제 요청은 200 OK로 처리되어 DB에서 락 레코드가 지워집니다.

### UAT-3: 웹 UI 상에서의 LFS 이미지 렌더링 검증
- **테스트 환경:** 웹 브라우저 (Chrome, Edge 등)
- **시나리오:**
  1. 웹 UI 대시보드에서 이미지가 LFS로 저장되어 있는 리포지토리 브라우저로 이동합니다.
  2. LFS 포인터로 기록된 이미지 파일(예: `logo.png`)을 클릭하여 봅니다.
- **예상 결과:**
  - 파일 내용이 `version https://...` 텍스트로 보이지 않고, 실제 이미지 원본 파일이 인라인으로 디코딩되어 화면에 미리보기 형태로 정상 노출됩니다.

### UAT-4: 웹 UI 상에서의 LFS 바이너리 다운로드 검증
- **테스트 환경:** 웹 브라우저
- **시나리오:**
  1. 리포지토리 브라우저에서 LFS로 저장된 압축 파일(예: `resource.zip`)을 클릭합니다.
- **예상 결과:**
  - 화면에 "대용량 LFS 파일" 및 파일 크기 요약 정보와 함께 전용 다운로드 버튼이 노출됩니다.
  - 다운로드 버튼을 클릭하면 `api/lfs/.../download/...` URL을 통해 원본 zip 파일이 정상 다운로드되고 정상 해제되는지 확인됩니다.
