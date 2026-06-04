# Project Roadmap

## Previous Milestones
- [v1.0 Milestone Archive](milestones/v1.0-ROADMAP.md) - Initial Release (Auth, Git Smart HTTP, Repo Browser, Issues, PRs)

## Current Milestone (v1.1)

**4 phases** | **15 requirements mapped** | All v1.1 requirements covered ✓

| # | Phase | Goal | Requirements | Success Criteria |
|---|-------|------|--------------|------------------|
| 6 | SSH Key & Connectivity | 3/3 | Completed | 3 success criteria |
| 7 | SSH Commit Signature | 2/2 | Completed | 2 success criteria |
| 8 | PR Inline Comments | PR Diff 화면에서의 인라인 댓글 작성, 저장 및 대화 스레드화 | CODE-04, CODE-06, CODE-08 | 3 success criteria |
| 9 | Advanced Review Workflow | 리뷰 상태 추적, 일괄 제출, 미해결 코멘트 병합 차단 및 라인 보정(Line Shift) | CODE-05, CODE-07, CODE-09, CODE-10, CODE-11 | 4 success criteria |

### Phase Details

### Phase 6: SSH Key & Connectivity
- **Goal:** SSH 키 등록/관리 및 FxSsh 기반 임베디드 SSH 서버 기본 연동
- **Mode:** mvp
- **Requirements:** SSH-01, SSH-02, SSH-03, SSH-04, SSH-05, SSH-06
- **UI hint**: yes
- **Success criteria:**
  1. 사용자가 프로필 설정 화면에서 SSH 공개키(Ed25519, ECDSA, RSA-3072+ 알고리즘)를 등록 및 삭제할 수 있으며, 공개키의 SHA-256 지문(Fingerprint)이 웹 UI에 정상 표시된다.
  2. 로컬 터미널에서 `ssh -T git@domain` 명령을 통해 SSH 연결 검증을 테스트할 수 있으며, 성공 시 인증 사용자 이름이 담긴 진단 환영 메시지를 응답받는다.
  3. 사용자가 등록된 SSH 키를 기반으로 로컬 터미널에서 SSH 프로토콜(SSH Clone URL)을 통해 Git Clone, Push, Pull 작업을 안전하게 실행할 수 있다.

### Phase 7: SSH Commit Signature (Completed)
- **Goal:** SSH 키 기반 커밋 디지털 서명 서버 검증 및 Verified 배지 표시
- **Mode:** mvp
- **Requirements:** SSH-07
- **UI hint**: yes
- **Success criteria:**
  - [x] 1. 사용자가 로컬에서 자신의 SSH 키로 서명하여 푸시한 Git 커밋의 디지털 서명을 서버 측에서 성공적으로 검증한다.
  - [x] 2. 검증된 커밋에 대해 웹 UI 커밋 히스토리 및 상세 화면에 신뢰할 수 있는 커밋임을 뜻하는 "Verified" 배지를 시각적으로 표시한다.

### Phase 8: PR Inline Comments
- **Goal:** PR Diff 화면에서의 인라인 댓글 작성, 저장 및 대화 스레드화
- **Mode:** mvp
- **Requirements:** CODE-04, CODE-06, CODE-08
- **UI hint**: yes
- **Plans:** 2 plans
- **Success criteria:**
  1. Pull Request의 파일 변경 Diff 화면에서 특정 코드 행에 마우스를 올릴 때 `+` 버튼이 노출되며, 클릭 시 해당 줄 바로 아래에 마크다운 및 실시간 프리뷰가 지원되는 인라인 댓글 작성 창이 열린다.
  2. 작성된 인라인 댓글은 해당 코드 주변 컨텍스트(DiffHunk), 원본/대상 파일 경로 및 라인 번호와 함께 DB에 정확하게 연동 및 저장되며, 새로고침 없이 즉시 화면에 렌더링된다.
  3. 인라인 댓글에 스레드 형식으로 답글(Reply)을 달 수 있으며, 해결된 토론은 "Resolve conversation"을 통해 접기 처리하고 필요 시 다시 재개할 수 있다.

Plans:
- [ ] 08-01-PLAN.md — PR 인라인 댓글 단일 쓰기/조회 백엔드 핵심 비즈니스 로직 및 기본 UI 슬라이스
- [ ] 08-02-PLAN.md — 마크다운 프리뷰, 대댓글 스레드 및 대화 해결/재개 UI 통합 슬라이스

### Phase 9: Advanced Review Workflow
- **Goal:** 리뷰 상태 추적, 일괄 제출, 미해결 코멘트 병합 차단 및 라인 보정(Line Shift)
- **Mode:** mvp
- **Requirements:** CODE-05, CODE-07, CODE-09, CODE-10, CODE-11
- **UI hint**: yes
- **Success criteria:**
  1. 사용자는 단일 댓글 즉시 추가 또는 "리뷰 시작"을 통해 여러 댓글을 임시 보관(Pending)했다가 한 번에 일괄 제출(Submit review)할 수 있다.
  2. 새 커밋이 브랜치에 푸시되는 경우, 변경된 코드 영역의 댓글은 자동으로 "Outdated" 상태로 전환되어 접히고, 코드 추가/삭제로 위치가 밀린 댓글은 라인 번호가 자동으로 보정(Line Shift)된다.
  3. Pull Request에 해결되지 않은(Unresolved) 토론 스레드가 하나라도 존재할 경우, 대상 브랜치로의 병합(Merge)이 엄격하게 차단된다.
  4. 사용자는 PR 리뷰 시 `Comment`, `Approve`, `Request Changes`를 선택하여 제출할 수 있으며, 코드 수정 후 신규 커밋이 푸시되면 기존 승인(Approve) 상태가 자동으로 취소되고 재검토를 강제한다.
