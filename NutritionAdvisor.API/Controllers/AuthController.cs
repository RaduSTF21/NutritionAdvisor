using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NutritionAdvisor.Application.Users.Commands.LoginUser;
using NutritionAdvisor.Application.Users.Commands.RegisterUser;

namespace NutritionAdvisor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public AuthController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        try
        {
            var userId = await _mediator.Send(command);
            return Ok(new { UserId = userId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                new(ClaimTypes.Name, result.Name),
                new(ClaimTypes.Email, result.Email),
                new("subscription_plan", result.SubscriptionPlan.ToString()),
                new("subscription_status", result.SubscriptionStatus.ToString())
            };

            if (result.SubscriptionEndAt.HasValue)
            {
                claims.Add(new Claim("subscription_expires_at", result.SubscriptionEndAt.Value.ToString("O")));
            }

            // ASP.NET mapează automat variabila Jwt__Key din .env la acest camp
            var jwtKey = _configuration["Jwt:Key"];

            // Validare strictă
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32 || jwtKey.StartsWith("REPLACE_WITH"))
            {
                throw new InvalidOperationException("A secure JWT key is not configured in the environment.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}