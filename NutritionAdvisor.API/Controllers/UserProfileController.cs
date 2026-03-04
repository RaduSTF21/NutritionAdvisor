using Microsoft.AspNetCore.Mvc;
using MediatR;
using NutritionAdvisor.Application.UserProfile.Commands;
using NutritionAdvisor.Application.UserProfile.Commands.SaveUserProfile;


namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfileController:ControllerBase
{
    private readonly IMediator _mediator;
    
    public UserProfileController(IMediator mediator)
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