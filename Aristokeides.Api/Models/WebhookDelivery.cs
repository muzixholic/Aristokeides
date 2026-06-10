using System;

namespace Aristokeides.Api.Models;

public class WebhookDelivery
{
    public int Id { get; set; }
    public int WebhookId { get; set; }
    public Guid DeliveryId { get; set; } // UUID
    public required string EventType { get; set; } // "push", "issue", "pull_request"
    public string? RequestHeaders { get; set; } // JSON
    public string? RequestBody { get; set; }
    public string? ResponseHeaders { get; set; }
    public string? ResponseBody { get; set; }
    public int HttpStatusCode { get; set; }
    public long DurationMs { get; set; }
    public bool IsSuccess { get; set; }
    public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;

    public Webhook Webhook { get; set; } = null!;
}
