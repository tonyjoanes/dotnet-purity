using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Purity.Frontend;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.Configuration["Api:BaseAddress"] ?? builder.HostEnvironment.BaseAddress;

// TODO: Re-enable authentication once GitHub token validation is implemented (Option B)
// Authentication temporarily disabled for development (Option A)
// builder.Services.AddOidcAuthentication(...);

// Simple HttpClient setup without authentication for development
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseAddress)
});

await builder.Build().RunAsync();
