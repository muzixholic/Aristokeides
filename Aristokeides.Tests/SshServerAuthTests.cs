using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services.Ssh;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Renci.SshNet;
using Xunit;

namespace Aristokeides.Tests;

public class SshServerAuthTests
{
    private static (string publicKey, byte[] privateKeyBytes) GenerateEcdsaKeyPair()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        string privateKeyPem = ecdsa.ExportECPrivateKeyPem();
        byte[] privateKeyBytes = Encoding.UTF8.GetBytes(privateKeyPem);

        var parameters = ecdsa.ExportParameters(false);
        using var ms = new MemoryStream();
        
        string algo = "ecdsa-sha2-nistp256";
        WriteNextString(ms, algo);
        WriteNextString(ms, "nistp256");
        
        byte[] x = parameters.Q.X ?? Array.Empty<byte>();
        byte[] y = parameters.Q.Y ?? Array.Empty<byte>();
        byte[] qBytes = new byte[1 + x.Length + y.Length];
        qBytes[0] = 0x04;
        Array.Copy(x, 0, qBytes, 1, x.Length);
        Array.Copy(y, 0, qBytes, 1 + x.Length, y.Length);
        
        WriteNextBytes(ms, qBytes);

        string base64 = Convert.ToBase64String(ms.ToArray());
        string publicKeyStr = $"{algo} {base64} test-ecdsa-key";

        return (publicKeyStr, privateKeyBytes);
    }

    private static void WriteNextString(Stream stream, string val)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(val);
        WriteNextBytes(stream, bytes);
    }

    private static void WriteNextBytes(Stream stream, byte[] bytes)
    {
        byte[] lenBytes = BitConverter.GetBytes((uint)bytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
        stream.Write(lenBytes, 0, 4);
        stream.Write(bytes, 0, bytes.Length);
    }

    [Fact]
    public async Task GeneralShell_ShouldBeRejected()
    {
        int testPort = 2224;
        var (publicKey, privateKeyBytes) = GenerateEcdsaKeyPair();
        string fingerprint = SshFingerprintCalculator.CalculateSha256Fingerprint(publicKey);

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var dbContext = new AppDbContext(dbOptions))
        {
            var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", PasswordHash = "hashed", Role = "User" };
            dbContext.Users.Add(user);
            dbContext.SshKeys.Add(new SshKey { Id = 1, UserId = 1, Label = "Key", PublicKey = publicKey, Fingerprint = fingerprint });
            await dbContext.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddScoped(sp => new AppDbContext(dbOptions));
        services.AddTransient<SshCommandBridge>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { {"Ssh:Port", testPort.ToString()} })
            .Build();

        var service = new SshServerBackgroundService(serviceProvider, configuration, new TestLogger<SshServerBackgroundService>());
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(500);

        try
        {
            var connectionInfo = new ConnectionInfo("127.0.0.1", testPort, "git", new PrivateKeyAuthenticationMethod("git", new PrivateKeyFile(new MemoryStream(privateKeyBytes))));
            using var client = new SshClient(connectionInfo);
            client.HostKeyReceived += (sender, e) => e.CanTrust = true;
            client.Connect();

            using var cmd = client.CreateCommand("bash");
            cmd.Execute();
            
            Assert.Equal(1, cmd.ExitStatus);
            Assert.Contains("Interactive shell is not allowed", cmd.Result);
        }
        finally
        {
            cts.Cancel();
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task PathTraversal_ShouldBeRejected()
    {
        int testPort = 2225;
        var (publicKey, privateKeyBytes) = GenerateEcdsaKeyPair();
        string fingerprint = SshFingerprintCalculator.CalculateSha256Fingerprint(publicKey);

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var dbContext = new AppDbContext(dbOptions))
        {
            var user = new User { Id = 1, Username = "testuser", Email = "test@example.com", PasswordHash = "hashed", Role = "User" };
            dbContext.Users.Add(user);
            dbContext.SshKeys.Add(new SshKey { Id = 1, UserId = 1, Label = "Key", PublicKey = publicKey, Fingerprint = fingerprint });
            await dbContext.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddScoped(sp => new AppDbContext(dbOptions));
        services.AddTransient<SshCommandBridge>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { {"Ssh:Port", testPort.ToString()} })
            .Build();

        var service = new SshServerBackgroundService(serviceProvider, configuration, new TestLogger<SshServerBackgroundService>());
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(500);

        try
        {
            var connectionInfo = new ConnectionInfo("127.0.0.1", testPort, "git", new PrivateKeyAuthenticationMethod("git", new PrivateKeyFile(new MemoryStream(privateKeyBytes))));
            using var client = new SshClient(connectionInfo);
            client.HostKeyReceived += (sender, e) => e.CanTrust = true;
            client.Connect();

            using var cmd = client.CreateCommand("git-upload-pack '../secret/repo.git'");
            cmd.Execute();
            
            Assert.Equal(1, cmd.ExitStatus);
        }
        finally
        {
            cts.Cancel();
            await service.StopAsync(CancellationToken.None);
        }
    }
}

