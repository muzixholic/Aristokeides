# Phase 24: SSH 호환성 개선 — PLAN 24-01

**Created:** 2026-06-11
**Status:** Ready
**Phase:** 24-ssh
**Scope:** SSH 서버 라이브러리 교체 (FxSsh 1.3.0 → Microsoft.DevTunnels.Ssh) + 호스트 키 하이브리드 지원 + DB 감사 로깅

---

## 기존 코드 현황 요약

### FxSsh 의존성 (NuGet `FxSsh 1.3.0`)
- **SshServerBackgroundService.cs** (342줄): 8개 FxSsh 타입 사용 (SshServer, Session, UserauthService 등)
- **SshCommandBridge.cs** (302줄): SessionChannel 기반 스트림 중계
- 리플렉션으로 `Session._socket` 접근 (원격 IP 획득) → 개선 필요
- stderr를 stdout으로 대체 전송 중 (FxSsh의 SendExtendedData 미지원) → 개선 필요

### 영향 없는 파일 (라이브러리 독립적)
- `SshKeyParser.cs` (112줄) — OpenSSH 공개키 파싱 (ed25519 이미 지원)
- `SshSignatureVerificationService.cs` (230줄) — 커밋 SSH 서명 검증
- `SshSignatureParser.cs`, `SshSignatureVerifier.cs`, `SshFingerprintCalculator.cs`
- `SshSessionState.cs`, `SshUrlHelper.cs`

### 기존 테스트 (6개 파일)
| 테스트 파일 | 의존성 | Phase 24 영향 |
|------------|--------|-------------|
| SshTDiagnosticTests.cs | FxSsh + Renci.SshNet | 재검증 필요 |
| SshServerAuthTests.cs | FxSsh + Renci.SshNet | 재검증 필요 |
| SshCommandPipingTests.cs | FxSsh + Renci.SshNet | 재검증 필요 |
| SshKeyParserTests.cs | 독립 | 변경 없음 |
| SshKeyRegistrationTests.cs | API 컨트롤러 | 변경 없음 |
| SshSignatureTests.cs | ssh-keygen CLI | 변경 없음 |

---

## Wave 1: 기반 인프라 (병렬 수행 가능)

### Task 24-01-01: SshAuthLog 엔티티 및 DB 마이그레이션

**목표:** SSH 인증 시도 감사 추적을 위한 DB 테이블 신규 생성 (D-24-04, D-24-05)

**수정 파일:**
- `Aristokeides.Api/Models/SshAuthLog.cs` (신규 생성)
- `Aristokeides.Api/Data/AppDbContext.cs` (DbSet 추가)

**구현 상세:**

1. `SshAuthLog` 엔티티 정의:
   ```csharp
   public class SshAuthLog
   {
       public int Id { get; set; }
       public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
       public string ClientIp { get; set; } = string.Empty;
       public string? KeyFingerprint { get; set; }
       public string? Username { get; set; }
       public bool IsSuccess { get; set; }
       public string? FailureReason { get; set; }
       public string? KeyType { get; set; }  // ssh-rsa, ssh-ed25519, ecdsa-sha2-nistp256
   }
   ```

2. `AppDbContext`에 `DbSet<SshAuthLog> SshAuthLogs` 추가

3. EF Core 마이그레이션 생성:
   ```
   dotnet ef migrations add AddSshAuthLog --project Aristokeides.Api
   ```

**검증:**
- [ ] 마이그레이션 파일 생성 확인
- [ ] `dotnet build` 성공

---

### Task 24-01-02: Microsoft.DevTunnels.Ssh NuGet 패키지 추가 및 FxSsh 제거

**목표:** 신규 SSH 라이브러리 의존성 추가, 기존 FxSsh NuGet 패키지 참조 제거

**수정 파일:**
- `Aristokeides.Api/Aristokeides.Api.csproj`

**구현 상세:**

1. FxSsh 패키지 제거:
   ```
   dotnet remove Aristokeides.Api package FxSsh
   ```

2. DevTunnels.Ssh 패키지 추가:
   ```
   dotnet add Aristokeides.Api package Microsoft.DevTunnels.Ssh
   ```

3. csproj에서 패키지 참조가 정상 교체되었는지 확인

**주의:** 이 시점에서는 빌드 에러가 발생합니다 (FxSsh 사용 파일에서 컴파일 오류). Wave 2에서 해결됩니다.

**검증:**
- [ ] `dotnet restore` 성공

---

## Wave 2: SSH 서버 핵심 교체

### Task 24-01-03: SshServerBackgroundService 전면 재작성

**목표:** FxSsh 기반 SSH 서버를 Microsoft.DevTunnels.Ssh 기반으로 교체 (D-24-01), 호스트 키 하이브리드 지원 (D-24-02, D-24-03) 및 DB 감사 로깅 통합 (D-24-04~06)

**수정 파일:**
- `Aristokeides.Api/Services/Ssh/SshServerBackgroundService.cs` (전면 재작성)

**구현 상세:**

