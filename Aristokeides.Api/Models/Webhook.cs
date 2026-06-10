using System;
using System.Collections.Generic;

namespace Aristokeides.Api.Models;

public class Webhook
{
    public int Id { get; set; }
    public Guid RepositoryId { get; set; }
    public required string Url { get; set; }
    public string? Secret { get; set; }
    public required string ContentType { get; set; } = "application/json"; // "application/json"
    public required string WebhookType { get; set; } = "Generic"; // "Generic", "Slack", "Discord"
    public bool IsActive { get; set; } = true;
    public required string TriggerEvents { get; set; } = "push"; // Comma-separated: "push,issue,pull_request"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Repository Repository { get; set; } = null!;
    public ICollection<WebhookDelivery> Deliveries { get; set; } = new List<WebhookDelivery>();
}
