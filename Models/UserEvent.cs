using System.Text.Json.Serialization;

namespace MSDisTestTask.Models;

public class UserEvent
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}

public class UserEventStats
{
    public int UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int Count { get; set; }
}