# Phase 8: PR Inline Comments - 패턴 맵 (Pattern Map)

**매핑 일자:** 2026-06-04
**분석된 신규/수정 파일 수:** 7개
**아날로그 매칭 성공:** 7개 / 7개

이 문서는 Pull Request의 파일 변경 Diff 화면에서 코드 라인별로 인라인 댓글을 작성하고 스레드 형태로 관리하기 위해, 기존 코드베이스에서 재사용하거나 일관되게 복사해야 할 아키텍처적 패턴 및 구체적인 코드 발췌록을 정의합니다.

---

## 1. 파일 분류 (File Classification)

| 새 파일 / 수정 파일 | 역할 (Role) | 데이터 흐름 (Data Flow) | 가장 유사한 기존 아날로그 (Closest Analog) | 매칭 품질 (Match Quality) |
|:---|:---|:---|:---|:---|
| `Aristokeides.Api/Models/PullRequestReviewComment.cs` | model | CRUD | `Aristokeides.Api/Models/IssueComment.cs` | exact |
| `Aristokeides.Api/Services/DiffParser.cs` | service / utility | transform | `Aristokeides.Api/Services/Ssh/SshKeyParser.cs` | role-match |
| `Aristokeides.Api/Services/PullRequestService.cs` (수정) | service | CRUD / request-response | `Aristokeides.Api/Services/IssueService.cs` | role-match |
| `Aristokeides.Api/Components/Pages/RepoPullRequestDetail.razor` (수정) | component | request-response / CRUD | `RepoPullRequestDetail.razor` (기존) 및 `RepoIssueDetail.razor` | role-match |
| `Aristokeides.Tests/Services/InlineCommentTests.cs` | test | request-response | `Aristokeides.Tests/SshKeyParserTests.cs` | role-match |
| `Aristokeides.Tests/Data/InlineCommentDbTests.cs` | test | CRUD | `Aristokeides.Tests/SshKeyRegistrationTests.cs` | exact |
| `Aristokeides.Api/Data/AppDbContext.cs` (수정) | config / data access | CRUD | `Aristokeides.Api/Data/AppDbContext.cs` (기존) | exact |

---

## 2. 패턴 할당 및 발췌 코드 (Pattern Assignments)

### 2.1. `PullRequestReviewComment.cs` (model, CRUD)
* **아날로그:** `Aristokeides.Api/Models/IssueComment.cs`
* **설명:** 신규 댓글 엔터티는 기존 이슈 댓글 엔터티의 식별자 구조와 작성자 매핑 방식을 상속받되, 라인 번호와 Diff Hunk 및 self-referencing 대댓글을 위한 속성을 추가합니다.
* **참조 코드 발췌 (기존 IssueComment.cs: L6-19):**
```csharp
public class IssueComment
{
    public Guid Id { get; set; }
    
    public Guid IssueId { get; set; }
    public Issue? Issue { get; set; }
    
    public int AuthorId { get; set; }
    public User? Author { get; set; }
    
    public required string Content { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

### 2.2. `DiffParser.cs` (service / utility, transform)
* **아날로그:** `Aristokeides.Api/Services/Ssh/SshKeyParser.cs`
* **설명:** 문자열 데이터를 정적 헬퍼 함수를 통해 특정 문법 규칙(Unified Diff 형식)에 맞추어 검증 및 구조적 데이터 객체로 변환하고 잘못된 형식에 대해 명시적 예외를 던지는 헬퍼 유틸리티 패턴입니다.
* **참조 코드 발췌 (기존 SshKeyParser.cs: L10-33):**
```csharp
public static class SshKeyParser
{
    public static (string algorithm, int? keySize, string comment) ParseAndValidatePublicKey(string publicKeyContent)
    {
        if (string.IsNullOrWhiteSpace(publicKeyContent))
            throw new ArgumentException("유효하지 않은 SSH 공개키 포맷입니다.");

        // 스페이스로 잘라서 [알고리즘, base64 페이로드, 주석(선택)]으로 분리
        string[] parts = publicKeyContent.Trim().Split(' ', 3);
        if (parts.Length < 2)
            throw new ArgumentException("유효하지 않은 SSH 공개키 포맷입니다.");

        string algorithm = parts[0];
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            throw new ArgumentException("유효하지 않은 SSH 공개키 포맷입니다.");
        }
        
