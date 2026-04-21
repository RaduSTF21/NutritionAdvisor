using System.Text.Json.Serialization;

namespace NutritionAdvisor.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AllergySeverity
{
    Mild,
    Moderate,
    Severe
}
