# Sentry Setup Guide

Sentry is integrated into both the API and Frontend projects for exception tracking, performance monitoring, and error reporting.

## What Sentry Provides

- **Exception Tracking**: Automatically captures unhandled exceptions
- **Performance Monitoring**: Tracks request performance and slow operations
- **Error Context**: Captures stack traces, user context, and environment info
- **Release Tracking**: Associates errors with code releases
- **Alerting**: Notifies you when errors occur

## Setup Steps

### 1. Create a Sentry Account

1. Go to https://sentry.io/signup/
2. Create a free account (or sign in if you have one)
3. Create a new project:
   - **API Project**: Select **ASP.NET Core** platform
   - **Frontend Project**: Select **JavaScript** platform
     - *Note: Even though we use the .NET Sentry SDK, Blazor WebAssembly runs in the browser, so use JavaScript as the platform*

### 2. Get Your DSN

After creating a project, Sentry will provide a DSN (Data Source Name) that looks like:
```
https://abc123@o123456.ingest.sentry.io/123456
```

You'll need **two separate DSNs**:
- One for the API project
- One for the Frontend project

### 3. Configure the API

**Important**: DSNs should not be committed to source control. Use one of these approaches:

#### Option A: Development Configuration File (Recommended for Local Development)

1. Copy the example file:
   ```bash
   cp src/Purity.Api/appsettings.Development.json.example src/Purity.Api/appsettings.Development.json
   ```

2. Edit `src/Purity.Api/appsettings.Development.json` and add your DSN:
   ```json
   {
     "Sentry": {
       "Dsn": "https://your-api-dsn@sentry.io/project-id",
       "Enabled": true
     }
   }
   ```

   This file is gitignored and won't be committed.

#### Option B: Environment Variables

Set the `Sentry__Dsn` environment variable:
```bash
# Windows PowerShell
$env:Sentry__Dsn="https://your-api-dsn@sentry.io/project-id"

# Linux/Mac
export Sentry__Dsn="https://your-api-dsn@sentry.io/project-id"
```

#### Option C: User Secrets (ASP.NET Core)

```bash
dotnet user-secrets set "Sentry:Dsn" "https://your-api-dsn@sentry.io/project-id" --project src/Purity.Api
dotnet user-secrets set "Sentry:Enabled" "true" --project src/Purity.Api
```

### 4. Configure the Frontend

**Important**: DSNs should not be committed to source control. Use one of these approaches:

#### Option A: Development Configuration File (Recommended for Local Development)

1. Copy the example file:
   ```bash
   cp src/Purity.Frontend/wwwroot/appsettings.Development.json.example src/Purity.Frontend/wwwroot/appsettings.Development.json
   ```

2. Edit `src/Purity.Frontend/wwwroot/appsettings.Development.json` and add your DSN:

   ```json
   {
     "Sentry": {
       "Dsn": "https://your-frontend-dsn@sentry.io/project-id",
       "Enabled": true
     }
   }
   ```

   This file is gitignored and won't be committed.

#### Option B: Environment Variables

For production deployments, use environment variables or your hosting platform's configuration system.

### 5. Environment-Specific Configuration

For production, use `appsettings.Production.json`:

**API** (`src/Purity.Api/appsettings.Production.json`):
```json
{
  "Sentry": {
    "Dsn": "https://your-production-api-dsn@sentry.io/project-id",
    "Enabled": true
  }
}
```

**Frontend**: For Blazor WASM, you can use different `appsettings.{Environment}.json` files or set the DSN via environment variables.

## How It Works

### API Integration

- **Automatic Exception Capture**: All unhandled exceptions are automatically sent to Sentry
- **Request Tracing**: Performance data for API endpoints
- **Logging Integration**: Logs with level `Error` or higher are sent to Sentry
- **Custom Context**: Errors include request details, user info (when auth is enabled), and environment
- **Manual Capture**: The scan endpoint manually captures analyzer failures with context (failure code, reason)

