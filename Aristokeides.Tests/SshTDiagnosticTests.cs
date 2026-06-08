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

public class SshTDiagnosticTests
{
    private static (string publicKey, byte[] privateKeyBytes) GenerateEcdsaKeyPair()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        
        // 1. Private Key in EC PEM format for SshNet
        string privateKeyPem = ecdsa.ExportECPrivateKeyPem();
        byte[] privateKeyBytes = Encoding.UTF8.GetBytes(privateKeyPem);

        // 2. Public Key in OpenSSH format for DB
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
    public async Task SshT_WelcomeMessage_ShouldBeReturnedAndCloseSafely()
    {
        int testPort = 2224; // 테스트 포트 변경
        var (publicKey, privateKeyBytes) = GenerateEcdsaKeyPair();
        string fingerprint = SshFingerprintCalculator.CalculateSha256Fingerprint(publicKey);

        // 1. In-Memory AppDbContext 설정
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // DB에 테스트 사용자 및 SSH 키 추가
        using (var dbContext = new AppDbContext(dbOptions))
        {
            var user = new User
            {
                Id = 1,
                Username = "ssh_test_user",
                Email = "test@example.com",
                PasswordHash = "hashed",
                Role = "User"
            };
            dbContext.Users.Add(user);

            var sshKey = new SshKey
            {
                Id = 1,
                UserId = 1,
                Label = "Test RSA Key",
                PublicKey = publicKey,
                Fingerprint = fingerprint
            };
            dbContext.SshKeys.Add(sshKey);
            await dbContext.SaveChangesAsync();
        }

        // 2. DI ServiceProvider 생성 및 AppDbContext 등록
        var services = new ServiceCollection();
        services.AddScoped(sp => new AppDbContext(dbOptions));
        var serviceProvider = services.BuildServiceProvider();

        // AppDomain 미처리 예외 포획
        List<string> unhandledExceptions = new();
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            unhandledExceptions.Add($"[Unhandled] {args.ExceptionObject}");
        };

        // 3. IConfiguration 설정
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Ssh:Port", testPort.ToString()}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // 4. Background Service 기동
        string hostKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "host.key");
        if (File.Exists(hostKeyPath))
        {
            try { File.Delete(hostKeyPath); } catch {}
        }

        SshServerBackgroundService.LastException = null;
        SshServerBackgroundService.DebugLogs.Clear();
        var logger = new TestLogger<SshServerBackgroundService>();
        var service = new SshServerBackgroundService(serviceProvider, configuration, logger);

        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        // SSH 서버 구동 대기시간 확보
        await Task.Delay(500);

        if (SshServerBackgroundService.LastException != null)
        {
            throw SshServerBackgroundService.LastException;
        }

        try
        {
            // 5. Renci.SshNet을 사용한 SSH Client 시뮬레이션
            var connectionInfo = new ConnectionInfo("127.0.0.1", testPort, "git",
                new PrivateKeyAuthenticationMethod("git", new PrivateKeyFile(new MemoryStream(privateKeyBytes)))
            );

            if (SshServerBackgroundService.LastException != null)
            {
                throw SshServerBackgroundService.LastException;
            }

            using var client = new SshClient(connectionInfo);
            


            client.HostKeyReceived += (sender, e) =>
            {
                e.CanTrust = true;
            };
            try
            {
                client.Connect();
            }
            catch (Exception ex)
            {
                var serverLogsStr = string.Join("\n  ", SshServerBackgroundService.DebugLogs);
                var unhandledStr = string.Join("\n  ", unhandledExceptions);
                var debugInfo = $"KeysExchangedCount={SshServerBackgroundService.KeysExchangedCount}, ServiceRegisteredCount={SshServerBackgroundService.ServiceRegisteredCount}, LastAuthFailureReason={SshServerBackgroundService.LastAuthFailureReason}\nServer Logs:\n  {serverLogsStr}\nUnhandled Exceptions:\n  {unhandledStr}";
                throw new Exception($"SSH Connect Failed! {debugInfo}", ex);
            }

            // ssh -T 시뮬레이션을 위해 빈 명령 실행
            using var cmd = client.CreateCommand("");
            var asyncResult = cmd.BeginExecute();
            
            // 실행 완료 대기
            cmd.EndExecute(asyncResult);

            string resultText = cmd.Result;
            int? exitStatus = cmd.ExitStatus;

            // 6. 결과 검증
            Assert.Equal(0, exitStatus);
            Assert.Contains("Hi ssh_test_user! You've successfully authenticated, but Aristokeides does not provide shell access.", resultText);
        }
        finally
        {
            // Clean up
            cts.Cancel();
            await service.StopAsync(CancellationToken.None);
        }
    }
}

public class TestLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"[TestLog-{logLevel}] {formatter(state, exception)}");
        if (exception != null)
        {
            Console.WriteLine(exception.ToString());
        }
    }
}
