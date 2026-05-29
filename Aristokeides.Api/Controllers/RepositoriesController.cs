using System.Security.Claims;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aristokeides.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Can be JWT or Basic depending on the request, but we have default scheme set to JWT.
public class RepositoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly RepositoryCreationChannel _channel;

    public RepositoriesController(AppDbContext db, RepositoryCreationChannel channel)
    {
        _db = db;
        _channel = channel;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRepositoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Repository name is required." });
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            OwnerId = userId,
            Status = "Creating",
            CreatedAt = DateTime.UtcNow
        };

        _db.Repositories.Add(repository);
        await _db.SaveChangesAsync();

        await _channel.EnqueueAsync(repository.Id);

        return Accepted(new { repository.Id, repository.Name, repository.Status });
    }
}

public record CreateRepositoryRequest(string Name, string? Description);
