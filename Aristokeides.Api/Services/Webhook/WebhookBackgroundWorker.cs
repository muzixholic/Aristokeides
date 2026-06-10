using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                
                // 백그라운드에서 병렬 발송 수행 (큐 막힘 방지)
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
        
        // 10초 타임아웃
        client.Timeout = TimeSpan.FromSeconds(10);

        var request = new HttpRequestMessage(HttpMethod.Post, task.TargetUrl);
        request.Headers.Add("X-Aristokeides-Event", task.EventType);
        request.Headers.Add("X-Aristokeides-Delivery", task.DeliveryId.ToString());

        request.Content = new StringContent(task.Payload, Encoding.UTF8, task.ContentType);

        // [Skeleton Logic] (Signature, logging update and slack/discord adapters integrated in 21B/21C)
        try
        {
            var response = await client.SendAsync(request);
            _logger.LogInformation("Webhook sent. Result code: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook to {Url}", task.TargetUrl);
        }
    }
}
