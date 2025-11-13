# Purity Setup Guide

Complete setup instructions for the Purity project.

## Prerequisites

- .NET 8.0 SDK or later
- Git
- A GitHub account (for OAuth authentication)
- A Snyk account (optional, for security scanning)

## Initial Setup

### 1. Clone the Repository

```bash
git clone <your-purity-repo-url>
cd dotnet-purity
```

### 2. Restore Dependencies

```bash
dotnet restore Purity.sln
```

### 3. Build the Solution

```bash
dotnet build Purity.sln --configuration Release
```

## Project Structure

The solution consists of four main projects:

- **Purity.Frontend**: Blazor WebAssembly application for the UI, OAuth, and dashboard
- **Purity.Api**: .NET 8 Minimal API that serves as the API surface and orchestrator
- **Purity.Engine**: Core analyzer logic with Roslyn integration
- **Purity.Analyzers**: Roslyn analyzer rules as standalone libraries

Code is organized with clear boundaries: `Domain`, `Application`, `Infrastructure`, and `Presentation`.

## Configuration

### Development Settings

**Important**: Never commit DSNs, API keys, or other secrets to source control.

1. **For Local Development**: Copy the example configuration files and add your secrets:

```bash
# API
cp src/Purity.Api/appsettings.Development.json.example src/Purity.Api/appsettings.Development.json
# Edit appsettings.Development.json with your Sentry DSN and other secrets

# Frontend
cp src/Purity.Frontend/wwwroot/appsettings.Development.json.example src/Purity.Frontend/wwwroot/appsettings.Development.json
# Edit appsettings.Development.json with your Sentry DSN and GitHub OAuth Client ID
```

2. These `appsettings.Development.json` files are gitignored and won't be committed.

3. **Alternative Options**:
   - Use environment variables (recommended for production)
   - Use ASP.NET Core User Secrets: `dotnet user-secrets set "Sentry:Dsn" "your-dsn" --project src/Purity.Api`

4. See `docs/sentry-setup.md` for detailed Sentry configuration instructions.
5. See `docs/github-oauth-setup.md` for GitHub OAuth setup instructions.

## Functional Programming Practices

Purity enforces functional C# practices:

- Add the `language-ext` package to core projects:
  ```bash
  dotnet add package language-ext.Core
  ```
- Use `Option<T>`, `Either<L,R>`, and other functional types for data flow and error handling
- Prefer immutable types (records, readonly structs), pure functions, and ban nulls in business logic
- Separate side-effects from logic; use dependency injection for I/O/services

See `docs/style-guide.md` for detailed coding standards and guidelines.

## Analyzer Rules

The project includes three Roslyn analyzers:

- **PURITY001**: Detects await expressions inside loop constructs
- **PURITY002**: Flags synchronous waits on asynchronous operations
- **PURITY003**: Detects static mutable collection fields that leak shared state

All analyzers are independent modules for easy future extraction.

## Running the Application

### Development Mode

1. **Start the API**:
   ```bash
   cd src/Purity.Api
   dotnet run
   ```

2. **Start the Frontend** (in a separate terminal):
   ```bash
   cd src/Purity.Frontend
   dotnet run
   ```

3. Open your browser to the frontend URL (typically `https://localhost:5001` or `http://localhost:5000`)

### Using Launch Profiles

You can configure launch profiles in your IDE to launch both frontend and backend together.

## Testing

Run all tests (if any test projects exist):

```bash
dotnet test Purity.sln --configuration Release
```

## CI/CD Setup

See `docs/ci-setup.md` for information about:
- GitHub Actions CI workflows
- Snyk security scanning
- Dependabot configuration

## Best Practices

- Keep core analyzers, scoring, and PR logic in libraries, not `Api` or `Frontend`—ensuring later ease-of-extraction
- Separate integrations (Roslyn, GitHub, benchmarks) behind interfaces
- Document every architectural decision (as ADRs or in `/docs/design`)
- Regularly review with functional and modular guidelines in mind

## Troubleshooting

### Build Errors

- Ensure you have .NET 8.0 SDK installed: `dotnet --version`
- Restore dependencies: `dotnet restore Purity.sln`
- Clean and rebuild: `dotnet clean && dotnet build`

### Runtime Errors

- Verify your `appsettings.Development.json` files are configured correctly
- Check that required services (Sentry, GitHub OAuth) are properly configured
- Review logs for detailed error messages

## Additional Documentation

- [Architecture and Deployment](architecture-and-deployment.md)
- [CI/CD Setup](ci-setup.md)
- [GitHub OAuth Setup](github-oauth-setup.md)
- [Sentry Setup](sentry-setup.md)
- [Style Guide](style-guide.md)

