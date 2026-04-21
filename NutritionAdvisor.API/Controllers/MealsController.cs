using MediatR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.Meals.Commands.DeleteMeal;
namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class MealsController : ControllerBase
{
    private readonly ISender _sender;
    public MealsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMeal(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var command = new DeleteMealCommand(id, userId);
        await _sender.Send(command);
        return NoContent();
    }
}