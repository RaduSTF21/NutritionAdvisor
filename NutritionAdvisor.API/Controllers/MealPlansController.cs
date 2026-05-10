using System.Globalization;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.MealPlans.Commands.CreateMealPlan;
using NutritionAdvisor.Application.MealPlans.Commands.CreateWeeklyMealPlan;
using NutritionAdvisor.Application.MealPlans.Commands.DeleteMealPlan;
using NutritionAdvisor.Application.MealPlans.Queries.GetMealPlanByDate;
using NutritionAdvisor.Application.MealPlans.Queries.GetMealPlans;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MealPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public MealPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var plans = await _mediator.Send(new GetMealPlansQuery { UserId = userId.Value });
        return Ok(plans);
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> GetByDate(string date)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return BadRequest("Use the yyyy-MM-dd date format.");
        }

        var plan = await _mediator.Send(new GetMealPlanByDateQuery
        {
            UserId = userId.Value,
            Date = parsedDate
        });

        return plan == null ? NotFound() : Ok(plan);
    }

    [HttpPost("daily")]
    public async Task<IActionResult> CreateDaily([FromBody] CreateMealPlanCommand command)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        command.UserId = userId.Value;
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpPost("weekly")]
    public async Task<IActionResult> CreateWeekly([FromBody] CreateWeeklyMealPlanCommand command)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        command.UserId = userId.Value;
        var ids = await _mediator.Send(command);
        return Ok(ids);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var deleted = await _mediator.Send(new DeleteMealPlanCommand
        {
            MealPlanId = id,
            UserId = userId.Value
        });

        return deleted ? NoContent() : NotFound();
    }

    private Guid? GetUserId()
    {
        var userIdString = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}