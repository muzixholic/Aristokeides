# Phase 24-ssh Implementation Summary

## Progress
- **SSH 서버 라이브러리 교체:** 기존 `FxSsh 1.3.0`을 제거하고 `Microsoft.DevTunnels.Ssh`로 전면 교체하였습니다.
- **호스트 키 하이브리드 지원:** 기존 `host.key` (nistP256 ECDsa PEM) 호스트 키를 유지하면서, 신규 설치 시에는 `ED25519` 키가 자동 생성되도록 개선하였습니다.
- **DB 감사 로깅:** `SshAuthLog` 감사 테이블 및 엔티티를 추가하고 데이터베이스 마이그레이션(Sqlite 및 Postgres)을 완료하여 인증 시도(시각, IP, 지문, 유저네임, 성공 여부, 실패 사유, 키 유형)를 데이터베이스에 안전하게 영구 저장합니다.
- **스트림 릴레이 및 Command Bridge:** `SshCommandBridge`를 새 SSH 라이브러리의 비동기 스트림 API (`SshChannel`)를 활용하도록 재작성하여 안정적인 양방향 복사 패턴을 구축하고 stderr 분리 전송(extended data channel)을 가능하게 하였습니다.
- **테스트 및 검증:** `SshAuthLog` 저장 검증을 위한 신규 통합 테스트 `SshAuthLogTests.cs`를 추가하였고, 기존의 SSH 통합 테스트 3개 파일도 신규 서버 스펙에 정상 동작하도록 적응시켜 전체 104개 테스트가 모두 성공하였습니다.

## Tasks Completed
- [x] Task 24-01-01: `SshAuthLog` 엔티티 생성 및 데이터베이스 마이그레이션 적용
- [x] Task 24-01-02: `FxSsh` NuGet 패키지 제거 및 `Microsoft.DevTunnels.Ssh` 패키지 추가
- [x] Task 24-01-03: `SshServerBackgroundService.cs` 전면 재작성 및 하이브리드 키/로깅 통합
- [x] Task 24-01-04: `SshCommandBridge.cs` 라이브러리 전환 및 stderr 채널 개선
- [x] Task 24-01-05: 기존 SSH 통합 테스트 적응 및 전체 검증 완료
- [x] Task 24-01-06: `SshAuthLog` 감사 로깅 검증 테스트 추가 및 통과
