using MediatR;
using NutritionAdvisor.Application.MealPlans.Commands.CreateMealPlan;

namespace NutritionAdvisor.Application.MealPlans.Commands.CreateWeeklyMealPlan;

public class CreateWeeklyMealPlanCommandHandler : IRequestHandler<CreateWeeklyMealPlanCommand, List<Guid>>
{
    private readonly IMediator _mediator;

    public CreateWeeklyMealPlanCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<List<Guid>> Handle(CreateWeeklyMealPlanCommand request, CancellationToken cancellationToken)
    {
        var createdIds = new List<Guid>();

        foreach (var dailyPlan in request.DailyPlans)
        {
            var command = new CreateMealPlanCommand
            {
                UserId = request.UserId,
                Date = dailyPlan.Date,
                TargetCalories = dailyPlan.TargetCalories,
                IsAIGenerated = dailyPlan.IsAIGenerated,
                ReplaceExisting = true,
                Items = dailyPlan.Items
            };

            createdIds.Add(await _mediator.Send(command, cancellationToken));
        }

        return createdIds;
    }
}