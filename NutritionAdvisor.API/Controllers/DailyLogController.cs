namespace NutritionAdvisor.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.DailyLogs.Commands.LogMeal;

[ApiController]
[Route("api/[controller]")]

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
        var id = await _sender.Send(command);
        return Ok(id);
    }
}