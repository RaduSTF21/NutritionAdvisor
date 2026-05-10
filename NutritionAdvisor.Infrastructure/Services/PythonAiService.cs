using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using NutritionAdvisor.Application.AI;
using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Infrastructure.Services;

public class PythonAiService : IPythonAiService
{
    private readonly HttpClient _httpClient;

    public PythonAiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration["PythonAI:BaseUrl"] ?? "http://localhost:8000");
    }

    public async Task<AiRecommendationResponseModel?> GetRecommendationsAsync(AiRecommendationRequestModel request)
    {
        var response = await _httpClient.PostAsJsonAsync("/recommend", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AiRecommendationResponseModel>();
    }

    public async Task<AiMealPlanResponseModel?> GenerateMealPlanAsync(AiMealPlanRequestModel request)
    {
        var response = await _httpClient.PostAsJsonAsync("/meal-plan", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AiMealPlanResponseModel>();
    }

    public async Task<AiCoachResponseModel?> GetCoachAdviceAsync(AiCoachRequestModel request)
    {
        var response = await _httpClient.PostAsJsonAsync("/coach", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AiCoachResponseModel>();
    }
}
