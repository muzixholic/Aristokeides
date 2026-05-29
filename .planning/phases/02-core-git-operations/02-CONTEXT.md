# Phase 2: Core Git Operations - Context

**Gathered:** 2026-05-29
**Status:** Ready for planning

<domain>
## Phase Boundary

LibGit2Sharp를 이용한 Git Smart HTTP 및 저장소 생성 (REPO-01, REPO-02)
</domain>

<decisions>
## Implementation Decisions

### 저장소 디렉토리 구조
- **D-01:** 파일 시스템에 `{username}/{repo_name}.git` 형태로 저장합니다. (디버깅 및 식별 용이성을 위해)

### Git 클라이언트 인증 방식
- **D-02:** Git 클라이언트(터미널)에서 clone/push 시, 기존 로그인 이메일과 비밀번호를 Basic Auth로 그대로 사용합니다. (초기 MVP 개발 속도를 위해)

### 저장소 생성 처리 (오류 롤백/트랜잭션)
- **D-03:** 저장소 생성 요청 시, 먼저 DB에 '생성 중' 상태로 레코드를 기록한 뒤 비동기 백그라운드 작업으로 디렉토리를 생성합니다.

### the agent's Discretion
나머지 세부적인 C# / LibGit2Sharp 구현, Git HTTP Backend 세부 파싱 로직 등은 요원의 기술적 판단에 따릅니다.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

No external specs — requirements fully captured in decisions above.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `User` 모델 및 Auth 관련 비즈니스 로직 (Phase 1에서 구현됨)
- 인증 미들웨어 구조 (Git Smart HTTP 라우팅 시 연동)

### Established Patterns
- DB 연동은 EF Core 기반 (Phase 1)
- 인증/인가 처리는 API 중심 설계 유지

### Integration Points
- Git Smart HTTP (예: `/{username}/{repo.name}.git/info/refs`, `git-upload-pack`, `git-receive-pack`) 라우팅
</code_context>

<specifics>
## Specific Ideas

- Git 인증 시 Basic Auth 헤더를 파싱하여 기존 User 인증 로직을 재사용합니다.
</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope
</deferred>

---

*Phase: 2-Core Git Operations*
*Context gathered: 2026-05-29*
