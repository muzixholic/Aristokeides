using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using System.Text.Json;


namespace Aristokeides.Api.Services.Webhook;

public class WebhookService
{
    private readonly AppDbContext _db;
    private readonly WebhookQueue _queue;
    private readonly IHostEnvironment _env;

    public WebhookService(AppDbContext db, WebhookQueue queue, IHostEnvironment env)
    {
        _db = db;
        _queue = queue;
        _env = env;
    }

    // SSRF 방어 URL 유효성 검사
    public bool IsPrivateOrLocalUrl(string urlString)
    {
        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
            return true; // 잘못된 주소는 차단

        var host = uri.Host;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            var ips = Dns.GetHostAddresses(host);
            foreach (var ip in ips)
            {
                if (IPAddress.IsLoopback(ip))
                    return true;

                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    var bytes = ip.GetAddressBytes();
                    if (bytes[0] == 10) return true; // 10.0.0.0/8
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true; // 172.16.0.0/12
                    if (bytes[0] == 192 && bytes[1] == 168) return true; // 192.168.0.0/16
                }
                else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return true;
                    var bytes = ip.GetAddressBytes();
                    if ((bytes[0] & 0xFE) == 0xFC) return true; // fc00::/7 (ULA)
                }
            }
        }
        catch
        {
            return true; // 호스트 확인 불가능 시 차단
        }

        return false;
    }

    // HMAC-SHA256 서명 계산
    public static string GenerateSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return "sha256=" + Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    // 웹훅 트리거 이벤트 디스패치
    public async Task TriggerWebhookAsync(Guid repositoryId, string eventType, object payloadData)
    {
        var webhooks = await _db.Webhooks
            .Where(w => w.RepositoryId == repositoryId && w.IsActive)
            .ToListAsync();

        if (!webhooks.Any()) return;

        var payloadString = System.Text.Json.JsonSerializer.Serialize(payloadData);

        foreach (var webhook in webhooks)
        {
            // 이벤트 구독 여부 확인 (예: TriggerEvents에 push가 포함되어 있는지)
            var events = webhook.TriggerEvents.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                              .Select(e => e.Trim().ToLowerInvariant());

            if (!events.Contains(eventType.ToLowerInvariant()))
                continue;

            // SSRF 검사 (상용 모드일 때만 적용)
            if (!_env.IsDevelopment() && IsPrivateOrLocalUrl(webhook.Url))
            {
                continue; // 사설 IP 발송 원천 차단
            }

            var deliveryId = Guid.NewGuid();

            // 1. 선 로깅 저장 (Status: 0, Pending)
            var deliveryLog = new WebhookDelivery
            {
                WebhookId = webhook.Id,
                DeliveryId = deliveryId,
                EventType = eventType,
                RequestBody = payloadString,
                HttpStatusCode = 0,
                IsSuccess = false
            };

            _db.WebhookDeliveries.Add(deliveryLog);
            await _db.SaveChangesAsync();

            // 2. 인큐
            var task = new WebhookTask
            {
                WebhookId = webhook.Id,
                DeliveryId = deliveryId,
                TargetUrl = webhook.Url,
                Secret = webhook.Secret,
                WebhookType = webhook.WebhookType,
                ContentType = webhook.ContentType,
                EventType = eventType,
                Payload = payloadString,
                WebhookDeliveryId = deliveryLog.Id
            };

            await _queue.QueueWebhookTaskAsync(task);
        }
    }

    // 웹훅 이력 수동 재전송 (Redelivery)
    public async Task<bool> RedeliverAsync(int deliveryLogId)
    {
        var oldDelivery = await _db.WebhookDeliveries
            .Include(wd => wd.Webhook)
            .FirstOrDefaultAsync(wd => wd.Id == deliveryLogId);

        if (oldDelivery == null || oldDelivery.Webhook == null)
            return false;

        var webhook = oldDelivery.Webhook;

        // SSRF 검사 (상용 모드일 때만 적용)
        if (!_env.IsDevelopment() && IsPrivateOrLocalUrl(webhook.Url))
        {
            return false;
        }

        var newDeliveryId = Guid.NewGuid();

        // 1. 신규 로깅 생성
        var newDeliveryLog = new WebhookDelivery
        {
            WebhookId = webhook.Id,
            DeliveryId = newDeliveryId,
            EventType = oldDelivery.EventType,
            RequestBody = oldDelivery.RequestBody,
            HttpStatusCode = 0,
            IsSuccess = false
        };

        _db.WebhookDeliveries.Add(newDeliveryLog);
        await _db.SaveChangesAsync();

        // 2. 인큐
        var task = new WebhookTask
        {
            WebhookId = webhook.Id,
            DeliveryId = newDeliveryId,
            TargetUrl = webhook.Url,
            Secret = webhook.Secret,
            WebhookType = webhook.WebhookType,
            ContentType = webhook.ContentType,
            EventType = oldDelivery.EventType,
            Payload = oldDelivery.RequestBody ?? "",
            WebhookDeliveryId = newDeliveryLog.Id
        };

        await _queue.QueueWebhookTaskAsync(task);
        return true;
    }

    // Slack Incoming Webhook 규격으로 페이로드 변환
    public static string TransformToSlack(string eventType, string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            
            var repoName = root.TryGetProperty("repository", out var repoProp) && repoProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "unknown-repo";
            var senderName = root.TryGetProperty("sender", out var senderProp) && senderProp.TryGetProperty("username", out var userProp) ? userProp.GetString() : "unknown-user";
            
            string text = "";
            
            if (eventType.Equals("push", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("data", out var dataProp))
                {
                    var refStr = dataProp.TryGetProperty("ref", out var refProp) ? refProp.GetString() : "";
                    var branch = !string.IsNullOrEmpty(refStr) ? refStr.Replace("refs/heads/", "") : "unknown-branch";
                    
                    var commitsList = new List<string>();
                    if (dataProp.TryGetProperty("commits", out var commitsProp) && commitsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var commit in commitsProp.EnumerateArray())
                        {
                            var sha = commit.TryGetProperty("id", out var shaProp) ? shaProp.GetString() : "";
                            var shortSha = sha != null && sha.Length > 7 ? sha.Substring(0, 7) : sha;
                            var msg = commit.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "";
                            var author = commit.TryGetProperty("author", out var authProp) ? authProp.GetString() : "";
                            commitsList.Add($"- `{shortSha}` {msg} (by {author})");
                        }
                    }
                    
                    var commitDetails = string.Join("\n", commitsList);
                    text = $"🚀 *[{repoName}:{branch}]* {commitsList.Count} new commits pushed by *{senderName}*:\n{commitDetails}";
                }
                else
                {
                    text = $"🚀 *[{repoName}]* Push event triggered by *{senderName}*.";
                }
            }
            else if (eventType.Equals("issue", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("data", out var dataProp))
                {
                    var action = dataProp.TryGetProperty("action", out var actionProp) ? actionProp.GetString() : "updated";
                    
                    if (dataProp.TryGetProperty("issue", out var issueObj) && issueObj.ValueKind != JsonValueKind.Undefined)
                    {
                        var number = issueObj.TryGetProperty("number", out var numProp) ? numProp.GetInt32() : 0;
                        var title = issueObj.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : "";
                        var body = issueObj.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : "";
                        var htmlUrl = issueObj.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : "";
                        
                        var bodyPreview = !string.IsNullOrEmpty(body) ? (body.Length > 100 ? body.Substring(0, 100) + "..." : body) : "";
                        
                        var actionEmoji = action == "opened" ? "✏️" : (action == "closed" ? "✅" : "💬");
                        var link = string.IsNullOrEmpty(htmlUrl) ? $"#{number}" : $"<{htmlUrl}|#{number}>";
                        
                        if (action == "commented")
                        {
                            var commentBody = dataProp.TryGetProperty("comment", out var commentObj) && commentObj.TryGetProperty("body", out var cBodyProp) ? cBodyProp.GetString() : "";
                            var commentPreview = !string.IsNullOrEmpty(commentBody) ? (commentBody.Length > 100 ? commentBody.Substring(0, 100) + "..." : commentBody) : "";
                            
                            text = $"{actionEmoji} *[{repoName}]* Comment added on Issue {link} by *{senderName}*\n> *Comment:* {commentPreview}";
                        }
                        else
                        {
                            text = $"{actionEmoji} *[{repoName}]* Issue {link} *{action}* by *{senderName}*\n> *Title:* {title}\n> {bodyPreview}";
                        }
                    }
                    else
                    {
                        text = $"✏️ *[{repoName}]* Issue event ({action}) triggered by *{senderName}*.";
                    }
                }
            }
            else if (eventType.Equals("pull_request", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("data", out var dataProp))
                {
                    var action = dataProp.TryGetProperty("action", out var actionProp) ? actionProp.GetString() : "updated";
                    
                    if (dataProp.TryGetProperty("pull_request", out var prObj) && prObj.ValueKind != JsonValueKind.Undefined)
                    {
                        var number = prObj.TryGetProperty("number", out var numProp) ? numProp.GetInt32() : 0;
                        var title = prObj.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : "";
                        var body = prObj.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : "";
                        var htmlUrl = prObj.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : "";
                        
                        var bodyPreview = !string.IsNullOrEmpty(body) ? (body.Length > 100 ? body.Substring(0, 100) + "..." : body) : "";
                        
                        var actionEmoji = action == "opened" ? "🔀" : (action == "merged" ? "💜" : (action == "closed" ? "❌" : "💬"));
                        var link = string.IsNullOrEmpty(htmlUrl) ? $"#{number}" : $"<{htmlUrl}|#{number}>";
                        
                        if (action == "commented" || action == "review_commented")
                        {
                            var commentBody = dataProp.TryGetProperty("comment", out var commentObj) && commentObj.TryGetProperty("body", out var cBodyProp) ? cBodyProp.GetString() : "";
                            var commentPreview = !string.IsNullOrEmpty(commentBody) ? (commentBody.Length > 100 ? commentBody.Substring(0, 100) + "..." : commentBody) : "";
                            
                            text = $"{actionEmoji} *[{repoName}]* Review comment added on PR {link} by *{senderName}*\n> *Comment:* {commentPreview}";
                        }
                        else
                        {
                            text = $"{actionEmoji} *[{repoName}]* Pull Request {link} *{action}* by *{senderName}*\n> *Title:* {title}\n> {bodyPreview}";
                        }
                    }
                    else
                    {
                        text = $"🔀 *[{repoName}]* Pull Request event ({action}) triggered by *{senderName}*.";
                    }
                }
            }
            else
            {
                text = $"🔔 *[{repoName}]* Event `{eventType}` triggered by *{senderName}*.\n```json\n{payloadJson}\n```";
            }
            
            var slackPayload = new { text = text };
            return JsonSerializer.Serialize(slackPayload);
        }
        catch (Exception ex)
        {
            var fallbackPayload = new { text = $"⚠️ Error generating Slack webhook payload for event `{eventType}`: {ex.Message}" };
            return JsonSerializer.Serialize(fallbackPayload);
        }
    }

    // Discord Incoming Webhook 규격으로 페이로드 변환
    public static string TransformToDiscord(string eventType, string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            
            var repoName = root.TryGetProperty("repository", out var repoProp) && repoProp.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "unknown-repo";
            var senderName = root.TryGetProperty("sender", out var senderProp) && senderProp.TryGetProperty("username", out var userProp) ? userProp.GetString() : "unknown-user";
            
            string content = "";
            
            if (eventType.Equals("push", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("data", out var dataProp))
                {
                    var refStr = dataProp.TryGetProperty("ref", out var refProp) ? refProp.GetString() : "";
                    var branch = !string.IsNullOrEmpty(refStr) ? refStr.Replace("refs/heads/", "") : "unknown-branch";
                    
                    var commitsList = new List<string>();
                    if (dataProp.TryGetProperty("commits", out var commitsProp) && commitsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var commit in commitsProp.EnumerateArray())
                        {
                            var sha = commit.TryGetProperty("id", out var shaProp) ? shaProp.GetString() : "";
                            var shortSha = sha != null && sha.Length > 7 ? sha.Substring(0, 7) : sha;
                            var msg = commit.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "";
                            var author = commit.TryGetProperty("author", out var authProp) ? authProp.GetString() : "";
                            commitsList.Add($"- `{shortSha}` {msg} (by {author})");
                        }
                    }
                    
                    var commitDetails = string.Join("\n", commitsList);
                    content = $"🚀 **[{repoName}:{branch}]** {commitsList.Count} new commits pushed by **{senderName}**:\n{commitDetails}";
                }
                else
                {
                    content = $"🚀 **[{repoName}]** Push event triggered by **{senderName}**.";
                }
            }
            else if (eventType.Equals("issue", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("data", out var dataProp))
                {
                    var action = dataProp.TryGetProperty("action", out var actionProp) ? actionProp.GetString() : "updated";
                    
                    if (dataProp.TryGetProperty("issue", out var issueObj) && issueObj.ValueKind != JsonValueKind.Undefined)
                    {
                        var number = issueObj.TryGetProperty("number", out var numProp) ? numProp.GetInt32() : 0;
                        var title = issueObj.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : "";
                        var body = issueObj.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : "";
                        var htmlUrl = issueObj.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : "";
                        
                        var bodyPreview = !string.IsNullOrEmpty(body) ? (body.Length > 100 ? body.Substring(0, 100) + "..." : body) : "";
                        
                        var actionEmoji = action == "opened" ? "✏️" : (action == "closed" ? "✅" : "💬");
                        var link = string.IsNullOrEmpty(htmlUrl) ? $"#{number}" : $"[#{number}]({htmlUrl})";
                        
                        if (action == "commented")
                        {
                            var commentBody = dataProp.TryGetProperty("comment", out var commentObj) && commentObj.TryGetProperty("body", out var cBodyProp) ? cBodyProp.GetString() : "";
                            var commentPreview = !string.IsNullOrEmpty(commentBody) ? (commentBody.Length > 100 ? commentBody.Substring(0, 100) + "..." : commentBody) : "";
                            
                            content = $"{actionEmoji} **[{repoName}]** Comment added on Issue {link} by **{senderName}**\n> **Comment:** {commentPreview}";
                        }
                        else
                        {
                            content = $"{actionEmoji} **[{repoName}]** Issue {link} **{action}** by **{senderName}**\n> **Title:** {title}\n> {bodyPreview}";
                        }
                    }
                    else
                    {
                        content = $"✏️ **[{repoName}]** Issue event ({action}) triggered by **{senderName}**.";
                    }
                }
            }
            else if (eventType.Equals("pull_request", StringComparison.OrdinalIgnoreCase))
            {
                if (root.TryGetProperty("data", out var dataProp))
                {
                    var action = dataProp.TryGetProperty("action", out var actionProp) ? actionProp.GetString() : "updated";
                    
                    if (dataProp.TryGetProperty("pull_request", out var prObj) && prObj.ValueKind != JsonValueKind.Undefined)
                    {
                        var number = prObj.TryGetProperty("number", out var numProp) ? numProp.GetInt32() : 0;
                        var title = prObj.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : "";
                        var body = prObj.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() : "";
                        var htmlUrl = prObj.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : "";
                        
                        var bodyPreview = !string.IsNullOrEmpty(body) ? (body.Length > 100 ? body.Substring(0, 100) + "..." : body) : "";
                        
                        var actionEmoji = action == "opened" ? "🔀" : (action == "merged" ? "💜" : (action == "closed" ? "❌" : "💬"));
                        var link = string.IsNullOrEmpty(htmlUrl) ? $"#{number}" : $"[#{number}]({htmlUrl})";
                        
                        if (action == "commented" || action == "review_commented")
                        {
                            var commentBody = dataProp.TryGetProperty("comment", out var commentObj) && commentObj.TryGetProperty("body", out var cBodyProp) ? cBodyProp.GetString() : "";
                            var commentPreview = !string.IsNullOrEmpty(commentBody) ? (commentBody.Length > 100 ? commentBody.Substring(0, 100) + "..." : commentBody) : "";
                            
                            content = $"{actionEmoji} **[{repoName}]** Review comment added on PR {link} by **{senderName}**\n> **Comment:** {commentPreview}";
                        }
                        else
                        {
                            content = $"{actionEmoji} **[{repoName}]** Pull Request {link} **{action}** by **{senderName}**\n> **Title:** {title}\n> {bodyPreview}";
                        }
                    }
                    else
                    {
                        content = $"🔀 **[{repoName}]** Pull Request event ({action}) triggered by **{senderName}**.";
                    }
                }
            }
            else
            {
                content = $"🔔 **[{repoName}]** Event `{eventType}` triggered by **{senderName}**.\n```json\n{payloadJson}\n```";
            }
            
            var discordPayload = new { content = content };
            return JsonSerializer.Serialize(discordPayload);
        }
        catch (Exception ex)
        {
            var fallbackPayload = new { content = $"⚠️ Error generating Discord webhook payload for event `{eventType}`: {ex.Message}" };
            return JsonSerializer.Serialize(fallbackPayload);
        }
    }
}

