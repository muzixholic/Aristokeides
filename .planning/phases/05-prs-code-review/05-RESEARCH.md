# Phase 05 Research: PRs & Code Review

## 1. 개요 (Overview)
본 문서는 Phase 5 (PRs & Code Review) 구현을 위한 데이터 모델링, LibGit2Sharp를 활용한 PR 생성 및 상태 확인, Bare Repository 환경에서의 병합 처리 방식, 그리고 Blazor 기반의 리뷰 UI(Diff Viewer) 설계 방안을 정리합니다. 해당 구현 방식은 `.planning/phases/05-prs-code-review/05-CONTEXT.md`의 결정사항을 충실히 반영합니다.

## 2. 데이터 모델링 (Data Modeling)

**2.1. PR과 Issue의 통합 번호 체계 (Decision 1 반영)**
PR과 Issue가 동일한 `LocalId` 대역을 공유하는 "GitHub 모델"을 채택합니다.
`PullRequest`를 `Issue`의 확장 정보(1:1 관계)로 매핑합니다.

- **`PullRequest` 엔터티**
  ```csharp
  public class PullRequest
  {
      [Key]
      public Guid IssueId { get; set; } // FK to Issue
      public string SourceBranch { get; set; } = null!;
      public string TargetBranch { get; set; } = null!;
      public bool IsMerged { get; set; }
      public string? MergeCommitSha { get; set; }
      public Issue? Issue { get; set; }
  }
  ```
- **`Issue` 엔터티 업데이트**:
  `public PullRequest? PullRequest { get; set; }`
  `public ICollection<IssueComment> Comments { get; set; } = new List<IssueComment>();`
- **`IssueComment` 엔터티 (요구사항 CODE-03 지원)**
  ```csharp
  public class IssueComment
  {
      public Guid Id { get; set; }
      public Guid IssueId { get; set; }
      public Issue? Issue { get; set; }
      public int AuthorId { get; set; }
      public User? Author { get; set; }
      public string Content { get; set; } = null!;
      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  }
  ```
- **설계 이점**: `IssueService`의 기존 `LocalId` 생성 로직(MaxAsync + 트랜잭션)을 수정 없이 그대로 활용할 수 있습니다. `IssueComment`는 PR뿐만 아니라 일반 Issue 댓글 기능으로도 재사용 가능합니다.

## 3. PR 상태 확인 (Conflict Detection)

**3.1. 충돌 확인 로직 (Decision 4 반영)**
웹에서는 충돌을 해결하는 에디터를 제공하지 않고 경고만 표시합니다.
- LibGit2Sharp의 `ObjectDatabase.MergeCommits()`를 사용하여 메모리 상에서 가상 병합(Merge)을 시도합니다.
- 반환된 `MergeTreeResult.Status`가 `MergeTreeStatus.Conflicts`인 경우 "충돌 발생(Conflict)" 상태로 판단하여 화면에 경고(Merge 비활성화)를 노출합니다.

## 4. PR 병합 처리 전략 (Bare Repo Merge)

**4.1. 워킹 디렉토리 없는 병합 (Decision 2 반영)**
프로젝트의 Git 리포지토리는 워킹 트리(Working Tree)가 없는 Bare Repository입니다. LibGit2Sharp의 표준 `Repository.Merge()` 메서드는 워킹 디렉토리를 요구하므로 직접 사용할 수 없습니다.
- **해결 방안 (Server-side In-memory Merge)**:
  1. `repo.ObjectDatabase.MergeCommits(targetCommit, sourceCommit, new MergeTreeOptions())`를 호출하여 병합된 `Tree`를 획득합니다. (결과가 `Succeeded`일 경우에만 진행)
  2. `repo.ObjectDatabase.CreateCommit(signature, signature, "Merge PR ...", mergeResult.Tree, new[] { targetCommit, sourceCommit }, false)`를 호출해 방금 생성된 `Tree`를 기반으로 새로운 커밋(Merge Commit)을 생성합니다.
  3. `repo.Refs.UpdateTarget(repo.Branches[targetBranch].CanonicalName, newCommit.Id)`를 통해 타겟 브랜치 참조를 새로 생성된 머지 커밋으로 업데이트합니다.
  4. DB 상에 `IsMerged = true` 및 `MergeCommitSha = newCommit.Sha` 업데이트.

## 5. Diff 뷰어 (Unified View) 구현 방안

**5.1. 패치(Diff) 조회 및 렌더링 (Decision 3 반영)**
- `repo.Diff.Compare<Patch>(targetTree, sourceTree)`를 통해 두 브랜치 간의 코드 패치 내역을 조회합니다.
- `Patch` 객체 내의 각 `PatchEntryChanges`를 순회하며 `.Patch` 속성의 문자열 데이터를 가져옵니다.
- **UI 렌더링**: Split View(좌우 분할)를 제외하고, Unified View 방식을 채택합니다.
  ```html
  @foreach (var entry in patchEntries)
  {
      <div class="diff-file">
          <h4>@entry.Path</h4>
          <pre><code class="language-diff">@entry.Patch</code></pre>
      </div>
  }
  ```
  Phase 3에서 도입된 `highlight.js` CDN이 `<code class="language-diff">` 엘리먼트를 식별하여 인라인 신택스 하이라이팅을 자동으로 적용합니다.

## 6. Blazor UI 컴포넌트 구조 제안

1. **`RepoPullRequests.razor`**: PR 목록을 렌더링. `Issue` 테이블에서 `PullRequest` 네비게이션 프로퍼티가 null이 아닌 항목만 가져오거나 PR 전용 API 쿼리 사용.
2. **`RepoPullRequestForm.razor`**: Source/Target 브랜치를 선택하여 새 PR을 생성하는 화면.
3. **`RepoPullRequestDetail.razor`**: PR 상세 화면.
   - **Conversation 탭**: PR 본문, 댓글(`IssueComment`) 목록, 새 댓글 작성 폼, 그리고 Merge 버튼(충돌 여부에 따라 활성/비활성).
   - **Commits 탭**: Source와 Target 브랜치 간의 추가 커밋 히스토리 표시.
   - **Files Changed 탭**: 위에서 설계한 Unified Diff 기반의 코드 변경 내역 표시.
