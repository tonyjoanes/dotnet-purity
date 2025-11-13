using Microsoft.AspNetCore.Authentication.JwtBearer;
using Purity.Api.Endpoints;
using Purity.Api.Infrastructure;
using Purity.Engine.Application;
using Sentry;

const string CorsPolicyName = "purity-frontend";

var builder = WebApplication.CreateBuilder(args);

// Configure Sentry (only if DSN is provided)
var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName;
        options.TracesSampleRate = builder.Environment.IsDevelopment() ? 1.0 : 0.1;
        options.Debug = builder.Environment.IsDevelopment();
    });
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        // For development, allow all origins
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>();

            if (allowedOrigins is { Length: > 0 })
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        }
    });
});

// Authentication is configured but not enforced on endpoints (Option A - development mode)
// TODO: Implement GitHub token validation and re-enable .RequireAuthorization() on endpoints
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var githubSection = builder.Configuration.GetSection("Authentication:GitHub");
        var authority = githubSection["Authority"];
        var audience = githubSection["Audience"];

        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
        }

        if (!string.IsNullOrWhiteSpace(audience))
        {
            options.Audience = audience;
        }

        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });

builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IAnalyzerCatalog, DefaultAnalyzerCatalog>();
builder.Services.AddSingleton<IAnalyzerRunner, AnalyzerRunner>();

var app = builder.Build();

// Sentry must be configured before other middleware (only if enabled)
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    app.UseSentryTracing();
}

app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapScanEndpoints();

// TODO: Re-enable authentication once GitHub token validation is implemented
app.MapGet("/results/{id}", (string id) =>
{
    return Results.NotFound(new
    {
        message = "Result storage is not implemented yet.",
        id
    });
}).WithName("GetScanResult")
  .WithSummary("Placeholder endpoint for retrieving historical scan results.");
  // .RequireAuthorization() temporarily disabled for development (Option A)

app.Run();
