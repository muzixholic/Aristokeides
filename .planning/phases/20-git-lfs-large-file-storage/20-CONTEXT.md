# Phase 20: Git LFS (Large File Storage) 지원 - Context

**Gathered:** 2026-06-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Git LFS API 규격(LFS Batch API, Locks API) 엔드포인트를 완전히 구현하고, 대용량 바이너리 파일을 보관하는 로컬 글로벌 스토리지 백엔드 연동 및 웹 UI에서의 LFS 포인터 감지/대체 렌더링/다운로드 처리를 구현합니다.

</domain>

<decisions>
## Implementation Decisions

### LFS 바이너리 저장 구조 및 경로
- **D-01:** 글로벌 LFS 저장소 활용 (`GitRepos/lfs/objects/xx/yy/{sha256}`). 동일한 대용량 파일이 여러 저장소에 중복 업로드될 때 디듀플리케이션(중복 제거)을 적용하여 스토리지 공간 효율성을 높입니다.

### LFS Batch API 인증 메커니즘
- **D-02:** Basic Auth를 통해 최초 인증을 거친 뒤, 실제 파일 전송(Actions)을 요청할 때는 시한성(만료 시간 있음) 임시 토큰(JWT 등)을 인증 헤더에 담아 전송하고 검증하여 보안성을 강화합니다.

### LFS Locks API 구현 범위
- **D-03:** 실제 데이터베이스 테이블 연동을 통한 완전 구현. `LfsLock` 엔티티 테이블을 정의하여 사용자가 잠근 파일의 상태를 기억하고 충돌 푸시를 엄격히 방지합니다.

### 웹 UI 상에서의 LFS 파일 처리 방식
- **D-04:** LFS 포인터를 자동 감지하여 저장소 파일 뷰어에서 텍스트(포인터 내용) 대신 실제 바이너리 파일로 대체 표시합니다. 이미지 파일이면 화면에 렌더링하고, 기타 파일은 다운로드 버튼을 제공합니다.

### the agent's Discretion
- 임시 토큰의 토큰 발급 사양 및 유효 시간 설계 (기본 1시간 이내 등 보안 규칙에 따라 설계)
- `LfsLock` 데이터베이스 스키마 세부 필드 정의 및 충돌 발생 시의 상세 에러 코드/메시지 처리

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Git LFS API Specifications
- [Git LFS API Specification](https://github.com/git-lfs/git-lfs/tree/main/docs/api) — LFS Batch, Locks, and Transfer API specification
- [PROJECT.md](file:///E:/Workspace/VisualC%23/Aristokeides/.planning/PROJECT.md) — 핵심 가치 및 마일스톤 요구사항 정의

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- [GitSmartHttpMiddleware.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs): Git HTTP 스마트 프로토콜 요청 처리 미들웨어입니다. 이 미들웨어를 참고하거나 확장하여 `/info/lfs/objects/batch` 및 `/locks` API 요청을 가로채고 처리할 수 있습니다.
- [GitBrowserService.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Services/GitBrowserService.cs): 저장소 브라우저에서 파일을 읽어오는 서비스입니다. LFS 포인터 여부를 식별하고 원본 바이너리를 매핑해주는 로직 추가 시 참고합니다.

### Established Patterns
- 미들웨어 레벨의 인증 및 오류 응답 포맷 (HttpContext를 통한 JSON 응답 처리)
- Entity Framework Core를 활용한 데이터베이스 엔티티 정의 및 마이그레이션 (`AppDbContext`)

### Integration Points
- `/info/lfs/objects/batch` (Batch API 엔드포인트) 및 `/locks` (Locks API 엔드포인트) 구현 및 라우팅 추가
- 웹 UI `RepoBlob.razor` 및 `RepoBrowser.razor` 파일 뷰어 연동

</code_context>

<specifics>
## Specific Ideas

- **LFS 포인터 감지**: 파일 크기가 매우 작고 내용의 첫 줄이 `version https://git-lfs.github.com/spec/v1`인 경우 LFS 포인터로 감지
- **디렉토리 분할 규칙**: OID(SHA-256 해시값)의 첫 2글자, 다음 2글자를 각각 하위 폴더명으로 사용하여 파일 시스템 오버헤드를 방지 (예: `GitRepos/lfs/objects/ab/cd/abcdef...`)

</specifics>

<deferred>
## Deferred Ideas

- 클라우드 오브젝트 스토리지(S3, Azure Blob 등) 백엔드 연동 — 이번 페이즈는 로컬 글로벌 스토리지에 집중하고, 멀티 클라우드 스토리지는 추후 확장 페이즈로 미룸

</deferred>

---

*Phase: 20-Git LFS (Large File Storage) 지원*
*Context gathered: 2026-06-10*
