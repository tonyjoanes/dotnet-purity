using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Purity.Frontend;
using Sentry;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure Sentry (only if DSN is provided)
var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    SentrySdk.Init(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.HostEnvironment.Environment;
        options.TracesSampleRate = builder.HostEnvironment.IsDevelopment() ? 1.0 : 0.1;
        options.Debug = builder.HostEnvironment.IsDevelopment();
    });
}

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
