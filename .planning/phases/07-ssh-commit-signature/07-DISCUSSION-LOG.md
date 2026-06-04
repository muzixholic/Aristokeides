# Phase 7: SSH Commit Signature - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-04
**Phase:** 07-SSH Commit Signature
**Areas discussed:** 서명 검증 시점, Verified 배지 UI 디자인, 서명 검증 데이터 저장, 서명-사용자 매핑 정책

---

## 서명 검증 시점

### Q1: 서명 검증을 언제 수행할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| Push 수신 시 즉시 검증 | git-receive-pack 후처리 훅에서 새 커밋의 서명을 즉시 검증하고 결과를 DB에 저장 | ✓ |
| 커밋 조회 시 온디맨드 검증 | UI에서 커밋 히스토리를 로드할 때 LibGit2Sharp로 서명을 읽고 DB의 SshKey와 대조 | |
| 에이전트에게 맡김 | 성능과 복잡도 균형을 맞춰서 에이전트가 최적의 접근을 판단 | |

**User's choice:** Push 수신 시 즉시 검증
**Notes:** Push 레이턴시 약간 증가하지만 조회 시 즉시 노출 가능

### Q2: 검증 결과를 DB에 저장하는 구조는 어떻게 할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| 전용 CommitSignature 테이블 | CommitHash, RepositoryId, SignerUserId, Status, Algorithm, VerifiedAt 등 전용 엔티티로 모델링 | ✓ |
| 기존 커밋 조회 모델에 필드 추가 | GitCommitInfo record에 IsVerified, SignerName 등 필드를 추가하고 DB 없이 메모리/캐시로 처리 | |

**User's choice:** 전용 CommitSignature 테이블

### Q3: Push 후처리를 어떤 방식으로 트리거할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| post-receive 훅 스타일 | SshCommandBridge에서 git-receive-pack 완료 후 새로 추가된 커밋들의 서명을 스캔 | |
| Smart HTTP 연동도 동시 처리 | SSH와 HTTP Push 모두에서 통합 검증 서비스 호출 | ✓ |

**User's choice:** 통합 검증 서비스 (SSH + HTTP 모두)

### Q4: 서명 없는 커밋이 Push될 때 서버의 대응 정책은?

| Option | Description | Selected |
|--------|-------------|----------|
| Push 거부 없이 검증만 수행 | 서명이 유효/무효/없음을 기록만 하고 Push는 항상 허용 | |
| 서명 없는 커밋 Push 차단 옵션 | 저장소별 설정으로 서명 필수 여부를 제어 가능 | ✓ |

**User's choice:** 저장소별 서명 필수 설정 옵션

---

## Verified 배지 UI 디자인

### Q1: Verified 배지의 시각적 스타일을 어떻게 할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| GitHub 스타일 | 커밋 해시 옆에 초록색 'Verified' 라벨 배지 (배경: 연두 초록, 텍스트: 진한 초록) | ✓ |
| GitLab 스타일 | 커밋 메시지 옆에 마름모꼴 아이콘 + 'Verified' 텍스트 | |
| 미니멀 | 커밋 해시 옆에 체크마크(✔) 아이콘만 표시 (툴팁으로 'Verified' 설명) | |

**User's choice:** GitHub 스타일 초록색 Verified 라벨 배지

### Q2: Verified 배지를 클릭했을 때 상세 정보를 보여줄까요?

| Option | Description | Selected |
|--------|-------------|----------|
| 클릭 시 팝오버/모달 표시 | 서명자 이름, 사용된 SSH 키 지문, 알고리즘, 검증 일시 등 상세 정보 | |
| 배지만 표시 | 단순 Verified/Unverified 라벨만 보여주고 상세 정보는 커밋 상세 페이지에서 확인 | ✓ |

**User's choice:** 배지만 표시 — 상세 정보는 커밋 상세 페이지에서

### Q3: 서명이 없는(미검증) 커밋은 어떻게 표시할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| 서명 없는 커밋은 아무 표시 없음 | 배지 자체가 없음 — Verified만 있는 커밋에 노출 | ✓ |
| 서명 없는 커밋에 회색 'Unverified' 라벨 표시 | 모든 커밋에 상태 표시 | |

