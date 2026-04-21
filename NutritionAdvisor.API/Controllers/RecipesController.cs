using MediatR;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.Recipes.Commands.CreateRecipe;
using NutritionAdvisor.Application.Recipes.Queries.GetAllRecipes;
using NutritionAdvisor.Application.Recipes.Queries.GetRecipesById;

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
    public async Task<IActionResult> Create([FromBody] CreateRecipeCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
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
}