1. **TcpListener 기반 연결 수락:**
   - FxSsh의 `SshServer` 클래스 대신 직접 `TcpListener`로 설정된 SSH 포트에서 연결 대기
   - 연결 수락 시 `NetworkStream`을 `SshServerSession`에 전달
   - 각 연결을 비동기 태스크로 처리 (CancellationToken 전파)

2. **호스트 키 하이브리드 로딩:**
   - `LoadOrGenerateHostKey()` 메서드 재작성:
     - **기존 host.key 존재 시 (D-24-02):** PEM에서 ECDsa P256 키를 로드 → `SshServerSession` 구성에 호스트 키로 추가
     - **host.key 미존재 시 (D-24-03):** ED25519 키 자동 생성 → PEM으로 저장 → 세션 구성에 추가
   - `KeyPair` 클래스의 팩토리 메서드 사용

3. **SshSessionConfiguration 구성:**
   ```csharp
   var config = new SshSessionConfiguration();
   // 지원 알고리즘: ed25519, rsa-sha2-256, rsa-sha2-512, ecdsa-sha2-nistp256
   config.AddService(typeof(AuthenticationService));
   ```

4. **인증 핸들러 재구현:**
   - `SshServerSession.Authenticating` 이벤트를 통해 공개키 인증 처리
   - DB에서 사용자 SSH 키 조회 → 키 지문 매칭 (기존 로직 보존)
   - 인증 결과를 `SshAuthLog` 테이블에 비동기 저장 (비차단형)
   - 기존 정적 변수 유지 (D-24-06):
     - `LastAuthFailureReason`
     - `LastAuthFailedUsername`

5. **원격 IP 획득 개선:**
   - FxSsh의 리플렉션 해킹 제거
   - `TcpClient.Client.RemoteEndPoint`에서 직접 IP 추출

6. **채널 요청 처리:**
   - 인증 성공 후 `ChannelOpening` → `ChannelRequest("exec")` → `SshCommandBridge`로 위임
   - 새 라이브러리의 `SshChannel` 스트림 API 활용

7. **감사 로깅 비동기 처리:**
   - `IServiceScopeFactory`로 Scoped `AppDbContext` 획득
   - 인증 시도마다 `SshAuthLog` 레코드 비동기 저장
   - 저장 실패 시 로그만 남기고 SSH 연결 흐름에 영향 없도록 처리

**검증:**
- [ ] SSH 서버가 정상 기동되는지 확인
- [ ] 기존 host.key (ECDsa P256) 로드 정상 동작
- [ ] 신규 설치 시 ED25519 키 자동 생성
- [ ] 원격 IP가 리플렉션 없이 정상 추출
- [ ] 인증 성공/실패가 DB `SshAuthLogs`에 기록되는지 확인
- [ ] `LastAuthFailureReason` / `LastAuthFailedUsername` 정적 변수 호환 유지

---

### Task 24-01-04: SshCommandBridge 라이브러리 전환

**목표:** FxSsh의 `SessionChannel` 타입을 Microsoft.DevTunnels.Ssh의 `SshChannel`로 교체하여 Git 명령 중계 로직 유지 + stderr 채널 개선

**수정 파일:**
- `Aristokeides.Api/Services/Ssh/SshCommandBridge.cs` (재작성)

**구현 상세:**

1. **메서드 시그니처 변경:**
   - FxSsh의 `Session` + `SessionChannel` → `SshChannel` + 컨텍스트 정보
   - FxSsh 타입 의존성 완전 제거

2. **스트림 릴레이 개선:**
   - FxSsh의 `DataReceived` 이벤트 기반 → `SshChannel`의 비동기 스트림 API (`ReadAsync`/`WriteAsync`)
   - `CopyToAsync` 패턴으로 양방향 복사 단순화
   - `CancellationToken` 전파로 채널 종료 시 자원 정리 보장

3. **stderr 전송 개선:**
   - FxSsh에서 불가능했던 stderr 분리 → `SshChannel`의 Extended Data 채널로 stderr 전송
   - 기존 `CopyStreamToChannelExtendedAsync` → 실제 extended data 스트림 사용

4. **Git 프로세스 연동 보존:**
   - 기존 `Process.Start()` → stdin/stdout/stderr 파이핑 로직 유지
   - 채널 EOF 시 프로세스 stdin 닫기
   - 프로세스 종료 시 exit-status 전송 후 채널 닫기
   - Push 후처리 로직 (서명 검증, PR 서비스, 웹훅) 그대로 유지

**검증:**
- [ ] `git-upload-pack` (clone/fetch) 정상 동작
- [ ] `git-receive-pack` (push) 정상 동작
- [ ] stderr 메시지가 클라이언트에서 정상 표시
- [ ] Push 후처리 (서명 검증, PR, 웹훅) 정상 동작

---

## Wave 3: 테스트 검증 및 정리

### Task 24-01-05: 기존 SSH 통합 테스트 적응 및 전체 검증

**목표:** SSH 라이브러리 전환 후 기존 통합 테스트 3개가 정상 통과하도록 보장 (D-24-06)

