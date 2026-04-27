using MediatR;
using NutritionAdvisor.Application.Interfaces;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.Allergies.Commands.AddAllergy;

public class AddAllergyCommandHandler : IRequestHandler<AddAllergyCommand, Guid>
{
    private readonly IAllergyRepository _repository;

    public AddAllergyCommandHandler(IAllergyRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(AddAllergyCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AllergenName))
        {
            throw new ArgumentException("Allergen name is required.");
        }

        var allergenName = request.AllergenName.Trim();
        var existingAllergies = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        var existing = existingAllergies
            .FirstOrDefault(a => string.Equals(a.AllergenName, allergenName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            return existing.Id;
        }

        var allergy = new Allergy
        {
            UserId = request.UserId,
            AllergenName = allergenName,
            SeverityLevel = request.SeverityLevel
        };

        await _repository.AddAsync(allergy, cancellationToken);
        return allergy.Id;
    }
}
