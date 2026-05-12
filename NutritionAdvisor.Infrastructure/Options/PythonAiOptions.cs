namespace NutritionAdvisor.Infrastructure.Options;

public sealed class PythonAiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 180;
}
