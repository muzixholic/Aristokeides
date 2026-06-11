using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aristokeides.Tests;

public class PlaywrightHostHelper : IDisposable
{
    public const string Port = "5002";
    public string ServerAddress => $"http://localhost:{Port}";
    
    public readonly string DbName;
    private CancellationTokenSource? _cts;
    private Task? _hostTask;

    public bool IsInstalledOverride { get; set; } = true;

    public PlaywrightHostHelper()
    {
        // 격리 테스트를 위해 매 실행마다 고유한 파일명 지정
        DbName = $"e2e_test_{Guid.NewGuid():N}.db";
    }

    public async Task StartAsync()
    {
        // 1. 임시 격리 DB 리셋
        ResetDatabase();

        // 2. 환경 변수 주입 (Program.Main 기동 시 설정값 오버라이드)
        Environment.SetEnvironmentVariable("Database__Provider", "SQLite");
        Environment.SetEnvironmentVariable("Database__ConnectionString", $"Data Source={DbName}");
        Environment.SetEnvironmentVariable("IsInstalled", IsInstalledOverride ? "true" : "false");
        Environment.SetEnvironmentVariable("GitSettings__BasePath", Path.Combine(Directory.GetCurrentDirectory(), "GitRepos"));
        Environment.SetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME", "Aristokeides.Api");

        // 3. 백그라운드 태스크로 웹 서버 기동
        _cts = new CancellationTokenSource();
        _hostTask = Task.Run(() => 
        {
            // Program.Main을 직접 기동하여 Kestrel 포트 5002를 점유해 서빙하도록 유도.
            // 최상위 문(Top-level statement)으로 컴파일된 Program 클래스의 EntryPoint를 리플렉션으로 획득하여 호출합니다.
            var entryPoint = typeof(Program).Assembly.EntryPoint;
            if (entryPoint == null)
            {
                throw new InvalidOperationException("Could not find the entry point (Main) of Aristokeides.Api assembly.");
            }
            
            var parameters = entryPoint.GetParameters();
            object?[] argsArray;
            if (parameters.Length > 0 && parameters[0].ParameterType == typeof(string[]))
            {
                argsArray = new object?[] { new string[] { "--urls", ServerAddress } };
            }
            else
            {
                argsArray = Array.Empty<object>();
            }

            try
            {
                var result = entryPoint.Invoke(null, argsArray);
                if (result is Task task)
                {
                    task.GetAwaiter().GetResult();
                }
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                Console.WriteLine($"[E2E Host Error] TargetInvocationException: {ex.InnerException}");
                throw ex.InnerException ?? ex;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[E2E Host Error] Exception: {ex}");
                throw;
            }
        });

        // 4. Kestrel 포트 바인딩 안정화를 위해 대기
        await Task.Delay(3000);

        if (_hostTask.IsFaulted)
        {
            throw new InvalidOperationException("Web host failed to start. Exception: " + _hostTask.Exception?.Flatten().InnerException?.Message, _hostTask.Exception);
        }
        if (_hostTask.IsCompleted)
        {
            throw new InvalidOperationException("Web host stopped immediately after start.");
        }
    }

    private void ResetDatabase()
    {
        string dbPath = Path.Combine(Directory.GetCurrentDirectory(), DbName);
        if (File.Exists(dbPath))
        {
            try { File.Delete(dbPath); } catch { }
        }
    }

    public void Dispose()
    {
        // 환경 변수 초기화
        Environment.SetEnvironmentVariable("Database__Provider", null);
        Environment.SetEnvironmentVariable("Database__ConnectionString", null);
        Environment.SetEnvironmentVariable("IsInstalled", null);
        Environment.SetEnvironmentVariable("GitSettings__BasePath", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_APPLICATIONNAME", null);

        // Kestrel 프로세스 정지 유도
        if (Program.App != null)
        {
            try
            {
                Program.App.StopAsync().GetAwaiter().GetResult();
            }
            catch { }
            Program.App = null;
        }
        
        _cts?.Cancel();
        
        // 리소스 정리
        string dbPath = Path.Combine(Directory.GetCurrentDirectory(), DbName);
        if (File.Exists(dbPath))
        {
            try { File.Delete(dbPath); } catch { }
        }

        string repoPath = Path.Combine(Directory.GetCurrentDirectory(), "GitRepos");
        if (Directory.Exists(repoPath))
        {
            try { Directory.Delete(repoPath, true); } catch { }
        }
    }
}