        string comment = parts.Length > 2 ? parts[2].Trim() : string.Empty;
```

---

### 2.3. `PullRequestService.cs` (service, CRUD / request-response)
* **아날로그:** `Aristokeides.Api/Services/IssueService.cs`
* **설명:** EF Core DBContext를 주입받아 비즈니스 처리를 수행하고 데이터를 영속화하며, 비동기 호출을 통해 예외 발생 가능성을 전파하는 전형적인 C# 서비스 비즈니스 레이어 구조입니다.
* **참조 코드 발췌 (기존 IssueService.cs: L127-142):**
```csharp
public async Task<IssueComment> AddCommentAsync(Guid issueId, int authorId, string content)
{
    var comment = new IssueComment
    {
        Id = Guid.NewGuid(),
        IssueId = issueId,
        AuthorId = authorId,
        Content = content,
        CreatedAt = DateTime.UtcNow
    };

    _context.IssueComments.Add(comment);
    await _context.SaveChangesAsync();

    return comment;
}
```

---

### 2.4. `RepoPullRequestDetail.razor` (component, request-response / CRUD)
* **아날로그:** 본인 파일 자체 (`RepoPullRequestDetail.razor`)의 기존 구조 및 `RepoIssueDetail.razor`
* **설명:** Blazor Server Interactive 모드 상에서 사용자의 데이터 입력을 받아 서비스를 실행하고 비동기적으로 UI 상태를 새로고침 없이 즉각 리렌더링하는 컴포넌트 생명주기 및 폼 바인딩 패턴입니다.
* **참조 코드 발췌 (기존 RepoPullRequestDetail.razor: L88-105 & L286-310):**
```razor
<!-- Comments -->
@if (pullRequest.Issue?.Comments != null)
{
    @foreach (var comment in pullRequest.Issue.Comments.OrderBy(c => c.CreatedAt))
    {
        <div style="display: flex; gap: 16px; margin-bottom: 24px;">
            <div style="flex-shrink: 0; width: 40px; height: 40px; border-radius: 50%; background-color: #e5e7eb; display: flex; align-items: center; justify-content: center; font-weight: bold; color: #6b7280;">
                @(comment.Author?.Username?.FirstOrDefault().ToString().ToUpper() ?? "?")
            </div>
            <div style="flex-grow: 1; border: 1px solid #e5e7eb; border-radius: 6px; overflow: hidden;">
                <div style="background-color: #f9fafb; padding: 12px 16px; border-bottom: 1px solid #e5e7eb; font-size: 13px; color: #4b5563;">
                    <span style="font-weight: 600; color: #111827;">@comment.Author?.Username</span> commented on @comment.CreatedAt.ToString("MMM d, yyyy")
                </div>
                <div style="padding: 16px; font-size: 14px; color: #111827; white-space: pre-wrap;">@comment.Content</div>
            </div>
        </div>
    }
}
```
```csharp
private async Task AddComment()
{
    if (string.IsNullOrWhiteSpace(newComment) || currentUserId == 0 || pullRequest?.Issue == null)
        return;

    isSubmittingComment = true;

    try
    {
        await IssueService.AddCommentAsync(pullRequest.IssueId, currentUserId, newComment);
        newComment = "";
        
        // Refresh comments
        pullRequest = await PullRequestService.GetPullRequestAsync(repository!.Id, LocalId);
    }
    catch (Exception ex)
    {
        errorMessage = "Failed to add comment: " + ex.Message;
    }
    finally
    {
        isSubmittingComment = false;
    }
}
```

---

### 2.5. `InlineCommentTests.cs` (test, request-response)
* **아날로그:** `Aristokeides.Tests/SshKeyParserTests.cs`
* **설명:** xUnit 테스트 프레임워크를 기반으로 하며, 특정 비즈니스 로직(예: DiffParser)에 대해 기대되는 입력과 예외 시나리오를 각각의 `[Fact]` 또는 `[Theory]` 속성을 부여하여 단위 테스트하는 템플릿입니다.
* **참조 코드 발췌 (기존 SshKeyParserTests.cs: L75-88):**
```csharp
[Fact]
public void ParseAndValidatePublicKey_Rsa3072OrHigher_ShouldSucceed()
{
    // Arrange
    string rsa3072 = GenerateOpenSshRsaKey(3072, "rsa-3072-key");

    // Act
    var (algorithm, keySize, comment) = SshKeyParser.ParseAndValidatePublicKey(rsa3072);

    // Assert
    Assert.Equal("ssh-rsa", algorithm);
    Assert.Equal(3072, keySize);
    Assert.Equal("rsa-3072-key", comment);
}
```

---

### 2.6. `InlineCommentDbTests.cs` (test, CRUD)
* **아날로그:** `Aristokeides.Tests/SshKeyRegistrationTests.cs`
* **설명:** `UseInMemoryDatabase`를 사용하여 분리된 테스트용 DbContext 인스턴스를 동적으로 구성하고, 서비스 레이어와 연동하여 DB CRUD, 외래키 관계 및 연쇄 삭제(Cascade/Restrict) 동작을 모방 검증하는 통합 데이터베이스 테스트 구조입니다.
* **참조 코드 발췌 (기존 SshKeyRegistrationTests.cs: L19-26 및 L71-101):**
```csharp
private static AppDbContext CreateInMemoryDbContext()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    return new AppDbContext(options);
}
```
```csharp
[Fact]
public async Task Register_ValidSshKey_ShouldSucceed()
{
    // Arrange
    using var db = CreateInMemoryDbContext();
    var controller = new SshKeysController(db)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = CreateMockUser(1) }
        }
    };

    string rsa3072 = GenerateOpenSshRsaKey(3072);
    var request = new RegisterSshKeyRequest("My RSA Key", rsa3072);

    // Act
    var result = await controller.Register(request);

    // Assert
    var createdResult = Assert.IsType<CreatedAtActionResult>(result);
    var createdKey = Assert.IsType<SshKey>(createdResult.Value);
    Assert.Equal("My RSA Key", createdKey.Label);
    Assert.Equal(1, createdKey.UserId);
    Assert.StartsWith("SHA256:", createdKey.Fingerprint);

    // DB에 정상 저장 확인
    var dbKey = await db.SshKeys.FirstOrDefaultAsync(k => k.Id == createdKey.Id);
    Assert.NotNull(dbKey);
    Assert.Equal("My RSA Key", dbKey.Label);
}
```

---

### 2.7. `AppDbContext.cs` (config / data access, CRUD)
* **아날로그:** 본인 파일 자체 (`AppDbContext.cs`)
* **설명:** EF Core 상에 모델을 데이터베이스 테이블과 매핑하고, `OnModelCreating` 메소드 내에서 Fluent API를 통하여 복합 키 제약 조건 및 Cascade/Restrict 관계 삭제 정책을 설정하는 패턴입니다.
* **참조 코드 발췌 (기존 AppDbContext.cs: L94-100):**
```csharp
modelBuilder.Entity<IssueComment>(entity =>
{
    entity.HasOne(c => c.Author)
          .WithMany()
          .HasForeignKey(c => c.AuthorId)
          .OnDelete(DeleteBehavior.Restrict);
});
```

---

## 3. 공통 및 공유 패턴 (Shared Patterns)

### 3.1. Markdig 마크다운 HTML 안전 컴파일 패턴
* **사용 용도:** 모든 인라인 댓글 및 답글 내용 렌더링 시 XSS 공격을 방지하기 위한 보안 설정 패턴
* **적용 대상:** `RepoPullRequestDetail.razor` 및 관련 렌더링 헬퍼
```csharp
using Markdig;
using Microsoft.AspNetCore.Components;

