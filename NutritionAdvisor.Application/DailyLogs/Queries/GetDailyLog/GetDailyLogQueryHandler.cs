using MediatR;
using NutritionAdvisor.Application.DailyLogs.Queries.GetDailyLog;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

public class GetDailyLogQueryHandler : IRequestHandler<GetDailyLogQuery, DailyLog?>
{
    private readonly IDailyLogRepository _dailyLogRepository;

    public GetDailyLogQueryHandler(IDailyLogRepository dailyLogRepository)
    {
        _dailyLogRepository = dailyLogRepository;
    }

    public async Task<DailyLog?> Handle(GetDailyLogQuery request, CancellationToken cancellationToken)
    {
        return await _dailyLogRepository.GetByDateAsync(request.UserId, request.Date, cancellationToken);
    }
}