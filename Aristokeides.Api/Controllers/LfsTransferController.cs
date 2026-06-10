using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services;

namespace Aristokeides.Api.Controllers;

[ApiController]
[Route("api/lfs/{owner}/{repo}")]
public class LfsTransferController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly LfsService _lfs;

    public LfsTransferController(AppDbContext db, LfsService lfs)
    {
        _db = db;
        _lfs = lfs;
    }

    // --- Helpers ---
    private async Task<Repository?> GetRepoAsync(string owner, string repoName)
    {
        return await _db.Repositories
            .Include(r => r.Owner)
            .Include(r => r.Organization)
            .FirstOrDefaultAsync(r => 
                (r.Owner != null && r.Owner.Username == owner && r.Name == repoName) ||
                (r.Organization != null && r.Organization.Name == owner && r.Name == repoName));
    }

    private string? GetBearerToken()
    {
        string authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return authHeader.Substring("Bearer ".Length).Trim();
    }

    private static string CalculateSha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = System.IO.File.OpenRead(filePath);
        var hashBytes = sha256.ComputeHash(stream);
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private async Task<bool> CheckReadAccessAsync(Repository repo, int userId)
    {
        if (!repo.IsPrivate) return true;
        if (repo.OwnerId == userId) return true;

        if (repo.OrganizationId.HasValue)
        {
            var isOrgMember = await _db.OrganizationMembers.AnyAsync(om => 
                om.OrganizationId == repo.OrganizationId.Value && om.UserId == userId);
            if (isOrgMember) return true;
        }

        return await _db.RepositoryPermissions.AnyAsync(rp => 
            rp.RepositoryId == repo.Id && 
            rp.UserId == userId && 
            (rp.AccessLevel == "Read" || rp.AccessLevel == "Write" || rp.AccessLevel == "Admin"));
    }

    // --- Endpoints ---

    // 1. Download
    [HttpGet("download/{oid}")]
    public async Task<IActionResult> Download([FromRoute] string owner, [FromRoute] string repo, [FromRoute] string oid)
    {
        var repository = await GetRepoAsync(owner, repo);
        if (repository == null)
            return NotFound();

        var token = GetBearerToken();
        if (token != null)
        {
            var principal = _lfs.ValidateToken(token, repository.Id, oid, "download");
            if (principal == null)
                return Forbid();
        }
        else
        {
            // Web UI 브라우저 직접 요청 대응 (Cookie 인증 확인)
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();

            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                return Unauthorized();

            var hasAccess = await CheckReadAccessAsync(repository, userId);
            if (!hasAccess)
                return Forbid();
        }

        var filePath = _lfs.GetObjectPath(oid);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(fileStream, "application/octet-stream");
    }

    // 2. Upload
    [HttpPut("upload/{oid}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload([FromRoute] string owner, [FromRoute] string repo, [FromRoute] string oid)
    {
        // OID 해시 포맷 검증 (Directory Traversal 방지)
        if (string.IsNullOrEmpty(oid) || oid.Length != 64 || !System.Text.RegularExpressions.Regex.IsMatch(oid, "^[a-fA-F0-9]{64}$"))
        {
            return BadRequest(new { message = "Invalid OID format." });
        }

        var repository = await GetRepoAsync(owner, repo);
        if (repository == null)
            return NotFound();

        var token = GetBearerToken();
        if (token == null)
            return Unauthorized();

        var principal = _lfs.ValidateToken(token, repository.Id, oid, "upload");
        if (principal == null)
            return Forbid();

        // 임시 파일 업로드 진행
        var tempFilePath = _lfs.GetTempPath(oid);
        try
        {
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await Request.Body.CopyToAsync(fileStream);
            }

            // 파일 정보 검사
            var fileInfo = new FileInfo(tempFilePath);
            
            // 1. OID 무결성 체크 (SHA-256)
            var calculatedOid = CalculateSha256(tempFilePath);
            if (!calculatedOid.Equals(oid, StringComparison.OrdinalIgnoreCase))
            {
                if (System.IO.File.Exists(tempFilePath)) System.IO.File.Delete(tempFilePath);
                return BadRequest(new { message = "Corrupted upload: OID SHA-256 mismatch." });
            }

            // 2. 글로벌 스토리지 목적지 검사 및 저장
            var finalPath = _lfs.GetObjectPath(oid);
            var finalFolder = Path.GetDirectoryName(finalPath);
            if (finalFolder != null && !Directory.Exists(finalFolder))
            {
                Directory.CreateDirectory(finalFolder);
            }

            // Atomic Move
            if (System.IO.File.Exists(finalPath))
            {
                System.IO.File.Delete(finalPath);
            }
            System.IO.File.Move(tempFilePath, finalPath);

            return Ok();
        }
        catch (Exception ex)
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                try { System.IO.File.Delete(tempFilePath); } catch { }
            }
            return StatusCode(500, new { message = "Upload failed.", details = ex.Message });
        }
    }

    // 3. Verify
    [HttpPost("verify/{oid}")]
    public async Task<IActionResult> Verify([FromRoute] string owner, [FromRoute] string repo, [FromRoute] string oid, [FromBody] LfsVerifyRequest request)
    {
        var repository = await GetRepoAsync(owner, repo);
        if (repository == null)
            return NotFound();

        var token = GetBearerToken();
        if (token == null)
            return Unauthorized();

        var principal = _lfs.ValidateToken(token, repository.Id, oid, "verify");
        if (principal == null)
            return Forbid();

        if (request == null || request.Oid != oid)
        {
            return BadRequest(new { message = "OID mismatch in request body." });
        }

        // 실제로 로컬 스토리지에 존재하는지 교차 체크
        if (!_lfs.Exists(oid, request.Size))
        {
            return BadRequest(new { message = "File does not exist in LFS storage or size mismatch." });
        }

        // DB 메타데이터 등록
        var alreadyExists = await _db.LfsObjects.AnyAsync(o => o.RepositoryId == repository.Id && o.Oid == oid);
        if (!alreadyExists)
        {
            var lfsObj = new LfsObject
            {
                RepositoryId = repository.Id,
                Oid = oid,
                Size = request.Size
            };
            _db.LfsObjects.Add(lfsObj);
            await _db.SaveChangesAsync();
        }

        return Ok();
    }
}

public class LfsVerifyRequest
{
    public string Oid { get; set; } = "";
    public long Size { get; set; }
}
