using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services.Webhook;

namespace Aristokeides.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Basic")]
[Route("api/repos/{owner}/{repo}/webhooks")]
public class WebhookApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly WebhookService _webhookService;

    public WebhookApiController(AppDbContext db, WebhookService webhookService)
    {
        _db = db;
        _webhookService = webhookService;
    }

    // --- Helpers ---
    private async Task<(Repository? repo, User? user, string? accessLevel)> GetValidatedRepoAndUserAsync(string owner, string repoName, string requiredAccess)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
            return (null, null, null);

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return (null, null, null);

        var repo = await _db.Repositories
            .Include(r => r.Owner)
            .Include(r => r.Organization)
            .FirstOrDefaultAsync(r => 
                (r.Owner != null && r.Owner.Username == owner && r.Name == repoName) ||
                (r.Organization != null && r.Organization.Name == owner && r.Name == repoName));

        if (repo == null)
            return (null, null, null);

        string? maxAccess = null;
        if (repo.OwnerId == userId)
        {
            maxAccess = "Admin";
        }
        else if (repo.OrganizationId.HasValue)
        {
            var isOrgOwner = await _db.OrganizationMembers.AnyAsync(om => 
                om.OrganizationId == repo.OrganizationId.Value && 
                om.UserId == userId && 
                om.Role == "Owner");

            if (isOrgOwner)
            {
                maxAccess = "Admin";
            }
            else
            {
                var teamIds = await _db.TeamMembers
                    .Where(tm => tm.UserId == userId && tm.Team.OrganizationId == repo.OrganizationId.Value)
                    .Select(tm => tm.TeamId)
                    .ToListAsync();

                var permissions = await _db.RepositoryPermissions
                    .Where(rp => rp.RepositoryId == repo.Id && 
                                 (rp.UserId == userId || (rp.TeamId != null && teamIds.Contains(rp.TeamId.Value))))
                    .Select(rp => rp.AccessLevel)
                    .ToListAsync();

                if (permissions.Any())
                {
                    if (permissions.Contains("Admin")) maxAccess = "Admin";
                    else if (permissions.Contains("Write")) maxAccess = "Write";
                    else if (permissions.Contains("Read")) maxAccess = "Read";
                }
            }
        }

        if (repo.IsPrivate && maxAccess == null)
        {
            return (null, null, null);
        }

        if (requiredAccess == "Write" && maxAccess != "Admin" && maxAccess != "Write")
        {
            return (repo, user, null);
        }

        return (repo, user, maxAccess ?? "Read");
    }

    private static WebhookDto MapToDto(Webhook webhook)
    {
        return new WebhookDto
        {
            Id = webhook.Id,
            RepositoryId = webhook.RepositoryId,
            Url = webhook.Url,
            Secret = string.IsNullOrEmpty(webhook.Secret) ? null : "********", // Secret 노출 차단 마스킹
            ContentType = webhook.ContentType,
            WebhookType = webhook.WebhookType,
            IsActive = webhook.IsActive,
            TriggerEvents = webhook.TriggerEvents,
            CreatedAt = webhook.CreatedAt
        };
    }

    // --- Endpoints ---

    // 1. 목록 조회
    [HttpGet]
    public async Task<IActionResult> GetWebhooks([FromRoute] string owner, [FromRoute] string repo)
    {
        var (validatedRepo, _, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Read");
        if (validatedRepo == null) return NotFound();
        if (access == null) return Forbid();

        var webhooks = await _db.Webhooks
            .Where(w => w.RepositoryId == validatedRepo.Id)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        return Ok(webhooks.Select(MapToDto));
    }

    // 2. 단건 조회
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWebhook([FromRoute] string owner, [FromRoute] string repo, [FromRoute] int id)
    {
        var (validatedRepo, _, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Read");
        if (validatedRepo == null) return NotFound();
        if (access == null) return Forbid();

        var webhook = await _db.Webhooks
            .FirstOrDefaultAsync(w => w.RepositoryId == validatedRepo.Id && w.Id == id);

        if (webhook == null) return NotFound();

        return Ok(MapToDto(webhook));
    }

    // 3. 웹훅 추가
    [HttpPost]
    public async Task<IActionResult> CreateWebhook([FromRoute] string owner, [FromRoute] string repo, [FromBody] WebhookCreateDto request)
    {
        var (validatedRepo, _, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Write");
        if (validatedRepo == null) return NotFound();
        if (access == null) return Forbid();

        // SSRF 방어 URL 체크
        if (_webhookService.IsPrivateOrLocalUrl(request.Url))
        {
            return BadRequest(new { message = "Invalid Target URL: Private or loopback IP range is blocked." });
        }

        var webhook = new Webhook
        {
            RepositoryId = validatedRepo.Id,
            Url = request.Url,
            Secret = string.IsNullOrEmpty(request.Secret) ? null : request.Secret,
            ContentType = request.ContentType ?? "application/json",
            WebhookType = request.WebhookType ?? "Generic",
            IsActive = request.IsActive,
            TriggerEvents = request.TriggerEvents ?? "push"
        };

        _db.Webhooks.Add(webhook);
        await _db.SaveChangesAsync();

        return StatusCode(201, MapToDto(webhook));
    }

    // 4. 웹훅 수정
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWebhook([FromRoute] string owner, [FromRoute] string repo, [FromRoute] int id, [FromBody] WebhookUpdateDto request)
    {
        var (validatedRepo, _, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Write");
        if (validatedRepo == null) return NotFound();
        if (access == null) return Forbid();

        var webhook = await _db.Webhooks
            .FirstOrDefaultAsync(w => w.RepositoryId == validatedRepo.Id && w.Id == id);

        if (webhook == null) return NotFound();

        // SSRF 방어 URL 체크
        if (_webhookService.IsPrivateOrLocalUrl(request.Url))
        {
            return BadRequest(new { message = "Invalid Target URL: Private or loopback IP range is blocked." });
        }

        webhook.Url = request.Url;
        webhook.ContentType = request.ContentType ?? "application/json";
        webhook.WebhookType = request.WebhookType ?? "Generic";
        webhook.IsActive = request.IsActive;
        webhook.TriggerEvents = request.TriggerEvents ?? "push";

        // 마스킹 문자열 "********" 이 들어온 경우 기존 Secret 보존
        if (!string.IsNullOrEmpty(request.Secret) && request.Secret != "********")
        {
            webhook.Secret = request.Secret;
        }
        else if (string.IsNullOrEmpty(request.Secret))
        {
            webhook.Secret = null;
        }

        await _db.SaveChangesAsync();
        return Ok(MapToDto(webhook));
    }

    // 5. 웹훅 삭제
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebhook([FromRoute] string owner, [FromRoute] string repo, [FromRoute] int id)
    {
        var (validatedRepo, _, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Write");
        if (validatedRepo == null) return NotFound();
        if (access == null) return Forbid();

        var webhook = await _db.Webhooks
            .FirstOrDefaultAsync(w => w.RepositoryId == validatedRepo.Id && w.Id == id);

        if (webhook == null) return NotFound();

        _db.Webhooks.Remove(webhook);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // 6. 전송 이력 목록 조회
    [HttpGet("{id}/deliveries")]
    public async Task<IActionResult> GetDeliveries([FromRoute] string owner, [FromRoute] string repo, [FromRoute] int id)
    {
        var (validatedRepo, _, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Read");
        if (validatedRepo == null) return NotFound();
        if (access == null) return Forbid();

        var webhookExists = await _db.Webhooks.AnyAsync(w => w.RepositoryId == validatedRepo.Id && w.Id == id);
        if (!webhookExists) return NotFound();

        var deliveries = await _db.WebhookDeliveries
            .Where(wd => wd.WebhookId == id)
            .OrderByDescending(wd => wd.DeliveredAt)
            .Take(50)
            .ToListAsync();

        return Ok(deliveries);
    }

    // 7. 전송 이력 단건 조회
    [HttpGet("{id}/deliveries/{deliveryId}")]
    public async Task<IActionResult> GetDelivery([FromRoute] string owner, [FromRoute] string repo, [FromRoute] int id, [FromRoute] int deliveryId)
    {
        var (validatedRepo, _, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Read");
        if (validatedRepo == null) return NotFound();
        if (access == null) return Forbid();

        var webhookExists = await _db.Webhooks.AnyAsync(w => w.RepositoryId == validatedRepo.Id && w.Id == id);
        if (!webhookExists) return NotFound();

        var delivery = await _db.WebhookDeliveries
            .FirstOrDefaultAsync(wd => wd.WebhookId == id && wd.Id == deliveryId);

        if (delivery == null) return NotFound();

        return Ok(delivery);
    }

    // 8. 수동 재전송 (Redeliver)
    [HttpPost("deliveries/{deliveryId}/redeliver")]
    public async Task<IActionResult> Redeliver([FromRoute] string owner, [FromRoute] string repo, [FromRoute] int deliveryId)
    {
        var (validatedRepo, _, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Write");
        if (validatedRepo == null) return NotFound();
        if (access == null) return Forbid();

        // 수동 재전송 위임
        var success = await _webhookService.RedeliverAsync(deliveryId);
        if (!success)
            return BadRequest(new { message = "Failed to redeliver webhook. Log not found or destination URL blocked." });

        return Ok(new { message = "Redelivery request queued successfully." });
    }
}

// --- DTOs ---

public class WebhookDto
{
    public int Id { get; set; }
    public Guid RepositoryId { get; set; }
    public string Url { get; set; } = "";
    public string? Secret { get; set; }
    public string ContentType { get; set; } = "application/json";
    public string WebhookType { get; set; } = "Generic";
    public bool IsActive { get; set; }
    public string TriggerEvents { get; set; } = "push";
    public DateTime CreatedAt { get; set; }
}

public class WebhookCreateDto
{
    public string Url { get; set; } = "";
    public string? Secret { get; set; }
    public string? ContentType { get; set; } // default: application/json
    public string? WebhookType { get; set; } // default: Generic
    public bool IsActive { get; set; } = true;
    public string? TriggerEvents { get; set; } // default: push
}

public class WebhookUpdateDto
{
    public string Url { get; set; } = "";
    public string? Secret { get; set; }
    public string? ContentType { get; set; }
    public string? WebhookType { get; set; }
    public bool IsActive { get; set; }
    public string? TriggerEvents { get; set; }
}