**User's choice:** 서명 없는 커밋은 아무 표시 없음

### Q4: Verified 배지를 커밋 테이블의 어디에 배치할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| RepoCommits 테이블의 해시 컬럼 옆에 배지 추가 | 기존 4컬럼 유지, 해시 셀 내부에 함께 배치 | |
| 독립적인 '서명' 컬럼 추가 | 테이블에 5번째 컬럼으로 새 열 추가 | ✓ |

**User's choice:** 독립적인 '서명' 컬럼 추가 (5번째 컬럼)

---

## 서명 검증 데이터 저장

### Q1: CommitSignature 테이블의 기본 키/인덱스 전략은?

| Option | Description | Selected |
|--------|-------------|----------|
| CommitHash + RepositoryId 복합 유니크 키 | 커밋 SHA는 저장소 내에서 고유하므로 동일 커밋의 중복 레코드 방지 | ✓ |
| CommitHash 단독 유니크 키 | Git SHA는 글로벌로 고유하므로 RepositoryId 없이 CommitHash만으로 고유 식별 | |

**User's choice:** CommitHash + RepositoryId 복합 유니크 키

### Q2: CommitSignature의 Status 필드는 어떤 상태값들을 가질까요?

| Option | Description | Selected |
|--------|-------------|----------|
| Verified / Invalid / NoSignature 세 상태 | 검증 성공, 서명은 있지만 검증 실패, 서명 자체가 없음 | |
| Verified / Unverified 두 상태만 | 검증 성공 vs 그 외 모두 | |
| Verified / Invalid / Unknown / NoSignature 네 상태 | Unknown은 서명은 있지만 매칭되는 키가 DB에 없는 경우 | ✓ |

**User's choice:** 4단계 상태값

### Q3: 사용자가 SSH 키를 삭제했을 때 기존 커밋 서명 레코드는 어떻게 처리할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| SSH 키 삭제 시 CommitSignature 유지 | 이미 검증된 레코드는 그대로 보존 — Verified 상태 유지 | ✓ |
| SSH 키 삭제 시 Unknown으로 전환 | 해당 키로 검증된 커밋들의 상태를 Unknown으로 변경 | |

**User's choice:** 기존 CommitSignature 레코드 보존

---

## 서명-사용자 매핑 정책

### Q1: 커밋 Author 이메일과 SSH Key 소유자의 등록 이메일이 다를 때 어떻게 처리할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| 엄격 매칭 | Author 이메일이 서명 키 소유자의 등록 이메일과 일치해야만 Verified | |
| 유연 매칭 | 서명 키가 시스템의 등록된 SSH Key와 매칭되면 Author 이메일 무관하게 Verified (GitHub/GitLab 동일 방식) | ✓ |

**User's choice:** 유연 매칭 (GitHub/GitLab 동일)

### Q2: Push하는 사용자와 커밋 서명 키의 소유자가 다른 경우는 어떻게 처리할까요?

| Option | Description | Selected |
|--------|-------------|----------|
| Push 사용자와 서명 키 소유자가 같아야 Verified | A 사용자가 Push하지만 커밋은 B의 키로 서명된 경우 → Unknown | |
| Push 사용자와 서명 키 소유자 무관 | DB에 등록된 누구의 키든 상관없이 서명이 유효하면 Verified | ✓ |

**User's choice:** Push 사용자와 서명 키 소유자 무관

### Q3: 커밋 상세 페이지에서 서명자가 누구인지 보여줄까요?

| Option | Description | Selected |
|--------|-------------|----------|
| 보여준다 | Verified 배지에 서명자 이름을 함께 노출 (CommitSignature.SignerUserId로 사용자명 조회) | ✓ |
| 보여주지 않는다 | Verified 라벨만 표시, 서명자 정보는 숨김 | |

**User's choice:** 서명자 이름 노출

---

## Agent's Discretion

없음 — 모든 결정 사항을 사용자가 직접 선택함.

## Deferred Ideas

없음 — 논의가 Phase 범위 내에서 유지됨.
