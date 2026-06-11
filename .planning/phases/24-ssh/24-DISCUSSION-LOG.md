# Phase 24: SSH 호환성 개선 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-11
**Phase:** 24-ssh
**Areas discussed:** SSH 서버 라이브러리 채택 방안, 호스트 키 알고리즘 및 파일 호환성 정책, SSH 로그인 인증 검증 시 실패 로깅 정책

---

## SSH 서버 라이브러리 채택 방안

| Option | Description | Selected |
|--------|-------------|----------|
| Microsoft.DevTunnels.Ssh 라이브러리 교체 | ed25519 및 rsa-sha2를 기본으로 지원하며 유지보수성이 높은 Microsoft 공식 라이브러리로 전면 포팅 | ✓ |
| 기존 FxSsh 라이브러리 유지 및 수동 암호화 패치 | FxSsh를 포크(Fork)하여 nistP256 외에 sha256 서명 검증 및 ed25519 디코드 파서를 C#으로 수동 구현 | |

**User's choice:** Microsoft.DevTunnels.Ssh 라이브러리 교체
**Notes:** 장기적 호환성 및 현대적 암호화 표준 지원 안정을 위해 DevTunnels.Ssh로 완전 교체하도록 결정함.

---

## 호스트 키(Host Key) 알고리즘 및 파일 호환성 정책

| Option | Description | Selected |
|--------|-------------|----------|
| 하이브리드 지원 및 신규 ED25519/RSA 자동 생성 | 기존 host.key (nistP256 ECDsa PEM) 호환 지원하되, 신규 설치 시에는 최신 ed25519/RSA 자동 생성 및 저장 | ✓ |
| 기존 키 호환 중단 및 ED25519/RSA 강제 신규 생성 | 기존 nistP256 PEM 키를 완전히 배제하고 ED25519 또는 RSA 포맷으로 무조건 새로 강제 생성 | |

**User's choice:** 하이브리드 지원 및 신규 ED25519/RSA 자동 생성
**Notes:** 기존 사용자의 host.key 파괴를 방지하기 위해 로드 방안은 유지하며, 신규 셋업 시에는 최신 표준 키를 생성하도록 하여 안전성과 편리성을 동시 확보함.

---

## SSH 로그인 인증 검증 시 실패 로깅 정책

| Option | Description | Selected |
|--------|-------------|----------|
| ILogger 및 메모리 정적 변수 로깅 유지 | 성능 저하를 방지하기 위해 로컬 로그 및 기존 테스트용 메모리 상태 값만 유지 | |
| DB SshAuthLog 테이블 신규 구축하여 영구 저장 | 보안 감사 강화를 위해 DB 마이그레이션 스키마를 설계하여 영구적인 시도 이력을 테이블에 기록 | ✓ |

**User's choice:** DB SshAuthLog 테이블 신규 구축하여 영구 저장
**Notes:** 침입 및 로그인 실패에 대한 보안 모니터링 강화를 위해 데이터베이스에 SSH 인증 로그 테이블(`SshAuthLog`)을 추가하는 방안을 선택함.

---

## the agent's Discretion

- Microsoft.DevTunnels.Ssh 세션 및 채널 스트림 중계 세부 포팅 구현
- SshAuthLog 엔티티 컬럼 정의 및 마이그레이션 세부 설계

## Deferred Ideas

- None — 모든 논의 항목이 Phase 24의 현대화 스펙 범위 내에 귀속됨.

---

*Phase: 24-ssh*
*Discussion log generated: 2026-06-11*
