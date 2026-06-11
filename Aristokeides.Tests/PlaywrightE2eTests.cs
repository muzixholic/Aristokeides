using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Aristokeides.Tests;

public class PlaywrightE2eTests : IDisposable
{
    private readonly PlaywrightHostHelper _helper;
    private readonly ITestOutputHelper _output;
    private string? _tempSetupDbName;

    public PlaywrightE2eTests(ITestOutputHelper output)
    {
        _helper = new PlaywrightHostHelper();
        _output = output;
    }

    [Fact]
    public async Task E2E_Test1_SetupWizard_ShouldInstallAndShutdownServer()
    {
        // --- 1. 최초 Setup 단계 E2E 검증 ---
        _helper.IsInstalledOverride = false;
        await _helper.StartAsync(); // 백그라운드 Kestrel 기동

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();

        // 1-1. Setup 페이지 접속
        await page.GotoAsync(_helper.ServerAddress + "/setup");
        
        // SignalR 회로 연결 및 인풋 부착 완료 대기
        await page.WaitForSelectorAsync("input.form-input");
        
        // 1-2. Setup 정보 기입 (로케이터 자동 재시도 기능을 통해 Blazor 가상 돔 리렌더링 이슈 극복)
        _tempSetupDbName = $"e2e_test_setup_{Guid.NewGuid():N}.db";
        await page.Locator("div.form-group:has(label:has-text('Database File Path')) input").FillAsync(_tempSetupDbName);
        await page.Locator("div.form-group:has(label:has-text('Username')) input").FillAsync("admin");
        await page.Locator("div.form-group:has(label:has-text('Email')) input").FillAsync("admin@example.com");
        
        await page.Locator("input[type='password']").Nth(0).FillAsync("Password1Inst!");
        await page.Locator("input[type='password']").Nth(1).FillAsync("Password1Inst!");

        // 1-3. 제출 (제출 직후 서버가 StopApplication을 시도하여 셧다운됨)
        await page.ClickAsync("button[type='submit']");
        
        // 셧다운 처리 동안 대기 후 DB 상태 점검
        await Task.Delay(2000);

        // DB에 어드민 생성이 올바르게 완료되었는지 검증
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_tempSetupDbName}")
            .Options;
        using (var db = new AppDbContext(options))
        {
            var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            Assert.NotNull(adminUser);
            Assert.Equal("admin", adminUser.Username);
        }
    }

    [Fact]
    public async Task E2E_Test2_MainWorkflow_ShouldPerformLoginAndRepoCreationAndIssueTracking()
    {
        // --- 2. 기설치 상태 기반 E2E 워크플로우 검증 ---
        _helper.IsInstalledOverride = true;
        await _helper.StartAsync(); // 백그라운드 Kestrel 기동

        // 2-1. E2E 검증용 데이터베이스에 사전 어드민 유저 시딩
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_helper.DbName}")
            .Options;
        using (var db = new AppDbContext(options))
        {
            if (!await db.Users.AnyAsync(u => u.Username == "admin"))
            {
                db.Users.Add(new User
                {
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1Inst!"),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();

        // 2-2. 로그인 접속 및 폼 입력 제출
        await page.GotoAsync(_helper.ServerAddress + "/login");
        await page.WaitForSelectorAsync("input[name='email']");
        
        await page.FillAsync("input[name='email']", "admin@example.com");
        await page.FillAsync("input[name='password']", "Password1Inst!");
        await page.ClickAsync("button[type='submit']");

        // 로그인 후 리다이렉션 경로 대기
        await page.WaitForURLAsync(_helper.ServerAddress + "/dashboard");

        // 2-3. 저장소 생성 페이지 접속
        await page.ClickAsync("text=새 저장소 만들기");
        await page.WaitForURLAsync("**/repositories/new");
        await page.WaitForSelectorAsync("input#repo-name");

        // 저장소 정보 기입 및 제출 (Public 지정)
        await page.FillAsync("input#repo-name", "e2e-project");
        await page.SetCheckedAsync("input#is-private", false);
        await page.ClickAsync("button[type='submit']");

        // 상세 화면 리다이렉트 대기
        await page.WaitForURLAsync("**/admin/e2e-project");

        // 백그라운드 RepositoryCreationBackgroundWorker 파일 IO 작업 대기
        await Task.Delay(2500);

        try
        {
            // 로컬 디스크 생성 상태 최종 검증
            string expectedPath = Path.Combine(Directory.GetCurrentDirectory(), "GitRepos", "admin", "e2e-project.git");
            Assert.True(Directory.Exists(expectedPath));

            // 백그라운드 폴더 생성 지연으로 인해 발생했을 에러 메시지 해소를 위해 새로고침 실행
            await page.ReloadAsync();

            // 2-4. 이슈 작성 (웹소켓 연결 연속성 보장을 위해 UI 클릭 내비게이션 활용)
            await page.ClickAsync("text=Issues");
            await page.WaitForURLAsync("**/issues");
            await page.ClickAsync("text=Create Issue");
            await page.WaitForURLAsync("**/issues/new");
            await page.WaitForSelectorAsync("input#title");

            await page.FillAsync("input#title", "E2E Issue Title");
            await page.FillAsync("textarea#description", "This is an E2E test issue description.");
            await page.ClickAsync("button[type='submit']");

            // 이슈 목록 이동 검증 및 작성 정보 표시 확인
            await page.WaitForURLAsync("**/admin/e2e-project/issues");
            
            // 칸반 보드에 이슈 카드가 렌더링될 때까지 대기
            await page.Locator("text=E2E Issue Title").WaitForAsync();
            
            var finalBodyText = await page.InnerTextAsync("body");
            Assert.Contains("E2E Issue Title", finalBodyText);
        }
        catch (Exception ex)
        {
            var url = page.Url;
            var content = await page.ContentAsync();
            throw new InvalidOperationException($"E2E Test failed. Current URL: {url}\nPage HTML:\n{content}", ex);
        }
    }

    public void Dispose()
    {
        _helper.Dispose();
        
        // 임시 셋업 디비 리소스 정리
        if (!string.IsNullOrEmpty(_tempSetupDbName))
        {
            string setupDbPath = Path.Combine(Directory.GetCurrentDirectory(), _tempSetupDbName);
            if (File.Exists(setupDbPath))
            {
                try { File.Delete(setupDbPath); } catch { }
            }
        }
    }
}
