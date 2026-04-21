using Microsoft.EntityFrameworkCore;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;
using NutritionAdvisor.Infrastructure.Databases;

namespace NutritionAdvisor.Infrastructure.Repositories;

public class AllergyRepository : IAllergyRepository
{
    private readonly ApplicationDbContext _context;

    public AllergyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Allergy>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.Allergies
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.AllergenName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Allergy allergy, CancellationToken cancellationToken)
    {
        await _context.Allergies.AddAsync(allergy, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid allergyId, Guid userId, CancellationToken cancellationToken)
    {
        var allergy = await _context.Allergies
            .FirstOrDefaultAsync(a => a.Id == allergyId && a.UserId == userId, cancellationToken);

        if (allergy == null)
        {
            return false;
        }

        _context.Allergies.Remove(allergy);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
