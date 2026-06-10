using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aristokeides.Api.Services.Webhook;

public class WebhookTask
{
    public int WebhookId { get; set; }
    public Guid DeliveryId { get; set; }
    public string TargetUrl { get; set; } = "";
    public string? Secret { get; set; }
    public string WebhookType { get; set; } = "Generic"; // "Generic", "Slack", "Discord"
    public string ContentType { get; set; } = "application/json";
    public string EventType { get; set; } = ""; // "push", "issue", "pull_request"
    public string Payload { get; set; } = "";
    public int WebhookDeliveryId { get; set; } // 로그 업데이트용 DB ID
}

public class WebhookQueue
{
    private readonly Channel<WebhookTask> _channel;

    public WebhookQueue()
    {
        // 제한 없는(Unbounded) 채널 생성
        _channel = Channel.CreateUnbounded<WebhookTask>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    // 작업 인큐 (비동기)
    public async ValueTask QueueWebhookTaskAsync(WebhookTask task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        await _channel.Writer.WriteAsync(task);
    }

    // 작업 디큐 (비동기)
    public async ValueTask<WebhookTask> DequeueWebhookTaskAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
