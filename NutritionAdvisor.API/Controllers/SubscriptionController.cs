using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetCurrentSubscription()
    {
        var user = ControllerContext?.HttpContext?.User;
        if (user?.Identity == null || user.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var planClaim = user.FindFirst("subscription_plan")?.Value;
        var statusClaim = user.FindFirst("subscription_status")?.Value;
        var expiresAtClaim = user.FindFirst("subscription_expires_at")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId) ||
            string.IsNullOrWhiteSpace(planClaim) ||
            string.IsNullOrWhiteSpace(statusClaim))
        {
            return Unauthorized();
        }

        DateTime? expiresAt = null;
        if (!string.IsNullOrWhiteSpace(expiresAtClaim) &&
            DateTime.TryParse(expiresAtClaim, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedExpiresAt))
        {
            expiresAt = parsedExpiresAt;
        }

        return Ok(new
        {
            UserId = userId,
            Plan = planClaim,
            Status = statusClaim,
            ExpiresAt = expiresAt
        });
    }
}
