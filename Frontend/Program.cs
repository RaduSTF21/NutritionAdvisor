using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using Frontend.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Preluăm portul 5066 (cel definit în docker-compose pentru API)
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5066";

builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();


builder.Services.AddScoped<AuthHeaderHandler>();

// Configurăm HttpClient-ul principal care folosește acel handler
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthHeaderHandler>();
    
    // Ne asigurăm că handler-ul customizat are motorul HTTP de bază atașat
    if (handler.InnerHandler == null)
    {
        handler.InnerHandler = new HttpClientHandler();
    }
    
    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
});

builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

await builder.Build().RunAsync();