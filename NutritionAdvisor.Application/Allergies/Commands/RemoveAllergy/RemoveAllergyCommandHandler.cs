using MediatR;
using NutritionAdvisor.Application.Interfaces;

namespace NutritionAdvisor.Application.Allergies.Commands.RemoveAllergy;

public class RemoveAllergyCommandHandler : IRequestHandler<RemoveAllergyCommand, bool>
{
    private readonly IAllergyRepository _repository;

    public RemoveAllergyCommandHandler(IAllergyRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(RemoveAllergyCommand request, CancellationToken cancellationToken)
    {
        return await _repository.DeleteAsync(request.AllergyId, request.UserId, cancellationToken);
    }
}
