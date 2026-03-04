using Microsoft.AspNetCore.Mvc;
using MediatR;
using NutritionAdvisor.Application.UserProfiles.Commands;
using NutritionAdvisor.Application.UserProfiles.Commands.SaveUserProfile;
using NutritionAdvisor.Application.UserProfiles.Queries.GetUserProfile;


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

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetProfile(Guid userId)
    {
        var querry = new GetUserProfileQuery(userId);
        
        var result = await _mediator.Send(querry);
        return Ok(result);
    }
    
}