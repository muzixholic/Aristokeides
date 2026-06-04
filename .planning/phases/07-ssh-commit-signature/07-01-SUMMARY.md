---
phase: 07-ssh-commit-signature
plan: 01
subsystem: database
tags: [efcore, postgres, openssh, sshsig]

requires:
  - phase: 06-ssh-key-connectivity
    provides: SSH key registration and validation tools
provides:
  - CommitSignature 데이터 모델 정의 및 RepositoryId+CommitHash 복합 유니크 인덱스 생성
  - OpenSSH 서명 바이너리(SSHSIG) 파서 SshSignatureParser 구현
  - ssh-keygen -Y verify 프로세스 기반 SshSignatureVerifier 구현
affects:
  - 07-02-PLAN
  - 07-03-PLAN

tech-stack:
  added: []
  patterns: [ProcessStartInfo-based OS command integration, Big-endian binary stream parsing]

key-files:
  created:
    - Aristokeides.Api/Models/CommitSignature.cs
    - Aristokeides.Api/Services/Ssh/SshSignatureParser.cs
    - Aristokeides.Api/Services/Ssh/SshSignatureVerifier.cs
    - Aristokeides.Tests/SshSignatureTests.cs
  modified:
    - Aristokeides.Api/Models/Repository.cs
    - Aristokeides.Api/Data/AppDbContext.cs

key-decisions:
  - "ssh-keygen CLI가 Local OS 환경에 설치되어 작동해야 하므로 System32/OpenSSH의 ssh-keygen.exe 도구를 ProcessStartInfo로 실행하고 IO 파이핑을 연결하도록 구현함"
  - "서명 파일 파싱 및allowed_signers 임시 파일 관리를 안전하게 유지하기 위해 try-finally 패턴을 사용해 리크를 방지함"

patterns-established:
  - "ProcessStartInfo를 사용해 ssh-keygen을 감싸고 안전하게 실행하는 구조"

requirements-completed:
  - SSH-07

duration: 30min
completed: 2026-06-04
---

# Plan 07-01: SSH Commit Signature - Database & Crypto Layer Summary

**SSH 서명(SSHSIG) 바이너리 파서 및 ssh-keygen을 활용한 검증 코어 모듈 구축 완료**

## Performance

- **Duration:** 30 min
- **Started:** 2026-06-04T10:55:00Z
- **Completed:** 2026-06-04T11:25:00Z
- **Tasks:** 4
- **Files modified:** 9

## Accomplishments
- `CommitSignature` 데이터 모델 생성 및 EF Core PostgreSQL 마이그레이션 적용 완료.
- `SshSignatureParser`를 작성해 SSHSIG 바이너리 디코딩, Magic string 검증, Public Key 및 Fingerprint 추출 연동.
- `SshSignatureVerifier`에 `ssh-keygen -Y verify`를 프로세스로 연동하여 서명과 페이로드 검증 파이프라인 형성.
- `SshSignatureTests` 통합 단위 테스트 작성을 통해 서명 검증 성공/실패 케이스 확인.

## Task Commits

Each task was committed atomically:

1. **Tasks 1-4: SSH Commit Signature Core Implementation** - `00e280c` (feat)

## Files Created/Modified
- `Aristokeides.Api/Models/CommitSignature.cs` - 서명 검증 상태 저장 모델
- `Aristokeides.Api/Models/Repository.cs` - 서명 필수 여부 옵션 추가
- `Aristokeides.Api/Data/AppDbContext.cs` - 복합 인덱스 및 User/Repository와의 연동 관계 매핑
- `Aristokeides.Api/Services/Ssh/SshSignatureParser.cs` - SSHSIG 파서 구현
- `Aristokeides.Api/Services/Ssh/SshSignatureVerifier.cs` - ssh-keygen 기반 검증기 구현
- `Aristokeides.Tests/SshSignatureTests.cs` - 핵심 모듈 통합 테스트 코드

## Decisions Made
- `ssh-keygen` 명령어를 실행할 때, 로컬에 임시 `allowed_signers` 파일과 `sig` 파일을 생성하여 안전하게 넘기고 페이로드는 standard input 파이핑으로 써서 파일 노출을 최소화함.

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
- Local PostgreSQL 컨테이너가 Exited 상태여서 `dotnet ef database update` 수행 시 연결에 실패했음. `podman start`를 이용해 데이터베이스 컨테이너를 정상 가동시킨 후 업데이트를 정상 완료함.

## Next Phase Readiness
- 암호학적 검증 모듈과 데이터베이스 테이블 구조가 완비되었으므로, 다음 단계인 Git Integration 및 Push Hooks Layer(Plan 07-02)를 진행할 준비가 되었음.
