using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using NutritionAdvisor.Application.UserProfiles.Commands;
using NutritionAdvisor.Application.UserProfiles.Commands.SaveUserProfile;
using NutritionAdvisor.Application.UserProfiles.Queries.GetUserProfile;


namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> SaveProfile([FromBody] SaveUserProfileCommand command)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var authenticatedUserId))
        {
            return Unauthorized();
        }

        if (authenticatedUserId != command.UserId)
        {
            return Forbid();
        }

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetProfile(Guid userId)
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

        var querry = new GetUserProfileQuery(userId);

        var result = await _mediator.Send(querry);
        return Ok(result);
    }

}