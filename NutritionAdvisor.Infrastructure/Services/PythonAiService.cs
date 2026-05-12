using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NutritionAdvisor.Application.AI;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Infrastructure.Options;

namespace NutritionAdvisor.Infrastructure.Services;

public class PythonAiService : IPythonAiService
{
    private readonly HttpClient _httpClient;

    public PythonAiService(HttpClient httpClient, IOptions<PythonAiOptions> options)
    {
        _httpClient = httpClient;
        var pythonAiOptions = options.Value;

        // Validăm că URL-ul este configurat corect în appsettings.json
        if (string.IsNullOrWhiteSpace(pythonAiOptions.BaseUrl))
        {
            throw new InvalidOperationException("PythonAI BaseUrl is missing from configuration.");
        }

        _httpClient.BaseAddress = new Uri(pythonAiOptions.BaseUrl);
    }

    public async Task<AiRecommendationResponseModel?> GetRecommendationsAsync(AiRecommendationRequestModel request)
    {
        var response = await _httpClient.PostAsJsonAsync("recommend", request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"AI Service recommendation error: {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<AiRecommendationResponseModel>();
    }

    public async Task<AiMealPlanResponseModel?> GenerateMealPlanAsync(AiMealPlanRequestModel request)
    {
        var response = await _httpClient.PostAsJsonAsync("meal-plan", request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"AI Service meal plan error: {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<AiMealPlanResponseModel>();
    }

    public async Task<AiCoachResponseModel?> GetCoachAdviceAsync(AiCoachRequestModel request)
    {
        var response = await _httpClient.PostAsJsonAsync("coach", request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"AI Service coach error: {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<AiCoachResponseModel>();
    }

    public async Task PrewarmAsync(AiMealPlanRequestModel request)
    {
        try
        {
            // Fire-and-forget: we don't await the response body, just send it
            await _httpClient.PostAsJsonAsync("prewarm", request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            // ignore network errors for prewarming
        }
    }
}