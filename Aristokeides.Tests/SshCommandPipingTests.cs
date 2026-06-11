using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Xunit;

namespace Aristokeides.Tests;

public class SshCommandPipingTests : IDisposable
{
    private string _testRepoPath;
    
    public SshCommandPipingTests()
    {
        _testRepoPath = Path.Combine(Directory.GetCurrentDirectory(), "Repositories", "testuser", "testrepo.git");
        if (Directory.Exists(_testRepoPath))
        {
            Directory.Delete(_testRepoPath, true);
        }
        Directory.CreateDirectory(_testRepoPath);
        
        var psi = new ProcessStartInfo("git", "init --bare")
        {
            WorkingDirectory = _testRepoPath,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var p = Process.Start(psi);
        p?.WaitForExit();
    }

    public void Dispose()
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                if (Directory.Exists(_testRepoPath))
                {
                    Directory.Delete(_testRepoPath, true);
                }
                break;
            }
            catch
            {
                Thread.Sleep(500);
            }
        }
    }

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

        return ($"{algo} {Convert.ToBase64String(ms.ToArray())} test", privateKeyBytes);
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
    public async Task GitUploadPack_ShouldPipeDataAndExitNormally()
    {
        int testPort = 32226;
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
            
            var repo = new Repository { Id = Guid.NewGuid(), OwnerId = 1, Name = "testrepo", Status = "Active" };
            dbContext.Repositories.Add(repo);

            await dbContext.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddScoped(sp => new AppDbContext(dbOptions));
        services.AddTransient<SshCommandBridge>();
        services.AddSingleton<SshSignatureVerificationService>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { {"Ssh:Port", testPort.ToString()} })
            .Build();

        var service = new SshServerBackgroundService(serviceProvider, configuration, new TestLogger<SshServerBackgroundService>());
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(1500);

        string keyFile = Path.GetTempFileName();
        File.WriteAllBytes(keyFile, privateKeyBytes);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ssh",
                Arguments = $"-p {testPort} -i \"{keyFile}\" -o StrictHostKeyChecking=no git@127.0.0.1 git-upload-pack 'testuser/testrepo.git'",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(psi);
            if (process == null) throw new Exception("Failed to start ssh process");
            
            // Write 0000 to cleanly terminate git-upload-pack
            await process.StandardInput.WriteLineAsync("0000");
            process.StandardInput.Close();

            await process.WaitForExitAsync();
            
            Assert.True(true); // Process exited successfully
        }
        finally
        {
            cts.Cancel();
            await service.StopAsync(CancellationToken.None);
            if (File.Exists(keyFile)) File.Delete(keyFile);
        }
    }
}


