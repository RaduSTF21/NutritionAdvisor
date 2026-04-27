namespace NutritionAdvisor.API.Controllers;

using MediatR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.DailyLogs.Commands.LogMeal;
using NutritionAdvisor.Application.DailyLogs.Queries.GetDailyLog;
using NutritionAdvisor.Domain.Entities;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class DailyLogController : ControllerBase
{
    private readonly ISender _sender;
    public DailyLogController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("log-meal")]
    public async Task<IActionResult> LogMeal([FromBody] LogMealCommand command)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var securedCommand = new LogMealCommand(userId, command.RecipeId);
        var id = await _sender.Send(securedCommand);
        return Ok(id);
    }

    [HttpGet("{userId}/{date}")]
    public async Task<IActionResult> GetDailyLog(Guid userId, DateTime date)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var authenticatedUserId))
        {
            return Unauthorized();
        }

        if (authenticatedUserId != userId)
        {
            return Forbid();
        }

        var query = new GetDailyLogQuery { UserId = userId, Date = date };
        var result = await _sender.Send(query);
        if (result == null)
            return Ok(new DailyLog
            {
                UserId = userId,
                Date = date.Date,
                Meals = new List<Meal>()
            });
        return Ok(result);
    }
}