using Microsoft.AspNetCore.Mvc;
using MediatR;
using NutritionAdvisor.Application.Commands;
using NutritionAdvisor.Application.Commands.SaveUserProfile;


namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfieController:ControllerBase
{
    private readonly IMediator _mediator;
    
    public UserProfieController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> SaveProfile([FromBody] SaveUserProfileCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    
}