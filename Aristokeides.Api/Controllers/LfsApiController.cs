using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;

namespace Aristokeides.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Basic")]
[Route("{owner}/{repo}.git/info/lfs")]
public class LfsApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public LfsApiController(AppDbContext db)
    {
        _db = db;
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

    // --- Endpoints ---

    // 1. Batch API
    [HttpPost("objects/batch")]
    public async Task<IActionResult> Batch([FromRoute] string owner, [FromRoute] string repo, [FromBody] LfsBatchRequest request)
    {
        var requiredAccess = request.Operation?.ToLowerInvariant() == "upload" ? "Write" : "Read";
        var (validatedRepo, validatedUser, access) = await GetValidatedRepoAndUserAsync(owner, repo, requiredAccess);
        
        if (validatedRepo == null)
            return NotFound(new { message = "Repository not found or no permission." });
        if (access == null)
            return Forbid();

        // [Skeleton Response] (Actions will be integrated in Wave 2)
        var response = new LfsBatchResponse
        {
            Transfer = "basic",
            Objects = new List<LfsObjectResponse>()
        };

        foreach (var obj in request.Objects)
        {
            response.Objects.Add(new LfsObjectResponse
            {
                Oid = obj.Oid,
                Size = obj.Size
            });
        }

        Response.ContentType = "application/vnd.git-lfs+json";
        return Ok(response);
    }

    // 2. Lock 생성 API
    [HttpPost("locks")]
    public async Task<IActionResult> CreateLock([FromRoute] string owner, [FromRoute] string repo, [FromBody] LfsCreateLockRequest request)
    {
        var (validatedRepo, validatedUser, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Write");
        
        if (validatedRepo == null)
            return NotFound();
        if (access == null)
            return Forbid();

        // [Skeleton Response] (Locks business logic integrated in Wave 3)
        var dummyLock = new LfsLockInfo
        {
            Id = "dummy-lock-id",
            Path = request.Path,
            LockedAt = DateTime.UtcNow,
            Owner = new LfsLockOwner { Name = validatedUser!.Username }
        };

        Response.ContentType = "application/vnd.git-lfs+json";
        return StatusCode(201, new { @lock = dummyLock });
    }

    // 3. Lock 목록 조회 API
    [HttpGet("locks")]
    public async Task<IActionResult> GetLocks([FromRoute] string owner, [FromRoute] string repo, [FromQuery] string? path)
    {
        var (validatedRepo, _, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Read");
        
        if (validatedRepo == null)
            return NotFound();
        if (access == null)
            return Forbid();

        var response = new LfsLocksResponse
        {
            Locks = new List<LfsLockInfo>()
        };

        Response.ContentType = "application/vnd.git-lfs+json";
        return Ok(response);
    }

    // 4. Lock 검증 API
    [HttpPost("locks/verify")]
    public async Task<IActionResult> VerifyLocks([FromRoute] string owner, [FromRoute] string repo, [FromBody] LfsVerifyLocksRequest request)
    {
        var (validatedRepo, _, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Write");
        
        if (validatedRepo == null)
            return NotFound();
        if (access == null)
            return Forbid();

        var response = new LfsVerifyLocksResponse
        {
            Ours = new List<LfsLockInfo>(),
            Theirs = new List<LfsLockInfo>()
        };

        Response.ContentType = "application/vnd.git-lfs+json";
        return Ok(response);
    }

    // 5. Lock 해제 API
    [HttpPost("locks/{id}/unlock")]
    public async Task<IActionResult> Unlock([FromRoute] string owner, [FromRoute] string repo, [FromRoute] string id, [FromBody] LfsUnlockRequest request)
    {
        var (validatedRepo, validatedUser, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Write");
        
        if (validatedRepo == null)
            return NotFound();
        if (access == null)
            return Forbid();

        var dummyLock = new LfsLockInfo
        {
            Id = id,
            Path = "dummy/path",
            LockedAt = DateTime.UtcNow,
            Owner = new LfsLockOwner { Name = validatedUser!.Username }
        };

        Response.ContentType = "application/vnd.git-lfs+json";
        return Ok(new { @lock = dummyLock });
    }
}

// --- DTO Classes ---

public class LfsBatchRequest
{
    public string Operation { get; set; } = "";
    public List<string> Transfers { get; set; } = new();
    public LfsRef? Ref { get; set; }
    public List<LfsObjectRequest> Objects { get; set; } = new();
}

public class LfsRef
{
    public string Name { get; set; } = "";
}

public class LfsObjectRequest
{
    public string Oid { get; set; } = "";
    public long Size { get; set; }
}

public class LfsBatchResponse
{
    public string Transfer { get; set; } = "basic";
    public List<LfsObjectResponse> Objects { get; set; } = new();
}

public class LfsObjectResponse
{
    public string Oid { get; set; } = "";
    public long Size { get; set; }
    public bool? Authenticated { get; set; }
    public LfsActions? Actions { get; set; }
}

public class LfsActions
{
    public LfsActionInfo? Download { get; set; }
    public LfsActionInfo? Upload { get; set; }
    public LfsActionInfo? Verify { get; set; }
}

public class LfsActionInfo
{
    public string Href { get; set; } = "";
    public Dictionary<string, string>? Header { get; set; }
    public int ExpiresIn { get; set; } = 3600;
}

public class LfsCreateLockRequest
{
    public string Path { get; set; } = "";
}

public class LfsLockInfo
{
    public string Id { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTime LockedAt { get; set; }
    public LfsLockOwner Owner { get; set; } = new();
}

public class LfsLockOwner
{
    public string Name { get; set; } = "";
}

public class LfsLocksResponse
{
    public List<LfsLockInfo> Locks { get; set; } = new();
    public string? NextCursor { get; set; }
}

public class LfsVerifyLocksRequest
{
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 100;
}

public class LfsVerifyLocksResponse
{
    public List<LfsLockInfo> Ours { get; set; } = new();
    public List<LfsLockInfo> Theirs { get; set; } = new();
    public string? NextCursor { get; set; }
}

public class LfsUnlockRequest
{
    public bool Force { get; set; }
}
