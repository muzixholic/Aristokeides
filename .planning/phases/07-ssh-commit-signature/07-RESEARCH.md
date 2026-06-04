# Phase 7: SSH Commit Signature - Research

이 문서는 Phase 7 (SSH 키 기반 커밋 디지털 서명 서버 검증 및 Verified 배지 표시) 구현을 위한 기술 분석 및 아키텍처 리서치 결과를 기술합니다.

---

## 1. Domain Knowledge: SSH 커밋 서명 검증

Git은 커밋 서명 시 GPG 외에도 SSH 키를 사용할 수 있는 기능(`gpg.format = ssh`)을 기본적으로 지원합니다.

### 1.1 Git 커밋 내 SSH 서명 구조
서명된 커밋은 내부적으로 다음과 같이 `gpgsig` 헤더를 포함하는 원시 텍스트 포맷으로 구성됩니다.
```text
tree [SHA]
parent [SHA]
author [Name] <[Email]> [Timestamp]
committer [Name] <[Email]> [Timestamp]
gpgsig -----BEGIN SSH SIGNATURE-----
 U3NoU2lnAAAAAXNzaC1rZXktZ2VuAAAAABVzc2gtcnNhLXNoYTItNTEyAAAAMA...
 -----END SSH SIGNATURE-----

[Commit Message]
```
- `gpgsig`는 커밋 헤더 필드에 들어갑니다.
- 서명 본문은 여러 줄에 걸쳐 들어가며, 들여쓰기(공백 문자)로 구분됩니다.
- 서명 검증 대상 **페이로드(Payload)**는 원시 커밋 데이터에서 `gpgsig` 헤더 라인 전체(필드 식별자 및 들여쓰기된 서명 본문 전체)를 제외한 텍스트 바이트 배열입니다.

### 1.2 OpenSSH 서명 바이너리 구조 (SSHSIG)
서명 블록의 Base64 디코딩 데이터는 다음과 같은 OpenSSH 서명 바이너리 포맷(RFC 4253 및 관련 명세)을 따릅니다.
1. **Magic String** (6 bytes): `"SSHSIG"`
2. **Version** (4 bytes, uint32): `1`
3. **Public Key Length** (4 bytes, uint32): `L`
4. **Public Key Data** (`L` bytes): 서명에 사용된 공개키의 원시 구조화 바이너리 (알고리즘 헤더 포함)
5. **Namespace Length** (4 bytes, uint32)
6. **Namespace Data** (variable): Git의 경우 항상 `"git"`
7. **Reserved Length** (4 bytes, uint32)
8. **Hash Algorithm Length** (4 bytes, uint32)
9. **Hash Algorithm Data** (variable): `"sha256"` 또는 `"sha512"`
10. **Signature Length** (4 bytes, uint32)
11. **Signature Data** (variable): 실제 서명 값

서명 데이터 내에 **서명용 공개키 원본(Public Key Data)**이 그대로 담겨 있으므로, 서명을 파싱하여 공개키의 바이트 배열을 추출할 수 있습니다. 
이를 통해 서명에 사용된 공개키의 **SHA-256 지문(Fingerprint)**을 계산하여 DB에 일치하는 등록 키가 있는지 선제적으로 쿼리할 수 있습니다.

---

## 2. Technology Options (.NET SSH 서명 검증)

### Option A: `ssh-keygen -Y verify` 외부 프로세스 실행 (추천)
Git 자체도 SSH 커밋 검증 시 내부적으로 `ssh-keygen` 명령을 호출합니다.
- **방법**: 
  1. `allowed_signers` 형식의 임시 파일 작성: `[식별자] [알고리즘] [공개키Base64]`
  2. 페이로드 데이터를 임시 파일(`payload.tmp`)에 저장
  3. 서명 텍스트(`-----BEGIN SSH SIGNATURE-----` ... `-----END SSH SIGNATURE-----`)를 임시 파일(`sig.tmp`)에 저장
  4. 명령어 실행: `ssh-keygen -Y verify -f allowed_signers -I [식별자] -n git -s sig.tmp < payload.tmp`
  5. 프로세스 종료 코드가 `0`이면 검증 성공
