using MediatR;
using NutritionAdvisor.Domain.Entities;

namespace NutritionAdvisor.Application.UserProfiles.Queries.GetUserProfile;

// Query that loads a user profile by user ID.
public record GetUserProfileQuery(Guid UserId) : IRequest<UserProfile?>;