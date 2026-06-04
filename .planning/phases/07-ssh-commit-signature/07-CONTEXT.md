# Phase 7: SSH Commit Signature - Context

**Gathered:** 2026-06-04
**Status:** Ready for planning

<domain>
## Phase Boundary

이 단계는 SSH 키 기반 Git 커밋 디지털 서명을 서버 측에서 검증하고, 그 결과를 DB에 기록하며, 웹 UI 커밋 히스토리 및 상세 화면에 "Verified" 배지를 시각적으로 표시하는 기능을 구축합니다. Push 수신 시점(SSH와 HTTP 모두)에서 통합적으로 서명을 검증하는 파이프라인을 제공하며, 저장소별로 서명 필수 정책을 설정할 수 있는 옵션을 포함합니다.

</domain>

<decisions>
## Implementation Decisions

### 1. 서명 검증 시점 및 트리거
- **D-01:** Push 수신 시 즉시 검증 — `git-receive-pack` 후처리 단계에서 새로 추가된 커밋의 SSH 서명을 즉시 검증하고 결과를 `CommitSignature` 테이블에 기록한다. 조회 시 별도 연산 없이 DB 조회만으로 Verified 상태를 즉시 노출한다.
- **D-02:** 통합 검증 서비스 — SSH Push(`SshCommandBridge`)와 HTTP Push(Smart HTTP) 모두에서 동일한 서명 검증 서비스를 호출한다. 프로토콜에 무관한 일관된 검증 보장.
- **D-03:** 저장소별 서명 필수 설정 — 서명 없는 커밋의 Push를 차단할 수 있는 저장소별 설정 옵션을 제공한다. 기본값은 비활성(서명 선택사항).

### 2. Verified 배지 UI 디자인
- **D-04:** GitHub 스타일 초록색 Verified 배지 — 커밋 해시 옆에 배경 연두 초록 / 텍스트 진한 초록의 'Verified' 라벨 배지를 표시한다.
- **D-05:** 배지는 단순 라벨만 표시 — 클릭 시 팝오버/모달 없이, 커밋 서명 상세 정보(서명자, 키 지문, 알고리즘)는 커밋 상세 페이지에서 확인한다.
- **D-06:** 서명 없는 커밋은 아무 표시 없음 — Verified 배지는 서명이 검증된 커밋에만 노출된다. Unverified 라벨은 없음.
- **D-07:** 독립적인 '서명' 컬럼 추가 — `RepoCommits.razor` 테이블에 메시지/작성자/날짜/해시에 이어 5번째 '서명' 컬럼을 추가한다.

### 3. 서명 검증 데이터 저장
- **D-08:** 전용 `CommitSignature` 테이블 — CommitHash, RepositoryId, SignerUserId, Status, Algorithm, VerifiedAt 등의 필드를 가진 전용 엔티티로 모델링한다.
- **D-09:** `CommitHash + RepositoryId` 복합 유니크 키 — 동일 커밋의 중복 검증 레코드를 방지한다.
- **D-10:** 4단계 Status 상태값 — `Verified`(검증 성공), `Invalid`(서명 존재하나 검증 실패), `Unknown`(서명 존재하나 매칭 키가 DB에 없음), `NoSignature`(서명 자체가 없음).
- **D-11:** SSH 키 삭제 시 기존 CommitSignature 레코드 보존 — 이미 검증된 기록은 Verified 상태를 유지한다.

### 4. 서명-사용자 매핑 정책
- **D-12:** 유연 매칭 방식 (GitHub/GitLab 동일) — 서명 키가 시스템에 등록된 SSH Key와 매칭되면 커밋 Author 이메일과 무관하게 Verified 처리한다.
- **D-13:** Push 사용자와 서명 키 소유자 무관 — DB에 등록된 누구의 키든 서명이 암호학적으로 유효하면 Verified 처리한다.
- **D-14:** 커밋 상세 페이지에 서명자 이름 노출 — `CommitSignature.SignerUserId`를 통해 서명자의 사용자명을 조회하여 커밋 상세 화면에 표시한다.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 요구사항
- `.planning/REQUIREMENTS.md` §SSH-07 — SSH 키 기반 커밋 서명 서버 검증 및 Verified 배지 요구사항

### 기존 SSH 구현 (Phase 6)
- `Aristokeides.Api/Models/SshKey.cs` — SSH 공개키 모델 (Id, UserId, Label, PublicKey, Fingerprint, CreatedAt)
- `Aristokeides.Api/Services/Ssh/SshServerBackgroundService.cs` — FxSsh 임베디드 SSH 서버 및 인증 로직 (OnUserAuth, OnCommandOpened)
- `Aristokeides.Api/Services/Ssh/SshKeyParser.cs` — SSH 공개키 파싱 유틸리티

### 커밋 히스토리 UI 및 서비스
- `Aristokeides.Api/Services/GitBrowserService.cs` — GetCommits() 메서드 (LibGit2Sharp 기반 커밋 조회)
- `Aristokeides.Api/Components/Pages/RepoCommits.razor` — 커밋 히스토리 테이블 UI (현재 4컬럼 → 5컬럼 확장 대상)

### 이전 Phase 결정 사항
- `.planning/phases/06-ssh-key-connectivity/06-CONTEXT.md` — SSH 포트(2222), Clone URL 형식, 키 라벨 자동 파싱 등 기 결정 사항

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **SshKey 모델**: `PublicKey` 필드에 원본 공개키 텍스트 보관 중 — 서명 검증 시 공개키 원본 활용 가능
- **SshKeyParser**: SSH 공개키 파싱 로직 — 서명 검증 시 키 타입/알고리즘 식별에 재활용 가능
- **AppDbContext**: 기존 `SshKeys` DbSet 설정 패턴을 따라 `CommitSignatures` DbSet 추가
- **GitCommitInfo record**: 커밋 조회 DTO — Verified 상태 필드 확장 필요

### Established Patterns
- **Blazor Server (SSR)**: 인라인 스타일 기반 UI, CSS 변수(`--accent`, `--secondary`, `--destructive`) 활용
- **EF Core Migration**: 마이그레이션 파일 네이밍 및 복합 인덱스 설정 패턴 (Phase 4 Issue 모델의 RepositoryId+LocalId 복합 유니크 키 참조)
- **DeleteBehavior**: Phase 4에서 `Restrict`/`SetNull` 정책 사용 사례 있음 — CommitSignature의 SignerUserId FK에도 적용 필요

### Integration Points
- **SshCommandBridge**: `RunGitCommandAsync()` 완료 후 서명 검증 서비스 호출 지점
- **Smart HTTP Controller**: HTTP Push 완료 후 서명 검증 서비스 호출 지점
- **RepoCommits.razor**: 5번째 '서명' 컬럼 추가 위치
- **Repository 모델**: `RequireSignedCommits` bool 속성 추가 위치 (저장소별 서명 필수 설정)

</code_context>

<specifics>
## Specific Ideas

- GitHub와 동일한 초록색 Verified 라벨 디자인 적용
- 서명자 정보는 커밋 히스토리가 아닌 커밋 상세 페이지에서만 노출 (리스트 뷰 간결성 유지)
- `NoSignature` 상태의 커밋도 DB에 레코드를 남겨서 "이 커밋은 검증 처리가 완료됨"을 추적

</specifics>

<deferred>
## Deferred Ideas

None — 논의가 Phase 범위 내에서 유지되었습니다.

</deferred>

---

*Phase: 7-SSH Commit Signature*
*Context gathered: 2026-06-04*
