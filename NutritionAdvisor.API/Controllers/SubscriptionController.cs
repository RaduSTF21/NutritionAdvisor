using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public SubscriptionController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var dbUser = await _userRepository.GetByIdAsync(userId);

        if (dbUser == null)
        {
            return NotFound("Utilizatorul nu a fost găsit în baza de date.");
        }

        return Ok(new
        {
            UserId = dbUser.UserId,
            Plan = dbUser.SubscriptionPlan.ToString(),
            Status = dbUser.SubscriptionStatus.ToString(),
            ExpiresAt = dbUser.SubscriptionEndAt, // Trimitem noul câmp
            AutoRenew = dbUser.AutoRenew // Trimitem noul câmp
        });
    }
}