- **장점**: 
  - 호스트 OS(Linux/Windows)에 설치된 OpenSSH 바이너리를 그대로 활용하므로 라이브러리 간 정밀한 암호학적 파싱 호환 문제 및 알고리즘 구현 복잡도를 배제할 수 있습니다.
  - 보안성이 검증된 표준 도구를 사용합니다.
- **단점**: 프로세스 생성 비용이 발생합니다. (Push 완료 시점 비동기 백그라운드 처리로 해결 가능)

### Option B: BouncyCastle 등 C# 암호 라이브러리로 순수 코드 구현
- **방법**: C# 라이브러(BouncyCastle 등)를 이용해 Ed25519, RSA, ECDSA 등의 서명을 직접 검증하고 SHA-256 디코딩 처리를 작성합니다.
- **장점**: 외부 프로세스 의존성이 없습니다.
- **단점**: SSH 서명 봉투 포맷(SSHSIG) 해독 및 다양한 키 유형별 암호 알고리즘 파싱 로직을 직접 구현해야 하여 개발 리스크가 매우 큽니다.

---

## 3. Codebase Analysis (기존 패턴 및 연동 지점)

### 3.1 LibGit2Sharp를 통한 커밋 서명 추출
`LibGit2Sharp 0.31.0`에서는 커밋 서명을 추출하기 위해 `ObjectDatabase`의 원시 데이터를 파싱하거나, API가 제공하는 경우 `repo.Commits.ExtractSignature`를 사용할 수 있습니다.
가장 확실한 폴백 방식은 커밋 오브젝트의 Raw 데이터를 직접 읽는 것입니다:
```csharp
var objectId = new ObjectId(commitHash);
var rawObject = repo.ObjectDatabase.RetrieveObjectRaw(objectId, ObjectType.Commit);
byte[] rawData = rawObject.Data;
string rawText = Encoding.UTF8.GetString(rawData);
```
`rawText` 헤더부에서 `gpgsig` 블록을 파싱하고, 해당 블록을 제거한 데이터로 서명 페이로드를 생성합니다.

### 3.2 Push 연동 트리거 (Integration Points)

#### 1) SSH Push 트리거 (`SshCommandBridge.cs`)
- 사용자가 SSH를 통해 Push하면 `RunGitCommandAsync`에서 `commandName`이 `"git-receive-pack"`인 프로세스가 실행됩니다.
- `process.WaitForExitAsync()`가 호출되어 정상 종료(`ExitCode == 0`)된 직후에 비동기 서명 검증 서비스를 실행합니다.
```csharp
// SshCommandBridge.cs
await process.WaitForExitAsync();
if (process.ExitCode == 0 && commandName == "git-receive-pack")
{
    // 비동기 검증 파이프라인 트리거
    _ = Task.Run(() => _signatureService.VerifyNewCommitsAsync(repoPath));
}
```

#### 2) HTTP Push 트리거 (`GitSmartHttpMiddleware.cs`)
- 사용자가 HTTP를 통해 Push하면 `GitSmartHttpMiddleware` 내에서 `git http-backend` 프로세스가 실행됩니다.
- HTTP Request Path가 `git-receive-pack`으로 끝나는 POST 요청이고 프로세스가 정상 완료되었을 때 검증 서비스를 실행합니다.
```csharp
// GitSmartHttpMiddleware.cs
await process.WaitForExitAsync();
if (context.Request.Method == "POST" && context.Request.Path.Value.EndsWith("git-receive-pack"))
{
    _ = Task.Run(() => _signatureService.VerifyNewCommitsAsync(repoNameWithUsername));
}
```

