using System.Text.Json.Serialization;

namespace NutritionAdvisor.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Difficulty
{
    Easy,
    Medium,
    Hard
}