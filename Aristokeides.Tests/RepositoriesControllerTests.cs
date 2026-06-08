using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Aristokeides.Api.Controllers;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aristokeides.Tests;

public class RepositoriesControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly RepositoryCreationChannel _channel;
    private readonly RepositoriesController _controller;

    public RepositoriesControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"Aristokeides_Repo_Test_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
        _channel = new RepositoryCreationChannel();

        var httpContext = new DefaultHttpContext();
        _controller = new RepositoriesController(_db, _channel)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task Create_ReturnsAccepted_WhenValid()
    {
        // Arrange
        var user = new User { Email = "test@example.com", Username = "testuser", PasswordHash = "hash", Role = "Reader" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "Cookies");
        _controller.HttpContext.User = new ClaimsPrincipal(identity);

        var request = new CreateRepositoryRequest("test-repo", "Description");

        // Act
        var result = await _controller.Create(request);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(acceptedResult.Value);

        var repo = await _db.Repositories.FirstOrDefaultAsync(r => r.Name == "test-repo");
        Assert.NotNull(repo);
        Assert.Equal(user.Id, repo.OwnerId);
        Assert.Equal("Creating", repo.Status);

        // Verify default board columns
        var columns = await _db.BoardColumns.ToListAsync();
        Assert.Equal(3, columns.Count);
        Assert.Contains(columns, c => c.Name == "To Do");
        Assert.Contains(columns, c => c.Name == "In Progress");
        Assert.Contains(columns, c => c.Name == "Done");
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims, "Cookies");
        _controller.HttpContext.User = new ClaimsPrincipal(identity);

        var request = new CreateRepositoryRequest("", "Description");

        // Act
        var result = await _controller.Create(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var request = new CreateRepositoryRequest("test-repo", "Description");

        // Act
        var result = await _controller.Create(request);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }
}