### 3.3 DB 모델 설계 및 EF Core 연동 (`CommitSignature`)
- **모델 명세**:
  ```csharp
  public class CommitSignature
  {
      public int Id { get; set; }
      public required string CommitHash { get; set; }
      public Guid RepositoryId { get; set; }
      public Repository? Repository { get; set; }
      public int? SignerUserId { get; set; }
      public User? SignerUser { get; set; }
      public required string Status { get; set; } // Verified, Invalid, Unknown, NoSignature
      public string? Algorithm { get; set; }
      public string? KeyFingerprint { get; set; }
      public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
  }
  ```
- **AppDbContext 제약 조건**:
  - `RepositoryId + CommitHash` 복합 유니크 키 인덱스 지정.
  - `SignerUserId` 삭제 제약 조건은 `DeleteBehavior.SetNull` 또는 `DeleteBehavior.Restrict` 지정 (키/사용자가 삭제되어도 기록 보존을 위해 `SetNull` 적용).

### 3.4 UI 연동 (`RepoCommits.razor`)
- 커밋 히스토리 조회 시 `CommitSignature` 테이블을 Left Join 하여 상태값을 획득합니다.
- `GitCommitInfo` 레코드를 확장하여 `SignatureStatus`, `SignatureFingerprint` 등을 추가합니다.
- `RepoCommits.razor`에 5번째 컬럼인 "서명"을 추가하고, Verified 상태일 때 GitHub 스타일 연두색 Verified 배지를 표시합니다.
  - 스타일: `background-color: #dafbe1; color: #1a7f37; border: 1px solid #1a7f37; border-radius: 4px; padding: 2px 6px; font-size: 0.75rem; font-weight: bold;`

---

## 4. Risks and Unknowns

1. **외부 프로세스(ssh-keygen) 가용성**: 호스트 OS에 `ssh-keygen`이 경로에 설정되어 있지 않으면 오동작 위험이 있습니다.
   - *대책*: 시작 시 `ssh-keygen` 가용성을 검증하여 불가능할 시 백로그에 에러 로그를 기록하거나 검증 로직을 우회할 수 있는 진단/경보 도구를 구성합니다.
2. **동시성 처리**: 다량의 커밋이 푸시될 때 동시 검증 처리가 파일 I/O 충돌을 유발할 수 있습니다.
   - *대책*: 서명 검증 시 고유한 파일명(GUID 임시 파일)을 사용하여 격리된 공간에서 검증을 수행하고, 작업 완료 후 반드시 삭제하도록 방어적으로 처리합니다.

---

## 5. Recommended Approach

1. `CommitSignature` 엔티티 정의 및 DB Migration 생성.
2. `SshKey` 모델의 `PublicKey` 원본 필드와 `SshKeys` 테이블 인덱스 유지.
3. OpenSSH 서명 문자열 파서 및 `ssh-keygen -Y verify`를 대행하는 통합 검증 서비스(`SshSignatureVerifier`) 구현.
4. SSH/HTTP Push 종료 시 해당 서비스를 이벤트성 비동기 큐나 백그라운드 태스크로 연동.
5. Blazor 웹 UI 커밋 상세 및 히스토리에 초록색 "Verified" 배지 추가.

## Validation Architecture

### 1. 단위 테스트 범위
- SSH 서명 바이너리 디코더 테스트 (SSHSIG 구조로부터 공개키 및 지문 추출 기능 검증).
- `ssh-keygen` 래퍼 서비스 작동성 검증 (임의 데이터 및 가짜 서명을 만들어 검증 실패 확인, 올바른 서명으로 검증 성공 확인).

### 2. 통합 검증 및 시나리오 테스트
- 서명된 커밋 푸시 후 DB 내 `CommitSignature` 상태가 `Verified`로 정상 반영되는지 확인.
- 미서명 커밋 푸시 후 `NoSignature` 상태가 기록되는지 확인.
- 등록되지 않은 SSH 키로 서명된 커밋 푸시 시 `Unknown` 상태가 기록되는지 확인.
- 웹 UI 커밋 페이지에 "Verified" 마크다운/HTML 라벨 노출 확인.

## RESEARCH COMPLETE
