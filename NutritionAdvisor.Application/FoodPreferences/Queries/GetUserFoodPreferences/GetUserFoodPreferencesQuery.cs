using MediatR;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.FoodPreferences.Queries.GetUserFoodPreferences;

public class GetUserFoodPreferencesQuery : IRequest<FoodPreference?>
{
    public Guid UserId { get; set; }

    public GetUserFoodPreferencesQuery(Guid userId)
    {
        UserId = userId;
    }
}
