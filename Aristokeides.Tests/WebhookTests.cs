using System;
using System.Text.Json;
using Xunit;
using Aristokeides.Api.Services.Webhook;

namespace Aristokeides.Tests;

public class WebhookTests
{
    [Fact]
    public void Test_Webhook_Signature_Generation()
    {
        var payload = "{\"hello\":\"world\"}";
        var secret = "my-secret-key";
        var signature = WebhookService.GenerateSignature(payload, secret);

        Assert.StartsWith("sha256=", signature);
        Assert.Equal(64 + 7, signature.Length); // sha256= + 64 hex characters
    }

    [Fact]
    public void Test_Webhook_Ssrf_Prevention()
    {
        var service = new WebhookService(null!, null!, null!);

        Assert.True(service.IsPrivateOrLocalUrl("http://localhost/test"));
        Assert.True(service.IsPrivateOrLocalUrl("http://127.0.0.1/test"));
        Assert.True(service.IsPrivateOrLocalUrl("http://192.168.1.1/test"));
        Assert.True(service.IsPrivateOrLocalUrl("http://10.0.0.1/test"));
        Assert.True(service.IsPrivateOrLocalUrl("http://172.16.0.1/test"));
        Assert.True(service.IsPrivateOrLocalUrl("http://fc00::1/test"));

        // 공용 IP 대역 등은 차단하지 않아야 함
        Assert.False(service.IsPrivateOrLocalUrl("https://8.8.8.8/test"));
        Assert.False(service.IsPrivateOrLocalUrl("https://github.com/webhook"));
    }

    [Fact]
    public void Test_Webhook_Adapter_Slack()
    {
        var commonPayload = new
        {
            repository = new { name = "my-repo" },
            sender = new { username = "user-a" },
            data = new
            {
                action = "opened",
                issue = new
                {
                    number = 42,
                    title = "Test Issue",
                    body = "This is a body",
                    html_url = "https://localhost:5001/user/repo/issues/42"
                }
            }
        };

        var json = JsonSerializer.Serialize(commonPayload);
        var slackJson = WebhookService.TransformToSlack("issue", json);

        using var doc = JsonDocument.Parse(slackJson);
        var text = doc.RootElement.GetProperty("text").GetString();

        Assert.Contains("my-repo", text);
        Assert.Contains("#42", text);
        Assert.Contains("user-a", text);
        Assert.Contains("Test Issue", text);
    }

    [Fact]
    public void Test_Webhook_Adapter_Discord()
    {
        var commonPayload = new
        {
            repository = new { name = "my-repo" },
            sender = new { username = "user-b" },
            data = new
            {
                @ref = "refs/heads/main",
                commits = new[]
                {
                    new { id = "abc123456", message = "Fix something", author = "user-b" }
                }
            }
        };

        var json = JsonSerializer.Serialize(commonPayload);
        var discordJson = WebhookService.TransformToDiscord("push", json);

        using var doc = JsonDocument.Parse(discordJson);
        var content = doc.RootElement.GetProperty("content").GetString();

        Assert.Contains("my-repo", content);
        Assert.Contains("main", content);
        Assert.Contains("user-b", content);
        Assert.Contains("Fix something", content);
        Assert.Contains("abc1234", content);
    }
}
