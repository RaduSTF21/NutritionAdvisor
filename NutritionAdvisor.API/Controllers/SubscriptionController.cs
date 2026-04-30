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

    // Injectăm Repository-ul pentru a citi direct din baza de date
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

        // Citim statusul proaspăt și real din baza de date!
        var dbUser = await _userRepository.GetByIdAsync(userId);

        if (dbUser == null)
        {
            return NotFound("Utilizatorul nu a fost găsit în baza de date.");
        }

        // Extragem valorile actualizate. Dacă ai o altă proprietate pentru ExpiresAt în Domain/Entities/User, o poți adăuga aici
        return Ok(new
        {
            UserId = dbUser.UserId,
            Plan = dbUser.SubscriptionPlan.ToString(),
            Status = dbUser.SubscriptionStatus.ToString()
            // Dacă ai data de expirare în DB, adaug-o aici: ExpiresAt = dbUser.SubscriptionExpiresAt
        });
    }
}