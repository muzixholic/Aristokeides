using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Xunit;

namespace Aristokeides.Tests;

[Collection("SshTests")]
public class SshAuthLogTests
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

    private async Task<(int ExitCode, string Output)> RunSshCommand(int port, byte[] privateKeyBytes, string username, string command)
    {
        string keyFile = Path.GetTempFileName();
        File.WriteAllBytes(keyFile, privateKeyBytes);
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ssh",
                Arguments = $"-p {port} -i \"{keyFile}\" -o StrictHostKeyChecking=no -o PasswordAuthentication=no -o PubkeyAuthentication=yes {username}@127.0.0.1 {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(psi);
            if (process == null) throw new Exception("Failed to start ssh process");
            
            await process.WaitForExitAsync();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            
            return (process.ExitCode, output + error);
        }
        finally
        {
            if (File.Exists(keyFile)) File.Delete(keyFile);
        }
    }

    [Fact]
    public async Task AuthSuccess_ShouldLogToDb()
    {
        int testPort = 2225;
        var (publicKey, privateKeyBytes) = GenerateEcdsaKeyPair();
        string fingerprint = SshFingerprintCalculator.CalculateSha256Fingerprint(publicKey);

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var dbContext = new AppDbContext(dbOptions))
        {
            var user = new User { Id = 1, Username = "testuser", Email = "t@t.com", PasswordHash = "h", Role = "User" };
            dbContext.Users.Add(user);
            var sshKey = new SshKey { Id = 1, UserId = 1, Label = "Key", PublicKey = publicKey, Fingerprint = fingerprint };
            dbContext.SshKeys.Add(sshKey);
            await dbContext.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddScoped(sp => new AppDbContext(dbOptions));
        services.AddTransient<SshCommandBridge>();
        services.AddSingleton<SshSignatureVerificationService>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { {"Ssh:Port", testPort.ToString()} }).Build();

        var logger = new NullLogger<SshServerBackgroundService>();
        var service = new SshServerBackgroundService(serviceProvider, configuration, logger);

        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(1000);

        try
        {
            // 인증에 통과하지만 쉘 커맨드가 빈칸이므로 welcome message가 돌아오고 정상 차단/인증성공 처리됨
            var (exitCode, output) = await RunSshCommand(testPort, privateKeyBytes, "git", "");

            using (var dbContext = new AppDbContext(dbOptions))
            {
                var logs = await dbContext.SshAuthLogs.ToListAsync();
                Assert.NotEmpty(logs);
                
                // 첫번째 None 인증 로그가 있을 수 있으므로 (Microsoft.DevTunnels.Ssh가 처음 None 인증 시도를 먼저 함)
                // "git"으로 인증된 로그를 검색
                var successLog = logs.FirstOrDefault(l => l.Username == "git" && l.IsSuccess);
                Assert.NotNull(successLog);
                Assert.Equal(fingerprint, successLog!.KeyFingerprint);
                Assert.Equal("ecdsa-sha2-nistp256", successLog.KeyType);
                Assert.True(successLog.IsSuccess);
            }
        }
        finally
        {
            cts.Cancel();
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task InvalidUsername_ShouldLogFailureToDb()
    {
        int testPort = 2226;
        var (publicKey, privateKeyBytes) = GenerateEcdsaKeyPair();
        string fingerprint = SshFingerprintCalculator.CalculateSha256Fingerprint(publicKey);

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using (var dbContext = new AppDbContext(dbOptions))
        {
            var user = new User { Id = 1, Username = "testuser", Email = "t@t.com", PasswordHash = "h", Role = "User" };
            dbContext.Users.Add(user);
            var sshKey = new SshKey { Id = 1, UserId = 1, Label = "Key", PublicKey = publicKey, Fingerprint = fingerprint };
            dbContext.SshKeys.Add(sshKey);
            await dbContext.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddScoped(sp => new AppDbContext(dbOptions));
        services.AddTransient<SshCommandBridge>();
        services.AddSingleton<SshSignatureVerificationService>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { {"Ssh:Port", testPort.ToString()} }).Build();

        var logger = new NullLogger<SshServerBackgroundService>();
        var service = new SshServerBackgroundService(serviceProvider, configuration, logger);

        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(1000);

        try
        {
            // "git"이 아닌 다른 유저네임으로 접속 시도
            var (exitCode, output) = await RunSshCommand(testPort, privateKeyBytes, "baduser", "");

            using (var dbContext = new AppDbContext(dbOptions))
            {
                var logs = await dbContext.SshAuthLogs.ToListAsync();
                
                var failureLog = logs.FirstOrDefault(l => l.Username == "baduser");
                Assert.NotNull(failureLog);
                Assert.False(failureLog!.IsSuccess);
                Assert.Contains("not 'git'", failureLog.FailureReason);
            }
        }
        finally
        {
            cts.Cancel();
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task KeyNotRegistered_ShouldLogFailureToDb()
    {
        int testPort = 2227;
        var (publicKey, privateKeyBytes) = GenerateEcdsaKeyPair();
        string fingerprint = SshFingerprintCalculator.CalculateSha256Fingerprint(publicKey);

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // DB에 아무 키도 등록하지 않음

        var services = new ServiceCollection();
        services.AddScoped(sp => new AppDbContext(dbOptions));
        services.AddTransient<SshCommandBridge>();
        services.AddSingleton<SshSignatureVerificationService>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { {"Ssh:Port", testPort.ToString()} }).Build();

        var logger = new NullLogger<SshServerBackgroundService>();
        var service = new SshServerBackgroundService(serviceProvider, configuration, logger);

        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(1000);

        try
        {
            // DB에 없는 키로 git 접속 시도
            var (exitCode, output) = await RunSshCommand(testPort, privateKeyBytes, "git", "");

            using (var dbContext = new AppDbContext(dbOptions))
            {
                var logs = await dbContext.SshAuthLogs.ToListAsync();
                
                var failureLog = logs.FirstOrDefault(l => l.Username == "git" && !l.IsSuccess && l.KeyFingerprint != null);
                Assert.NotNull(failureLog);
                Assert.Equal(fingerprint, failureLog!.KeyFingerprint);
                Assert.False(failureLog.IsSuccess);
                Assert.Contains("SSH Key not found", failureLog.FailureReason);
            }
        }
        finally
        {
            cts.Cancel();
            await service.StopAsync(CancellationToken.None);
        }
    }
}
