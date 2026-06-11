using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aristokeides.Api.Services.Webhook;


namespace Aristokeides.Api.Services;

public class IssueService
{
    private readonly AppDbContext _context;
    private readonly WebhookService _webhookService;

    public IssueService(AppDbContext context, WebhookService webhookService)
    {
        _context = context;
        _webhookService = webhookService;
    }

    public async Task<List<Issue>> GetIssuesAsync(Guid repositoryId)
    {
        return await _context.Issues
            .Include(i => i.Column)
            .Include(i => i.Creator)
            .Include(i => i.Assignee)
            .Where(i => i.RepositoryId == repositoryId)
            .OrderBy(i => i.LocalId)
            .ToListAsync();
    }

    public async Task<List<BoardColumn>> GetBoardColumnsAsync(Guid repositoryId)
    {
        return await _context.BoardColumns
            .Where(c => c.RepositoryId == repositoryId)
            .OrderBy(c => c.Order)
            .ToListAsync();
    }

    public async Task<Issue?> GetIssueAsync(Guid repositoryId, int localId)
    {
        return await _context.Issues
            .Include(i => i.Column)
            .Include(i => i.Creator)
            .Include(i => i.Assignee)
            .FirstOrDefaultAsync(i => i.RepositoryId == repositoryId && i.LocalId == localId);
    }

    public virtual async Task<Issue> CreateIssueAsync(Guid repositoryId, string title, string? description, int creatorId, int? assigneeId = null)
    {
        var todoColumn = await _context.BoardColumns
            .Where(c => c.RepositoryId == repositoryId)
            .OrderBy(c => c.Order)
            .FirstOrDefaultAsync();

        if (todoColumn == null)
            throw new InvalidOperationException("No board columns exist for this repository.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        
        var maxLocalId = await _context.Issues
            .Where(i => i.RepositoryId == repositoryId)
            .MaxAsync(i => (int?)i.LocalId) ?? 0;
            
        int nextLocalId = maxLocalId + 1;

        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            LocalId = nextLocalId,
            Title = title,
            Description = description,
            CreatorId = creatorId,
            AssigneeId = assigneeId,
            ColumnId = todoColumn.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Issues.Add(issue);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Webhook trigger
        try
        {
            var repo = await _context.Repositories.Include(r => r.Owner).Include(r => r.Organization).FirstOrDefaultAsync(r => r.Id == repositoryId);
            var senderUser = await _context.Users.FindAsync(creatorId);
            var payload = new
            {
                @event = "issue",
                repository = new
                {
                    id = repositoryId,
                    name = repo?.Name ?? "unknown",
                    owner = repo?.Owner?.Username ?? repo?.Organization?.Name ?? "unknown"
                },
                sender = new
                {
                    id = creatorId,
                    username = senderUser?.Username ?? "unknown"
                },
                data = new
                {
                    action = "opened",
                    issue = new
                    {
                        number = issue.LocalId,
                        title = issue.Title,
                        body = issue.Description,
                        html_url = $"/repositories/{repositoryId}/issues/{issue.LocalId}"
                    }
                }
            };
            await _webhookService.TriggerWebhookAsync(repositoryId, "issue", payload);
        }
        catch { }

        return issue;
    }

    public async Task UpdateIssueStatusAsync(Guid issueId, Guid newColumnId)
    {
        var issue = await _context.Issues.FindAsync(issueId);
        if (issue == null) throw new KeyNotFoundException("Issue not found.");

        issue.ColumnId = newColumnId;
        issue.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateIssueDetailsAsync(Guid issueId, string title, string? description, int? assigneeId)
    {
        var issue = await _context.Issues.FindAsync(issueId);
        if (issue == null) throw new KeyNotFoundException("Issue not found.");

        issue.Title = title;
        issue.Description = description;
        issue.AssigneeId = assigneeId;
        issue.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task CloseIssueAsync(Guid issueId, int? actorId = null)
    {
        var issue = await _context.Issues.Include(i => i.Repository).ThenInclude(r => r!.BoardColumns).FirstOrDefaultAsync(i => i.Id == issueId);
        if (issue == null) throw new KeyNotFoundException("Issue not found.");

        var doneColumn = issue.Repository?.BoardColumns.OrderByDescending(c => c.Order).FirstOrDefault() 
            ?? await _context.BoardColumns
            .Where(c => c.RepositoryId == issue.RepositoryId && c.Name.ToLower() == "done")
            .FirstOrDefaultAsync();

        if (doneColumn != null)
        {
            issue.ColumnId = doneColumn.Id;
            issue.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Webhook trigger
            try
            {
                int senderId = actorId ?? issue.CreatorId;
                var repo = await _context.Repositories.Include(r => r.Owner).Include(r => r.Organization).FirstOrDefaultAsync(r => r.Id == issue.RepositoryId);
                var senderUser = await _context.Users.FindAsync(senderId);
                var payload = new
                {
                    @event = "issue",
                    repository = new
                    {
                        id = issue.RepositoryId,
                        name = repo?.Name ?? "unknown",
                        owner = repo?.Owner?.Username ?? repo?.Organization?.Name ?? "unknown"
                    },
                    sender = new
                    {
                        id = senderId,
                        username = senderUser?.Username ?? "unknown"
                    },
                    data = new
                    {
                        action = "closed",
                        issue = new
                        {
                            number = issue.LocalId,
                            title = issue.Title,
                            body = issue.Description,
                            html_url = $"/repositories/{issue.RepositoryId}/issues/{issue.LocalId}"
                        }
                    }
                };
                await _webhookService.TriggerWebhookAsync(issue.RepositoryId, "issue", payload);
            }
            catch { }
        }
    }

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

        // Webhook trigger
        try
        {
            var issue = await _context.Issues.FindAsync(issueId);
            if (issue != null)
            {
                var repo = await _context.Repositories.Include(r => r.Owner).Include(r => r.Organization).FirstOrDefaultAsync(r => r.Id == issue.RepositoryId);
                var senderUser = await _context.Users.FindAsync(authorId);
                var payload = new
                {
                    @event = "issue",
                    repository = new
                    {
                        id = issue.RepositoryId,
                        name = repo?.Name ?? "unknown",
                        owner = repo?.Owner?.Username ?? repo?.Organization?.Name ?? "unknown"
                    },
                    sender = new
                    {
                        id = authorId,
                        username = senderUser?.Username ?? "unknown"
                    },
                    data = new
                    {
                        action = "commented",
                        issue = new
                        {
                            number = issue.LocalId,
                            title = issue.Title,
                            body = issue.Description,
                            html_url = $"/repositories/{issue.RepositoryId}/issues/{issue.LocalId}"
                        },
                        comment = new
                        {
                            body = comment.Content
                        }
                    }
                };
                await _webhookService.TriggerWebhookAsync(issue.RepositoryId, "issue", payload);
            }
        }
        catch { }

        return comment;
    }
}
