using MediatR;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.DailyLogs.Queries.GetDailyLog;

public class GetDailyLogQuery : IRequest<DailyLog?>
{
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
}