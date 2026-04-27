using System.Text.Json.Serialization;

namespace NutritionAdvisor.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DietType
{
    None,
    Vegetarian,
    Vegan,
    Keto,
    Paleo,
    Mediterranean
}