### Frontend Integration

- **Unhandled Exceptions**: JavaScript and C# exceptions in Blazor are captured
- **Performance Monitoring**: Tracks page load times and user interactions
- **User Context**: Associates errors with user sessions (when auth is enabled)
- **Manual Capture**: The scan operation captures exceptions with context (operation type, repository path)
- **Source Maps**: Supports source map uploads for better stack traces

## Manual Error Reporting

You can also manually capture errors:

### In API Code

```csharp
using Sentry;

try
{
    // Your code
}
catch (Exception ex)
{
    SentrySdk.CaptureException(ex);
    // Handle error
}
```

### In Frontend Code

```csharp
@using Sentry

try
{
    // Your code
}
catch (Exception ex)
{
    SentrySdk.CaptureException(ex, scope =>
    {
        scope.SetTag("operation", "my_operation");
        scope.SetExtra("context", "additional_info");
    });
    // Handle error
}
```

**Note**: The frontend already captures exceptions in `Home.razor` when scan operations fail.

## Configuration Options

### Sample Rates

- **Development**: `TracesSampleRate = 1.0` (100% of transactions)
- **Production**: `TracesSampleRate = 0.1` (10% of transactions)

Adjust these in `Program.cs` based on your needs and Sentry quota.

### Environment Tags

Sentry automatically tags events with:
- `application`: "purity-api" or "purity-frontend"
- `environment`: Development, Staging, Production

### Custom Context

You can add custom context before sending events:

```csharp
SentrySdk.ConfigureScope(scope =>
{
    scope.SetTag("repository", repositoryPath);
    scope.SetExtra("scanId", scanId);
});
```

## Testing Sentry

### Test Exception in API

Add a test endpoint (remove in production):

```csharp
app.MapGet("/test-sentry", () =>
{
    throw new Exception("Test Sentry integration");
});
```

### Test Exception in Frontend

Add a test button:

```razor
<button @onclick="TestSentry">Test Sentry</button>

@code {
    private void TestSentry()
    {
        throw new Exception("Test Sentry integration");
    }
}
```

## Disabling Sentry

To disable Sentry:

1. **Option 1**: Leave `Dsn` empty in `appsettings.json`
2. **Option 2**: Set `Enabled: false` in configuration
3. **Option 3**: Remove the Sentry configuration code (not recommended)

The code is designed to gracefully handle missing DSNs - if no DSN is provided, Sentry simply won't initialize.

## Best Practices

1. **Don't commit DSNs to source control**: Use environment variables or secrets management
2. **Use different projects**: Separate projects for API and Frontend
3. **Set up alerts**: Configure Sentry to notify you of critical errors
4. **Review regularly**: Check Sentry dashboard for error trends
5. **Filter noise**: Configure filters to ignore known non-critical errors
6. **Release tracking**: Tag releases to track which version introduced errors

## Security Considerations

- ✅ DSNs are safe to expose in client-side code (they're public)
- ✅ Sentry automatically filters sensitive data (passwords, tokens)
- ✅ You can configure additional data scrubbing rules
- ✅ Use different DSNs for different environments

## Troubleshooting

**Sentry not capturing errors?**
- Check that DSN is correctly configured
- Verify network connectivity to Sentry
- Check browser console for Sentry initialization errors
- Enable `Debug = true` in development to see Sentry logs

**Too many events?**
- Reduce `TracesSampleRate` in production
- Configure filters in Sentry dashboard
- Adjust log levels

**Missing stack traces?**
- Upload source maps for Blazor WASM
- Ensure PDB files are included for API
- Check Sentry's source map configuration

## Resources

- [Sentry .NET Documentation](https://docs.sentry.io/platforms/dotnet/)
- [Sentry Blazor Documentation](https://docs.sentry.io/platforms/dotnet/guides/blazor/)
- [Sentry Dashboard](https://sentry.io/)

