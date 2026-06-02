---
phase: 06-ssh-key-connectivity
plan: 02
type: summary
wave: 2
status: completed
---

## Phase 6 Wave 2 실행 요약

### 1. 목표 달성
- **SSH Clone URL 토글 위젯 및 복사 기능 구현**: 저장소 메인 화면(`RepoBrowser.razor`)에서 [HTTP]/[SSH] 프로토콜 토글 위젯을 구현하여 URL 구성을 동적으로 전환하고, 클립보드 📋 복사(2초간 툴팁 노출) 기능을 성공적으로 적용하였습니다.
- **FxSsh 임베디드 SSH 서버 기동 및 ssh -T 진단 처리**: FxSsh 라이브러리를 연동하고 `IHostedService` 기반의 `SshServerBackgroundService`를 구현했습니다. FxSsh가 RSA-SHA2 형식을 제대로 처리하지 못하는 이슈를 회피하기 위해 `ECDsa (nistP256)` 기반 호스트 키로 전환하여 성공적으로 안정적인 연결을 확보했습니다. `ssh -T git@localhost` 등의 접속 시 웰컴 메시지를 남기고 깔끔히 종료되도록 하였습니다.

### 2. 해결된 주요 이슈
- **FxSsh 호스트 키 알고리즘 불일치 및 접속 종료 버그 회피**: 처음 `rsa-sha2-256`를 호스트 키 알고리즘으로 시도했을 때, 내부 서명 생성 단계의 호환성 문제로 클라이언트(`Renci.SshNet`)와의 연결이 튕기는(connection closed by application) 심각한 버그가 있었습니다.
  이를 해결하기 위해 `System.Security.Cryptography.ECDsa`를 활용한 `ecdsa-sha2-nistp256` 호스트 키를 생성하고 `PEM` 형식으로 변환하여 `FxSsh`에 주입함으로써, 테스트 검증과 실제 연결이 모두 끊김 없이 안정적으로 통과함을 확인했습니다.

### 3. 검증 결과
- `RepositoryUrlTests.cs` 유닛 테스트 통과 확인 (Clone URL 포맷팅).
- `SshTDiagnosticTests.cs` 통합 테스트 통과 확인 (`Renci.SshNet` 클라이언트를 통한 실제 ECDSA 호스트 키 교환 및 `ssh -T` 인증 메시지 수신 후 정상 종료 여부 완벽 통과).

### 4. 다음 단계
- 다음 Wave(06-03)를 통해 실제 `SshCommandBridge`를 구현하여, OS에 설치된 `git-upload-pack`, `git-receive-pack` 프로세스와 SSH 터미널 파이프라인을 비동기로 중계하는 핵심 동작을 구현합니다.
