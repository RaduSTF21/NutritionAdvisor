using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.Recipes.Commands.CreateRecipe;
using NutritionAdvisor.Application.Recipes.Commands.DeleteRecipe;
using NutritionAdvisor.Application.Recipes.Commands.UpdateRecipe;
using NutritionAdvisor.Application.Recipes.Queries.GetAllRecipes;
using NutritionAdvisor.Application.Recipes.Queries.GetRecipesByFilter;
using NutritionAdvisor.Application.Recipes.Queries.GetRecipesById;
using NutritionAdvisor.Domain.Enums;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RecipesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Forward the request to the query handler.
        var result = await _mediator.Send(new GetAllRecipesQuery());
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateRecipeCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }

    [HttpGet("filter")]
    public async Task<IActionResult> GetFiltered([FromQuery] string? searchTerm, [FromQuery] string? tag, [FromQuery] Difficulty? level)
    {
        var result = await _mediator.Send(new GetRecipesByFilterQuery
        {
            SearchTerm = searchTerm,
            Tag = tag,
            Level = level
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var recipe = await _mediator.Send(new GetRecipeByIdQuery(id));
        if (recipe == null)
        {
            return NotFound();
        }
        return Ok(recipe);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRecipeCommand command)
    {
        command.Id = id;
        var updated = await _mediator.Send(command);
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _mediator.Send(new DeleteRecipeCommand(id));
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}