---
phase: 05
plan: 02
objective: PR Details, Diff View, Comments, and Merging
wave: 2
depends_on: [01-PLAN.md]
requirements: [CODE-02, CODE-03]
files_modified:
  - Aristokeides.Api/Services/PullRequestService.cs
  - Aristokeides.Api/Components/Pages/RepoPullRequestDetail.razor
autonomous: true
---

# Phase 05 Plan 02: PR Details, Diff View, Comments, and Merging

## 1. Description
이 플랜은 Phase 05의 두 번째 부분으로, PR 상세 정보 화면을 완성합니다. `LibGit2Sharp`를 활용하여 소스와 타겟 브랜치 간의 충돌 상태를 확인하고, In-memory 머지 커밋을 통해 Bare Repo 상에서 PR을 병합합니다. 또한 Unified Diff 형식으로 패치를 렌더링하고 `highlight.js`를 이용한 신택스 하이라이팅을 제공하며, 이슈 코멘트(댓글) 기능을 추가합니다.

## 2. Tasks

```xml
<task id="1" type="execute">
  <action>Implement Conflict Checking and Diff Generation Logic</action>
  <read_first>
    <file>Aristokeides.Api/Services/PullRequestService.cs</file>
    <file>.planning/phases/05-prs-code-review/05-RESEARCH.md</file>
  </read_first>
  <description>
    Extend `PullRequestService`.
    1. Add `CheckConflictAsync(repositoryId, pullRequest)`: Use `ObjectDatabase.MergeCommits(targetCommit, sourceCommit, new MergeTreeOptions())` to check if `MergeTreeResult.Status == MergeTreeStatus.Conflicts`. Return a boolean.
    2. Add `GetPullRequestDiffAsync(repositoryId, pullRequest)`: Use `repo.Diff.Compare&lt;Patch&gt;(targetTree, sourceTree)` to extract the unified patch string for the UI.
  </description>
  <acceptance_criteria>
    - Code compiles without error.
    - Methods properly interact with the Bare Repository via `LibGit2Sharp`.
  </acceptance_criteria>
</task>

<task id="2" type="execute">
  <action>Implement Bare Repository Merge Logic</action>
  <read_first>
    <file>Aristokeides.Api/Services/PullRequestService.cs</file>
    <file>.planning/phases/05-prs-code-review/05-RESEARCH.md</file>
  </read_first>
  <description>
    Add `MergePullRequestAsync(repositoryId, pullRequest, userId)` to `PullRequestService`.
    1. Validate conflict status again. If conflict, throw.
    2. Create a merge commit using `repo.ObjectDatabase.CreateCommit(signature, signature, "Merge PR ...", mergeResult.Tree, new[] { targetCommit, sourceCommit }, false)`.
    3. Update the target branch ref: `repo.Refs.UpdateTarget(...)`.
    4. Update DB `IsMerged = true`, `MergeCommitSha = newCommit.Sha`. Close the associated `Issue` as well (set status to Closed).
  </description>
  <acceptance_criteria>
    - Bare repository merge creates a valid Git commit.
    - Database is updated correctly marking the PR and Issue as closed/merged.
  </acceptance_criteria>
</task>

<task id="3" type="execute">
  <action>Implement Comments API & Service</action>
  <read_first>
    <file>Aristokeides.Api/Services/IssueService.cs</file>
  </read_first>
  <description>
    Add comment capabilities.
    1. Add `AddCommentAsync(issueId, authorId, content)` to `IssueService`.
    2. Add `GetCommentsAsync(issueId)` to fetch `IssueComment` ordered by `CreatedAt`.
  </description>
  <acceptance_criteria>
    - Comments can be added and fetched successfully from the DB.
  </acceptance_criteria>
</task>

<task id="4" type="execute">
  <action>Implement PR Detail UI (Conversation & Diff View)</action>
  <read_first>
    <file>Aristokeides.Api/Components/Pages/RepoPullRequestDetail.razor</file>
    <file>.planning/phases/05-prs-code-review/05-UI-SPEC.md</file>
  </read_first>
  <description>
    Create `RepoPullRequestDetail.razor` (mapped to `/{username}/{repoName}/pulls/{localId}`).
    1. Display Conversation tab: Show PR Title, Status badge (Open, Merged, Conflict).
    2. List all comments and provide a text area to add new comments.
    3. Include a "Merge Pull Request" button. Disable it and show a warning ("Merge conflict detected. Please resolve conflicts locally before merging.") if `CheckConflictAsync` returns true.
    4. Display Files Changed tab: Call `GetPullRequestDiffAsync` and render the patch using `<pre><code class="language-diff">@patchContent</code></pre>`. (highlight.js will auto-process).
    5. Wire up the Merge button to `MergePullRequestAsync`.
  </description>
  <acceptance_criteria>
    - Page loads at `/{username}/{repoName}/pulls/{localId}`.
    - Conversation tab displays comments and allows submitting new ones.
    - Conflict warning displays if branches conflict.
    - Diff renders correctly in the Files Changed tab with `language-diff` class.
    - Clicking Merge successfully completes the merge if no conflicts.
  </acceptance_criteria>
</task>
```

## 3. Verification Criteria
- Diff viewer correctly renders added/removed lines via highlight.js.
- Comments added on a PR appear instantly (via Blazor interactivity or reload).
- Merging a PR updates the target branch in the underlying bare Git repository and updates DB status to Merged.
- Conflicts trigger a disabled Merge button and error state UI.

## 4. Must Haves
- **Truth 1**: Merging MUST use `ObjectDatabase.CreateCommit` and `Refs.UpdateTarget` since there is no working directory.
- **Truth 2**: highlight.js CDN must be utilized for Diff syntax highlighting.
- **Truth 3**: Merge conflict checking must occur in memory using `MergeTreeOptions`.