**수정 파일:**
- `Aristokeides.Tests/SshTDiagnosticTests.cs` (필요 시 수정)
- `Aristokeides.Tests/SshServerAuthTests.cs` (필요 시 수정)
- `Aristokeides.Tests/SshCommandPipingTests.cs` (필요 시 수정)

**구현 상세:**

1. **SshTDiagnosticTests 검증:**
   - `ssh -T` 진단 접속 → Welcome 메시지 + ExitCode 0 시나리오 통과
   - Renci.SshNet 클라이언트 코드는 변경 불필요 (서버만 교체)

2. **SshServerAuthTests 검증:**
   - 일반 셸 명령 거부 시나리오 통과
   - 디렉토리 트래버설 차단 시나리오 통과
   - `LastAuthFailureReason` 정적 변수 검증 통과

3. **SshCommandPipingTests 검증:**
   - git-upload-pack 파이핑 + 정상 종료 시나리오 통과

4. **변경 없음 예상 파일:**
   - `SshKeyParserTests.cs` — 라이브러리 독립적
   - `SshKeyRegistrationTests.cs` — API 컨트롤러 레벨
   - `SshSignatureTests.cs` — ssh-keygen CLI 의존

**검증:**
- [ ] `dotnet test` 전체 테스트 스위트 통과
- [ ] SSH 통합 테스트 3개 파일 모두 통과

---

### Task 24-01-06: SshAuthLog 감사 로깅 검증 테스트

**목표:** SSH 인증 시도가 DB에 정확히 기록되는지 검증하는 테스트 추가

**수정 파일:**
- `Aristokeides.Tests/SshAuthLogTests.cs` (신규 생성)

**구현 상세:**

1. **테스트 시나리오:**
   - 인증 성공 시 `SshAuthLog` 레코드 생성 확인 (IsSuccess=true, Username/Fingerprint 일치)
   - 인증 실패 시 `SshAuthLog` 레코드 생성 확인 (IsSuccess=false, FailureReason 포함)
   - ClientIp 필드에 원격 IP가 올바르게 기록되는지 확인
   - KeyType 필드가 올바르게 기록되는지 확인

2. **테스트 구조:**
   - 기존 SSH 통합 테스트 패턴 (Renci.SshNet + 로컬 서버) 재사용
   - 테스트 후 `AppDbContext`에서 `SshAuthLogs` 직접 조회하여 검증

**검증:**
- [ ] 감사 로그 테스트 4건 이상 통과

---

## 작업 흐름 요약

```
Wave 1 (기반 인프라) — 병렬 수행 가능
├── Task 24-01-01: SshAuthLog 엔티티 + 마이그레이션
└── Task 24-01-02: DevTunnels.Ssh NuGet 추가 + FxSsh 제거

Wave 2 (핵심 교체) — Wave 1 완료 후
├── Task 24-01-03: SshServerBackgroundService 재작성
└── Task 24-01-04: SshCommandBridge 전환

Wave 3 (검증) — Wave 2 완료 후
├── Task 24-01-05: 기존 SSH 통합 테스트 적응 + 전체 검증
└── Task 24-01-06: SshAuthLog 감사 로깅 테스트
```

## 핵심 리스크 및 대응

| 리스크 | 심각도 | 대응 |
|--------|--------|------|
| DevTunnels.Ssh의 서버 세션 API가 FxSsh와 크게 다름 | 높음 | microsoft/dev-tunnels-ssh GitHub 테스트 디렉토리 참조, exec 채널 핸들링 예제 확인 |
| 기존 host.key (ECDsa P256 PEM) 포맷 호환성 | 중간 | 하이브리드 로딩: PEM 로더 유지 + 신규 키는 ED25519 생성 |
| 스트림 릴레이 성능 변화 | 중간 | 이벤트 → async 전환 시 버퍼 크기 튜닝 (4KB → 8KB 검토) |
| Renci.SshNet 테스트 클라이언트 호환성 | 낮음 | 테스트 클라이언트는 서버 라이브러리에 독립적 — 프로토콜 호환만 확인 |
| FxSsh 리플렉션 해킹 제거 | 낮음 | TcpClient 직접 접근으로 깨끗하게 대체 |

## 의사결정 참조

| ID | 내용 |
|----|------|
| D-24-01 | FxSsh 1.3.0 → Microsoft.DevTunnels.Ssh 전면 교체 |
| D-24-02 | 기존 host.key (nistP256 ECDsa PEM) 하이브리드 로드 |
| D-24-03 | 신규 설치 시 ED25519 키 자동 생성 |
| D-24-04 | SshAuthLog 엔티티 DB 감사 테이블 구축 |
| D-24-05 | 인증 시도 상세 정보 (시각, IP, 지문, 사용자, 결과) 영구 저장 |
| D-24-06 | 기존 정적 변수 (LastAuthFailureReason 등) 하위 호환 보존 |

---

*Phase: 24-ssh*
*Plan created: 2026-06-11*
