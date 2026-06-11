# Phase 24: SSH 호환성 개선 - Context

**Gathered:** 2026-06-11
**Status:** Ready for planning

<domain>
## Phase Boundary

임베디드 SSH 서버 모듈의 암호 알고리즘 지원 스펙을 ed25519 및 rsa-sha2로 현대화하여 최신 SSH 클라이언트와의 접속 호환성을 보장합니다. 기존 FxSsh 라이브러리 모델 대신 Microsoft.DevTunnels.Ssh 라이브러리로 전면 교체하며, 보안성 강화 및 이력 관리를 위해 SSH 로그인 및 인증 시도 기록을 보관할 데이터베이스 감사 테이블(SshAuthLog)을 신규 설계하고 구현합니다.

</domain>

<decisions>
## Implementation Decisions

### SSH 서버 라이브러리 교체
- **D-24-01:** 기존의 FxSsh 라이브러리 의존성을 제거하고, 현대적 암호화 표준을 자체적으로 내장 지원하며 Microsoft가 관리하는 `Microsoft.DevTunnels.Ssh` 라이브러리로 전면 교체합니다. 이를 통해 `ed25519`, `rsa-sha2-256`, `rsa-sha2-512` 알고리즘을 기본 탑재하고 호환성을 확보합니다.

### 호스트 키(Host Key) 하이브리드 지원 및 생성 정책
- **D-24-02:** 기존에 생성된 `host.key` (nistP256 ECDsa PEM 포맷) 파일이 디스크에 존재할 경우 호스트 키 인증을 위해 정상적으로 이를 로드하여 서빙을 개시합니다.
- **D-24-03:** `host.key` 파일이 존재하지 않는 신규 설치나 설치 마법사 기동 시에는 최신의 안전한 `ED25519` 또는 `RSA` 키를 기본 포맷으로 신규 자동 생성하여 `host.key` 파일로 저장 및 바인딩하도록 구현합니다.

### SSH 인증 시도 DB 감사 로깅 (Audit Trail)
- **D-24-04:** SSH 로그인 시도의 감사 추적을 위해 데이터베이스에 `SshAuthLog` 엔티티를 추가하고 데이터베이스 마이그레이션을 구성합니다.
- **D-24-05:** SSH 연결 시도가 있을 때마다 로그인 시도 시각, 접속 IP 주소, 제공된 SSH 키 지문(Fingerprint), 매칭을 시도한 사용자명, 그리고 성공 여부와 실패 사유를 DB 테이블에 영구 저장합니다.
- **D-24-06:** 기존 테스트 시나리오 검증용 메모리 정적 변수(`SshServerBackgroundService.LastAuthFailureReason` 등)도 함께 적절히 연동하여 기존 E2E/통합 테스트 스펙이 깨지지 않도록 하위 호환을 보존합니다.

### the agent's Discretion
- `Microsoft.DevTunnels.Ssh` 라이브러리 사용을 위한 구체적인 세션 채널 바인딩 및 비동기 스트림 중계 세부 구현.
- `SshAuthLog` 스키마 컬럼 및 필드 세부 네이밍 정의.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### SSH 서버 호환성 요건
- [REQUIREMENTS.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/REQUIREMENTS.md) §2 — SSH 서버 호환성 개선 요구사항 명세
- [PROJECT.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/PROJECT.md) — Active 요구사항 및 Decisions 이력 정보

</canonical_refs>

<specifics>
## Specific Ideas

- "다양한 Linux/MacOS/Windows 환경의 최신 OpenSSH 클라이언트에서 `ssh -T git@localhost` 진입 테스트가 예외 없이 안정적으로 통과해야 합니다."
- "DB 감사 로깅을 추가하지만, SSH 수송 스트림 속도나 성능 저하를 방지하기 위해 로깅 DB 저장 처리는 비동기 백그라운드로 처리하거나 세션 생명주기와 비차단형(Non-blocking)으로 설계합니다."

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- **SshKeyParser.cs**: DB 등록 사용자 공개키 포맷 해석 유틸. 새로운 ed25519 키 등이 들어올 때 정합성을 비교하는 토대가 됩니다.
- **SshSignatureVerificationService.cs**: 서명 서명값 파싱 및 검증 모듈.

### Established Patterns
- **SshServerBackgroundService.cs**: 백그라운드로 SSH를 바인딩하여 띄우는 호스팅 구조. FxSsh 의존성을 제거하고 `Microsoft.DevTunnels.Ssh` 구동 구조로 재작성해야 합니다.
- **SshCommandBridge.cs**: 클라이언트의 git-upload-pack / git-receive-pack 입출력 패킷 스트림을 로컬 디렉토리 프로세스와 중계해 주는 파이핑 구조.

### Integration Points
- `SshServerBackgroundService.cs` 내부의 Connection/Session 핸들러에서 `SshAuthLog` DB 로깅 처리가 일어나도록 `IServiceScopeFactory`를 통해 Scoped DB Context를 획득하여 기록합니다.

</code_context>

<deferred>
## Deferred Ideas

- None — 모든 논의가 현대적 SSH 개선 및 감사 로그 스키마 범위에 부합합니다.

</deferred>

---

*Phase: 24-ssh*
*Context gathered: 2026-06-11*
