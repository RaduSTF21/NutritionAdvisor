using MediatR;
using Microsoft.AspNetCore.Mvc;
using NutritionAdvisor.Application.Ingredients.Commands.CreateIngredient;
using NutritionAdvisor.Application.Ingredients.Queries.SearchIngredients;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngredientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IngredientsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string searchTerm = "")
    {
        var query = new SearchIngredientsQuery { SearchTerm = searchTerm };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIngredientCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id); // Return the ID of the newly created ingredient.
    }
}