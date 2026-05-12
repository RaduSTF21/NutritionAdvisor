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

// S1075: Preluăm URL-ul exclusiv din appsettings.json.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is missing from appsettings.json or configuration.");
}

builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<AuthHeaderHandler>();

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthHeaderHandler>();

    handler.InnerHandler ??= new HttpClientHandler();

    return new HttpClient(handler) { BaseAddress = new Uri(apiBaseUrl) };
});

builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

await builder.Build().RunAsync();