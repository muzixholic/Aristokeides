using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services;

namespace Aristokeides.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Basic")]
[Route("{owner}/{repo}.git/info/lfs")]
public class LfsApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly LfsService _lfs;

    public LfsApiController(AppDbContext db, LfsService lfs)
    {
        _db = db;
        _lfs = lfs;
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

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var isUpload = request.Operation?.ToLowerInvariant() == "upload";

        var response = new LfsBatchResponse
        {
            Transfer = "basic",
            Objects = new List<LfsObjectResponse>()
        };

        foreach (var obj in request.Objects)
        {
            var exists = _lfs.Exists(obj.Oid, obj.Size);
            var objResponse = new LfsObjectResponse
            {
                Oid = obj.Oid,
                Size = obj.Size
            };

            if (isUpload)
            {
                if (exists)
                {
                    // 파일이 이미 있으면 Actions 생략
                    objResponse.Actions = null;
                }
                else
                {
                    // 업로드 및 검증 토큰 발급
                    var uploadToken = _lfs.GenerateToken(validatedRepo.Id, obj.Oid, "upload");
                    var verifyToken = _lfs.GenerateToken(validatedRepo.Id, obj.Oid, "verify");

                    objResponse.Actions = new LfsActions
                    {
                        Upload = new LfsActionInfo
                        {
                            Href = $"{baseUrl}/api/lfs/{owner}/{repo}/upload/{obj.Oid}",
                            Header = new Dictionary<string, string> { { "Authorization", $"Bearer {uploadToken}" } },
                            ExpiresIn = 3600
                        },
                        Verify = new LfsActionInfo
                        {
                            Href = $"{baseUrl}/api/lfs/{owner}/{repo}/verify/{obj.Oid}",
                            Header = new Dictionary<string, string> { { "Authorization", $"Bearer {verifyToken}" } },
                            ExpiresIn = 3600
                        }
                    };
                }
            }
            else // Download
            {
                if (exists)
                {
                    var downloadToken = _lfs.GenerateToken(validatedRepo.Id, obj.Oid, "download");
                    objResponse.Authenticated = true;
                    objResponse.Actions = new LfsActions
                    {
                        Download = new LfsActionInfo
                        {
                            Href = $"{baseUrl}/api/lfs/{owner}/{repo}/download/{obj.Oid}",
                            Header = new Dictionary<string, string> { { "Authorization", $"Bearer {downloadToken}" } },
                            ExpiresIn = 3600
                        }
                    };
                }
                else
                {
                    objResponse.Error = new LfsObjectError
                    {
                        Code = 404,
                        Message = "Object not found"
                    };
                }
            }

            response.Objects.Add(objResponse);
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

        var existingLock = await _db.LfsLocks
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.RepositoryId == validatedRepo.Id && l.Path == request.Path);

        if (existingLock != null)
        {
            var conflictLock = new LfsLockInfo
            {
                Id = existingLock.Id.ToString(),
                Path = existingLock.Path,
                LockedAt = existingLock.LockedAt,
                Owner = new LfsLockOwner { Name = existingLock.User.Username }
            };
            Response.ContentType = "application/vnd.git-lfs+json";
            return StatusCode(409, new { @lock = conflictLock, message = "already locked" });
        }

        var newLock = new LfsLock
        {
            RepositoryId = validatedRepo.Id,
            UserId = validatedUser!.Id,
            Path = request.Path,
            LockedAt = DateTime.UtcNow
        };

        _db.LfsLocks.Add(newLock);
        await _db.SaveChangesAsync();

        var lockInfo = new LfsLockInfo
        {
            Id = newLock.Id.ToString(),
            Path = newLock.Path,
            LockedAt = newLock.LockedAt,
            Owner = new LfsLockOwner { Name = validatedUser.Username }
        };

        Response.ContentType = "application/vnd.git-lfs+json";
        return StatusCode(201, new { @lock = lockInfo });
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

        var query = _db.LfsLocks
            .Include(l => l.User)
            .Where(l => l.RepositoryId == validatedRepo.Id);

        if (!string.IsNullOrEmpty(path))
        {
            query = query.Where(l => l.Path == path);
        }

        var locks = await query.ToListAsync();
        var response = new LfsLocksResponse
        {
            Locks = new List<LfsLockInfo>()
        };

        foreach (var l in locks)
        {
            response.Locks.Add(new LfsLockInfo
            {
                Id = l.Id.ToString(),
                Path = l.Path,
                LockedAt = l.LockedAt,
                Owner = new LfsLockOwner { Name = l.User.Username }
            });
        }

        Response.ContentType = "application/vnd.git-lfs+json";
        return Ok(response);
    }

    // 4. Lock 검증 API
    [HttpPost("locks/verify")]
    public async Task<IActionResult> VerifyLocks([FromRoute] string owner, [FromRoute] string repo, [FromBody] LfsVerifyLocksRequest request)
    {
        var (validatedRepo, validatedUser, access) = await GetValidatedRepoAndUserAsync(owner, repo, "Write");
        
        if (validatedRepo == null)
            return NotFound();
        if (access == null)
            return Forbid();

        var locks = await _db.LfsLocks
            .Include(l => l.User)
            .Where(l => l.RepositoryId == validatedRepo.Id)
            .ToListAsync();

        var response = new LfsVerifyLocksResponse
        {
            Ours = new List<LfsLockInfo>(),
            Theirs = new List<LfsLockInfo>()
        };

        foreach (var l in locks)
        {
            var lockInfo = new LfsLockInfo
            {
                Id = l.Id.ToString(),
                Path = l.Path,
                LockedAt = l.LockedAt,
                Owner = new LfsLockOwner { Name = l.User.Username }
            };

            if (l.UserId == validatedUser!.Id)
            {
                response.Ours.Add(lockInfo);
            }
            else
            {
                response.Theirs.Add(lockInfo);
            }
        }

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

        if (!int.TryParse(id, out var lockId))
            return BadRequest(new { message = "Invalid lock ID format." });

        var lfsLock = await _db.LfsLocks
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.RepositoryId == validatedRepo.Id && l.Id == lockId);

        if (lfsLock == null)
            return NotFound(new { message = "Lock not found." });

        bool isOwner = lfsLock.UserId == validatedUser!.Id;
        bool isAdmin = access == "Admin";

        if (!isOwner && !isAdmin)
        {
            return Forbid();
        }

        if (request.Force && !isAdmin)
        {
            return Forbid();
        }

        _db.LfsLocks.Remove(lfsLock);
        await _db.SaveChangesAsync();

        var lockInfo = new LfsLockInfo
        {
            Id = lfsLock.Id.ToString(),
            Path = lfsLock.Path,
            LockedAt = lfsLock.LockedAt,
            Owner = new LfsLockOwner { Name = lfsLock.User.Username }
        };

        Response.ContentType = "application/vnd.git-lfs+json";
        return Ok(new { @lock = lockInfo });
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
    public LfsObjectError? Error { get; set; }
}

public class LfsObjectError
{
    public int Code { get; set; }
    public string Message { get; set; } = "";
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
