using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Enums;
using System.Security.Claims;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Am eliminat Policy-ul de aici. Verificăm manual.
public class AIController : ControllerBase
{
    private readonly IPythonAiService _aiService;
    private readonly IUserRepository _userRepository;

    public AIController(IPythonAiService aiService, IUserRepository userRepository)
    {
        _aiService = aiService;
        _userRepository = userRepository;
    }

    // Metodă care citește mereu realitatea la zi din Baza de Date
    private async Task<bool> IsUserPremiumAsync()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdString, out var userId))
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null
                   && user.SubscriptionPlan == SubscriptionPlan.Premium
                   && user.SubscriptionStatus == SubscriptionStatus.Active;
        }
        return false;
    }

    [HttpPost("recommend-recipes")]
    public async Task<IActionResult> RecommendRecipes([FromBody] AiRecommendationRequestModel request)
    {
        // Aceasta este funcția "Free", deci o lăsăm să treacă
        // Aici pui logica ta de apelare a serviciului Python
        // Exemplu: var result = await _aiService.GetRecommendationsAsync(request);
        return Ok(new { Recommendations = new List<object>() }); // Placeholder până legi Python-ul
    }

    [HttpPost("generate-meal-plan")]
    public async Task<IActionResult> GenerateMealPlan([FromBody] AiMealPlanRequestModel request)
    {
        // VERIFICARE PREMIUM
        if (!await IsUserPremiumAsync())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Premium subscription required." });
        }

        // Logica Python AI
        return Ok(new { Summary = "Premium Meal Plan Generated", Days = new List<object>() }); // Placeholder
    }

    [HttpPost("coach")]
    public async Task<IActionResult> AskCoach([FromBody] AiCoachRequestModel request)
    {
        // VERIFICARE PREMIUM
        if (!await IsUserPremiumAsync())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Premium subscription required." });
        }

        // Logica Python AI
        return Ok(new { Answer = "I am your AI Coach!", Tips = new[] { "Eat veggies." } }); // Placeholder
    }
}

// Modele DTO pentru a evita erori de compilare dacă nu le ai deja definite în API
public record AiRecommendationRequestModel(int Limit);
public record AiMealPlanRequestModel(int Days);
public record AiCoachRequestModel(string Message, string? Context);