# Phase 9 Wave 1 Summary: Advanced Review Backend

**Completed At:** 2026-06-04
**Author:** Antigravity (Advanced Agentic Coding Assistant)

## 🛠️ Completed Tasks

### Task 1: 데이터 모델 정의 및 데이터베이스 스키마 마이그레이션
- [PullRequestReviewComment.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Models/PullRequestReviewComment.cs) 엔터티에 `IsPending` 및 `IsOutdated` 필드를 추가했습니다.
- [PullRequestReview.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Models/PullRequestReview.cs) 데이터 모델 및 `PullRequestReviewState` 열거형(`Comment`, `Approved`, `ChangesRequested`, `Dismissed` 상태)을 신규 작성했습니다.
- [AppDbContext.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Data/AppDbContext.cs)에 `DbSet<PullRequestReview>`을 추가하고 `OnModelCreating`에서 적절한 관계 제약조건(Cascade 및 Restrict)을 명시했습니다.
- EF Core 마이그레이션 `AddAdvancedReviewWorkflow`를 성공적으로 생성하고 로컬 DB에 반영 완료했습니다.

### Task 2: PullRequestService 비즈니스 로직 및 병합 제어 구현
- `PullRequestService.cs` 내 인라인 댓글 추가 메소드에서 `isPending` 속성을 지원하도록 오버로드 및 처리를 보완했습니다.
- `SubmitReviewAsync` 메소드를 새로 작성하여 리뷰어가 임시 보관 중이던 `IsPending == true` 상태의 댓글들을 `IsPending = false`로 일괄 업데이트하고 리뷰 내역을 DB에 영속화하도록 구현했습니다.
- `MergePullRequestAsync`를 수정하여 PR 내에 해결되지 않은 토론(`IsResolved == false && IsPending == false`)이 1개라도 존재하는 경우 예외를 발생시켜 일반 사용자의 병합을 차단했습니다.
- 호출 사용자의 권한이 `Admin`이고 `forceMerge = true` 플래그가 제공된 경우에 한해서 예외적으로 병합 차단을 우회하도록 비즈니스 안전 정책(T-09-01 완화)을 구현했습니다.

### Task 3: Git Push 후처리 로직 구현 및 통합
- `PullRequestService.cs`에 `OnBranchPushedAsync`를 신규 구현하여 푸시 후 백그라운드 연산을 처리하게 했습니다.
  - Myers/Hunk 매핑 라인 보정 알고리즘을 사용해 변경된 라인 영역의 기존 인라인 댓글을 `IsOutdated = true`로 전환하거나, 라인 위치가 밀린 경우 줄 번호를 보정(Line Shift)하도록 구현했습니다.
  - 소스 브랜치에 새 커밋이 푸시될 때 기존 리뷰어들의 모든 승인(`Approved`) 상태 리뷰들을 `Dismissed` 상태로 일괄 변경하도록 로직을 작성했습니다.
- [GitSmartHttpMiddleware.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Middleware/GitSmartHttpMiddleware.cs) 및 [SshCommandBridge.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Api/Services/Ssh/SshCommandBridge.cs)의 Push 완료 감지 블록에 `OnBranchPushedAsync`가 백그라운드에서 비동기로 안전하게 기동되도록 통합을 마쳤습니다.

## 🧪 Verification Results

### Automated Tests
- [AdvancedReviewTests.cs](file:///E:/Workspace/VisualC%23/Aristokeides/Aristokeides.Tests/Services/AdvancedReviewTests.cs)를 작성하여 핵심 시나리오(일괄 리뷰 제출, 미해결 토론 병합 제한 및 관리자 우회, 실제 Git Repository 커밋 푸시 시뮬레이션을 활용한 라인 보정 및 승인 초기화)를 성공적으로 검증했습니다.
- `dotnet test --filter "FullyQualifiedName~AdvancedReviewTests"` 실행 결과 3개의 신규 테스트 케이스가 성공적으로 통과되었습니다.
