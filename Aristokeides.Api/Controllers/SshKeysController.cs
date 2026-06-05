using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services.Ssh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aristokeides.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SshKeysController : ControllerBase
{
    private readonly AppDbContext _db;

    public SshKeysController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyKeys()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var keys = await _db.SshKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        return Ok(keys);
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterSshKeyRequest request)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.PublicKey))
        {
            return BadRequest(new { message = "Invalid key format. Only Ed25519, ECDSA, and RSA (3072 bits or higher) keys are supported." });
        }

        // 1. 공개키 파싱 및 형식/크기 유효성 검증
        string algorithm;
        int? keySize;
        string comment;
        try
        {
            (algorithm, keySize, comment) = SshKeyParser.ParseAndValidatePublicKey(request.PublicKey);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while adding the key. Please try again. ({ex.Message})" });
        }

        // 라벨이 없거나 비어있는 경우, 파싱된 코멘트 사용 혹은 기본 라벨 사용
        string label = string.IsNullOrWhiteSpace(request.Label) ? 
            (string.IsNullOrWhiteSpace(comment) ? "Unnamed SSH Key" : comment) : request.Label;

        // 2. SHA-256 지문 계산
        string fingerprint;
        try
        {
            fingerprint = SshFingerprintCalculator.CalculateSha256Fingerprint(request.PublicKey);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"지문 계산 실패: {ex.Message}" });
        }

        // 3. 전역 중복 체크
        var exists = await _db.SshKeys.AnyAsync(k => k.Fingerprint == fingerprint);
        if (exists)
        {
            return Conflict(new { message = "This SSH key is already in use by another user or associated with this account. Please use a unique key." });
        }

        // 4. DB 적재
        var newKey = new SshKey
        {
            UserId = userId,
            Label = label,
            PublicKey = request.PublicKey.Trim(),
            Fingerprint = fingerprint,
            CreatedAt = DateTime.UtcNow
        };

        _db.SshKeys.Add(newKey);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyKeys), new { id = newKey.Id }, newKey);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var key = await _db.SshKeys.FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);
        if (key == null)
        {
            return NotFound(new { message = "SSH key not found or you are not authorized to delete it." });
        }

        _db.SshKeys.Remove(key);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record RegisterSshKeyRequest(string Label, string PublicKey);
