using System;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Aristokeides.Api.Controllers;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aristokeides.Tests;

public class SshKeyRegistrationTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ClaimsPrincipal CreateMockUser(int userId, string username = "testuser")
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username)
        }, "TestAuth"));
    }

    private static string GenerateOpenSshRsaKey(int keySize)
    {
        using var rsa = RSA.Create(keySize);
        var parameters = rsa.ExportParameters(false);
        using var ms = new MemoryStream();
        
        byte[] algoBytes = Encoding.ASCII.GetBytes("ssh-rsa");
        byte[] lenBytes = BitConverter.GetBytes((uint)algoBytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
        ms.Write(lenBytes, 0, 4);
        ms.Write(algoBytes, 0, algoBytes.Length);

        byte[] exponent = parameters.Exponent ?? Array.Empty<byte>();
        lenBytes = BitConverter.GetBytes((uint)exponent.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
        ms.Write(lenBytes, 0, 4);
        ms.Write(exponent, 0, exponent.Length);
        
        byte[] modulus = parameters.Modulus ?? Array.Empty<byte>();
        if (modulus.Length > 0 && modulus[0] >= 0x80)
        {
            byte[] temp = new byte[modulus.Length + 1];
            Array.Copy(modulus, 0, temp, 1, modulus.Length);
            modulus = temp;
        }
        lenBytes = BitConverter.GetBytes((uint)modulus.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
        ms.Write(lenBytes, 0, 4);
        ms.Write(modulus, 0, modulus.Length);

        string base64 = Convert.ToBase64String(ms.ToArray());
        return $"ssh-rsa {base64} key-comment";
    }

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

    [Fact]
    public async Task Register_DuplicateSshKey_ShouldReturnConflict()
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
        
        // 첫 번째 등록
        var request1 = new RegisterSshKeyRequest("First Key", rsa3072);
        var result1 = await controller.Register(request1);
        Assert.IsType<CreatedAtActionResult>(result1);

        // 동일한 키로 두 번째 등록 시도
        var request2 = new RegisterSshKeyRequest("Second Key", rsa3072);

        // Act
        var result2 = await controller.Register(request2);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result2);
        
        // 반환된 에러 메시지 검증 (Copywriting Contract 매칭)
        dynamic? err = conflictResult.Value;
        Assert.NotNull(err);
        var message = (string)err!.GetType().GetProperty("message").GetValue(err, null);
        Assert.Equal("This SSH key is already in use by another user or associated with this account. Please use a unique key.", message);
    }

    [Fact]
    public async Task Register_WeakRsaKey_ShouldReturnBadRequest()
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

        // 2048비트 RSA 키 생성 (보안 수준 미달)
        string rsa2048 = GenerateOpenSshRsaKey(2048);
        var request = new RegisterSshKeyRequest("Weak RSA Key", rsa2048);

        // Act
        var result = await controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        
        // 반환된 에러 메시지 검증 (Copywriting Contract 매칭)
        dynamic? err = badRequestResult.Value;
        Assert.NotNull(err);
        var message = (string)err!.GetType().GetProperty("message").GetValue(err, null);
        Assert.Equal("Invalid key format. Only Ed25519, ECDSA, and RSA (3072 bits or higher) keys are supported.", message);
    }

    [Fact]
    public async Task Register_InvalidFormat_ShouldReturnBadRequest()
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

        var request = new RegisterSshKeyRequest("Invalid Key", "invalid-key-data");

        // Act
        var result = await controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        
        dynamic? err = badRequestResult.Value;
        Assert.NotNull(err);
        var message = (string)err!.GetType().GetProperty("message").GetValue(err, null);
        Assert.Equal("Invalid key format. Only Ed25519, ECDSA, and RSA (3072 bits or higher) keys are supported.", message);
    }
}
