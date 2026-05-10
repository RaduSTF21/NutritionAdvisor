using System.Text.Json.Serialization;

namespace Frontend.Models;

public sealed record SubscriptionModel(
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("plan")] string Plan,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("expiresAt")] DateTime? ExpiresAt);
