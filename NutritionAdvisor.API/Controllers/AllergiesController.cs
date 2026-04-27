using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.Allergies.Commands.AddAllergy;
using NutritionAdvisor.Application.Allergies.Commands.RemoveAllergy;
using NutritionAdvisor.Application.Allergies.Queries.GetUserAllergies;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AllergiesController : ControllerBase
{
    private readonly ISender _sender;

    public AllergiesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(new GetUserAllergiesQuery(userId));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddAllergyCommand command)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        command.UserId = userId;
        var id = await _sender.Send(command);
        return Ok(id);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var deleted = await _sender.Send(new RemoveAllergyCommand(userId, id));
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
