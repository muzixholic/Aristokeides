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
}
