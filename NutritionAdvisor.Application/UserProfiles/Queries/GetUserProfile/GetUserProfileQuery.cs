using MediatR;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.UserProfiles.Queries.GetUserProfile;

// Interogarea cere un profil pe baza unui Id și promite să returneze un obiect UserProfile
public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfile?>;