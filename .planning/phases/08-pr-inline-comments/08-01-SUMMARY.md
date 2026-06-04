# Phase 8-01: PR 인라인 댓글 (Wave 1) 구현 요약

## 1. 개요 및 구현 목표
본 단계에서는 풀 리퀘스트(PR) 상세 화면에서 변경 사항(Diff)의 코드 라인 단위로 단일 인라인 댓글을 작성하고, 이를 데이터베이스에 영속화하며 새로고침 없이 즉각 리렌더링하는 수직 슬라이스(Vertical Slice) 구현 및 검증을 완료하였습니다.

## 2. 자동화 테스트 결과
`dotnet test --filter "FullyQualifiedName~InlineComment"` 실행을 통해 새로 작성한 모든 단위 및 데이터베이스 통합 테스트가 무결하게 통과하였습니다.
- **총 테스트 수:** 3개 (성공 3, 실패 0)
  1. `Parse_ValidUnifiedDiff_ParsesFilesAndLinesCorrectly`: `DiffParser`가 Unified Diff 텍스트의 파일, Hunk 헤더, 라인(Addition, Deletion, Context)을 정확히 구조화된 객체로 변환하는지 검증.
  2. `AddReviewCommentAsync_WithValidLine_SavesToDatabase`: 유효한 파일 및 라인에 댓글 추가 시 DB 저장 및 탐색 속성(Author 등)이 정확하게 로드되는지 검증.
  3. `AddReviewCommentAsync_WithInvalidLine_ThrowsArgumentException`: 허용되지 않은 파일이나 라인 번호로 댓글 등록 시도 시 예외 처리 검증 (STRIDE 보안 완화 조치).

## 3. 구현된 산출물 목록
### 신규 파일
- **`Aristokeides.Api/Models/PullRequestReviewComment.cs`**
  - 인라인 댓글의 메타데이터(Id, PR Id, 작성자, 파일 경로, 라인 번호, Hunk 컨텍스트 등)를 나타내는 EF Core 엔터티 모델입니다.
- **`Aristokeides.Api/Services/DiffParser.cs`**
  - LibGit2Sharp의 Unified Diff 문자열을 줄 단위로 안전하게 구문 분석하여 UI 및 비즈니스 로직에서 다룰 수 있도록 구조화된 `DiffFile`, `DiffHunk`, `DiffLine` 모델로 반환하는 헬퍼 유틸리티입니다.
- **`Aristokeides.Tests/Services/InlineCommentTests.cs`**
  - `DiffParser`의 줄 분석 비즈니스 로직을 검증하는 단위 테스트입니다.
- **`Aristokeides.Tests/Data/InlineCommentDbTests.cs`**
  - InMemory Database 및 Mock 설정을 활용하여 비동기 CRUD 연동 및 잘못된 유입 값 차단을 검증하는 통합 테스트입니다.

### 수정 파일
- **`Aristokeides.Api/Data/AppDbContext.cs`**
  - `PullRequestReviewComments` 테이블을 DbSet으로 등록하고, Fluent API 설정을 통해 삭제 전파(Cascade) 제약 조건을 구성하였습니다. (PR 삭제 시 댓글 연쇄 삭제, 댓글 부모 삭제 시 대댓글 연쇄 삭제, User 삭제 시 Restrict 적용)
- **`Aristokeides.Api/Services/PullRequestService.cs`**
  - `GetReviewCommentsAsync` 및 `AddReviewCommentAsync` 메서드를 추가하였고, 테스트 용이성을 위해 `GetPullRequestDiffAsync`를 `virtual` 메서드로 변경하였습니다.
- **`Aristokeides.Api/Components/Pages/RepoPullRequestDetail.razor`**
  - 단순 텍스트 Diff 출력을 테이블 구조로 개선하고, 마우스 호버 시 CSS 스타일을 통해 파란색 `+` 버튼을 노출하도록 프론트엔드를 리팩토링하였습니다.
  - 새로고침 없이 비동기 상태 갱신을 통해 저장된 댓글 목록이 실시간으로 렌더링되도록 연동하였습니다.

## 4. 보안 완화 조치 (STRIDE Threat Register)
- **T-08-02 (Tampering 완화):**
  - 클라이언트에서 임의로 악의적인 파일 경로(`FilePath`) 또는 조작된 줄 번호(`OldLineNumber`, `NewLineNumber`)를 전달하여 데이터의 무결성을 깨뜨리는 공격을 방지하기 위해, `PullRequestService.AddReviewCommentAsync` 비즈니스 로직 내에서 실제 Git 저장소의 PR Diff 목록과 교차 검증을 거칩니다. Diff 범위 내에 존재하지 않는 줄에 댓글 작성을 요청할 경우 `ArgumentException`이 발생하며 데이터베이스 저장이 사전에 차단됩니다.
