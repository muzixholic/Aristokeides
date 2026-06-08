using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Aristokeides.Tests;

public class LayoutAndStyleTests
{
    private readonly string _cssPath;
    private readonly string _razorPath;

    public LayoutAndStyleTests()
    {
        // Calculate paths relative to the test project base directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "../../../.."));
        _cssPath = Path.Combine(projectRoot, "Aristokeides.Api", "wwwroot", "css", "app.css");
        _razorPath = Path.Combine(projectRoot, "Aristokeides.Api", "Components", "MainLayout.razor");
    }

    [Fact]
    public void GlobalStylesheet_DefinesLayoutAndNavigationStyling()
    {
        Assert.True(File.Exists(_cssPath), $"app.css not found at {_cssPath}");
        var cssContent = File.ReadAllText(_cssPath);

        // 1. .nav-link.active
        Assert.Contains(".nav-link.active", cssContent);
        Assert.Contains("color: var(--accent);", cssContent);
        Assert.Contains("border-bottom: 2px solid var(--accent);", cssContent);

        // 2. .btn-nav-accent
        Assert.Contains(".btn-nav-accent", cssContent);
        Assert.Contains("background-color: var(--accent);", cssContent);
        Assert.Contains("padding:", cssContent);
        Assert.Contains("border-radius:", cssContent);

        // 3. .footer
        Assert.Contains(".footer", cssContent);
        Assert.Contains("display: flex;", cssContent);
        Assert.Contains("justify-content: space-between;", cssContent);

        // 4. @media (max-width: 640px)
        Assert.Contains("@media (max-width: 640px)", cssContent);
        var mediaQueryRegex = new Regex(@"@media[^{]+\{[^}]+\.navbar\s*\{[^}]*flex-direction:\s*column[^}]*\}[^}]+\}", RegexOptions.Singleline);
        Assert.Matches(mediaQueryRegex, cssContent);
    }

    [Fact]
    public void MainLayout_RendersNavigationLinksAndFooter()
    {
        Assert.True(File.Exists(_razorPath), $"MainLayout.razor not found at {_razorPath}");
        var razorContent = File.ReadAllText(_razorPath);

        // 1. <NavLink
        Assert.Contains("<NavLink", razorContent);

        // 2. Dashboard link inside Authorized
        var authorizedDashboardRegex = new Regex(@"<Authorized>.*?<NavLink[^>]*href=""/""[^>]*>대시보드</NavLink>.*?</Authorized>", RegexOptions.Singleline);
        Assert.Matches(authorizedDashboardRegex, razorContent);

        // 3. New repository link inside Authorized
        var newRepoRegex = new Regex(@"<Authorized>.*?<NavLink[^>]*class=""btn-nav-accent""[^>]*href=""/repositories/new""[^>]*>새 저장소 만들기</NavLink>.*?</Authorized>", RegexOptions.Singleline);
        Assert.Matches(newRepoRegex, razorContent);

        // 4. Footer with Swagger API link
        var footerRegex = new Regex(@"<footer class=""footer"">.*?<a[^>]*href=""/swagger""[^>]*>Swagger API</a>.*?</footer>", RegexOptions.Singleline);
        Assert.Matches(footerRegex, razorContent);
    }
}
