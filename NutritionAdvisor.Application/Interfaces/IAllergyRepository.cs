using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Interfaces;

public interface IAllergyRepository
{
    Task<IEnumerable<Allergy>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddAsync(Allergy allergy, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid allergyId, Guid userId, CancellationToken cancellationToken);
}