public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml() // 보안을 위해 날것의 입력 HTML을 모두 차단 및 이스케이프
        .UseAdvancedExtensions()
        .Build();

    public static MarkupString RenderHtml(string markdownText)
    {
        if (string.IsNullOrWhiteSpace(markdownText))
            return new MarkupString(string.Empty);

        var html = Markdown.ToHtml(markdownText, Pipeline);
        return new MarkupString(html);
    }
}
```

### 3.2. 외래키 제약조건 삭제 정책 패턴 (DeleteBehavior)
* **사용 용도:** 회원 탈퇴 및 이슈/PR 삭제에 따른 관계 무결성 제어 정책 일관성 유지
* **적용 대상:** `AppDbContext.cs` (`PullRequestReviewComment` 매핑)
  - `PullRequest` 엔터티 삭제 시: `.OnDelete(DeleteBehavior.Cascade)` (인라인 댓글 연쇄 삭제)
  - `Author(User)` 엔터티 삭제 시: `.OnDelete(DeleteBehavior.Restrict)` (댓글이 존재할 경우 유저 탈퇴 차단)
  - `ParentId(자가참조)` 댓글 삭제 시: `.OnDelete(DeleteBehavior.Cascade)` (부모 댓글 삭제 시 하위 스레드/답글 모두 삭제)

---

## 4. 아날로그가 없는 패턴 (No Analog Found)

본 단계에서는 모든 구현 대상이 기존 코드베이스 내에 훌륭하게 아날로그를 지니고 있으므로 아날로그가 없는 컴포넌트는 존재하지 않습니다. 다만, **Unified Diff 형식의 파서 비즈니스 로직**의 경우 프로젝트 내에 동일한 행 단위 Diff 파싱 알고리즘이 없으므로 `RESEARCH.md`에 기술된 **Unified Diff Line Parser 추천 양식**을 참고하여 `DiffParser.cs`를 구현해야 합니다.

---

## 5. 메타데이터 (Metadata)

* **아날로그 검색 범위:** `Aristokeides.Api/**/*`, `Aristokeides.Tests/**/*`
* **스캔된 파일 수:** 57개
* **패턴 추출 일시:** 2026-06-04
