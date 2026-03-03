using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Creăm o identitate goală. Fără date = vizitator nelogat.
        var anonymousIdentity = new ClaimsIdentity(); 
        var user = new ClaimsPrincipal(anonymousIdentity);
        
        return Task.FromResult(new AuthenticationState(user));
    }
}