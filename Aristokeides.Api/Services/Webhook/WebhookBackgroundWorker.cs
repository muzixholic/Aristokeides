using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;

namespace Aristokeides.Api.Services.Webhook;

public class WebhookBackgroundWorker : BackgroundService
{
    private readonly WebhookQueue _queue;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookBackgroundWorker> _logger;

    public WebhookBackgroundWorker(
        WebhookQueue queue,
        IHttpClientFactory httpClientFactory,
        IServiceProvider serviceProvider,
        ILogger<WebhookBackgroundWorker> logger)
    {
        _queue = queue;
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebhookBackgroundWorker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var task = await _queue.DequeueWebhookTaskAsync(stoppingToken);
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessWebhookTaskAsync(task);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing WebhookTask for WebhookId: {WebhookId}", task.WebhookId);
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dequeueing WebhookTask.");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("WebhookBackgroundWorker stopping.");
    }

    private async Task ProcessWebhookTaskAsync(WebhookTask task)
    {
        _logger.LogInformation("Processing webhook task for ID: {WebhookId}, URL: {Url}", task.WebhookId, task.TargetUrl);

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        var request = new HttpRequestMessage(HttpMethod.Post, task.TargetUrl);
        request.Headers.Add("X-Aristokeides-Event", task.EventType);
        request.Headers.Add("X-Aristokeides-Delivery", task.DeliveryId.ToString());

        // Secret 서명 해시 계산 및 추가
        if (!string.IsNullOrEmpty(task.Secret))
        {
            var signature = WebhookService.GenerateSignature(task.Payload, task.Secret);
            request.Headers.Add("X-Aristokeides-Signature-256", signature);
        }

        // 템플릿 변환 어댑터 (Slack/Discord 대응은 21C에서 고도화하지만 뼈대 연동)
        var finalPayload = task.Payload;
        if (task.WebhookType.Equals("Slack", StringComparison.OrdinalIgnoreCase))
        {
            // Slack 포맷 가공 뼈대
            var slackPayload = new { text = task.Payload };
            finalPayload = JsonSerializer.Serialize(slackPayload);
        }
        else if (task.WebhookType.Equals("Discord", StringComparison.OrdinalIgnoreCase))
        {
            // Discord 포맷 가공 뼈대
            var discordPayload = new { content = task.Payload };
            finalPayload = JsonSerializer.Serialize(discordPayload);
        }

        request.Content = new StringContent(finalPayload, Encoding.UTF8, task.ContentType);

        // 요청 헤더 직렬화
        var requestHeadersDict = new Dictionary<string, string>();
        foreach (var header in request.Headers)
        {
            requestHeadersDict[header.Key] = string.Join(", ", header.Value);
        }
        if (request.Content.Headers != null)
        {
            foreach (var header in request.Content.Headers)
            {
                requestHeadersDict[header.Key] = string.Join(", ", header.Value);
            }
        }
        var requestHeadersJson = JsonSerializer.Serialize(requestHeadersDict);

        int statusCode = 0;
        string? responseHeadersJson = null;
        string? responseBody = null;
        bool isSuccess = false;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await client.SendAsync(request);
            stopwatch.Stop();

            statusCode = (int)response.StatusCode;
            isSuccess = response.IsSuccessStatusCode;

            // 응답 헤더 직렬화
            var respHeadersDict = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                respHeadersDict[header.Key] = string.Join(", ", header.Value);
            }
            if (response.Content.Headers != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    respHeadersDict[header.Key] = string.Join(", ", header.Value);
                }
            }
            responseHeadersJson = JsonSerializer.Serialize(respHeadersDict);

            // DoS 방어: 응답 최대 64KB 절삭
            using var stream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[65536];
            int totalRead = 0;
            int read;
            while (totalRead < buffer.Length && (read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead)) > 0)
            {
                totalRead += read;
            }
            responseBody = Encoding.UTF8.GetString(buffer, 0, totalRead);
            if (stream.ReadByte() != -1) // 더 읽을 데이터가 존재함
            {
                responseBody += "\n[Response Body Truncated - Exceeded 64KB Limit]";
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            statusCode = 0;
            isSuccess = false;
            responseBody = $"Failed to send webhook: {ex.Message}\nDetails: {ex.StackTrace}";
            _logger.LogError(ex, "Failed to send webhook to {Url}", task.TargetUrl);
        }

        // DB 로그 업데이트 (Scoped AppDbContext Resolve)
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var deliveryLog = await db.WebhookDeliveries.FindAsync(task.WebhookDeliveryId);
            if (deliveryLog != null)
            {
                deliveryLog.RequestHeaders = requestHeadersJson;
                deliveryLog.ResponseHeaders = responseHeadersJson;
                deliveryLog.ResponseBody = responseBody;
                deliveryLog.HttpStatusCode = statusCode;
                deliveryLog.IsSuccess = isSuccess;
                deliveryLog.DurationMs = stopwatch.ElapsedMilliseconds;
                deliveryLog.DeliveredAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
            }
        }
    }
}
