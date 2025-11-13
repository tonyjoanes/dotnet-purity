# Purity

[![CI](https://github.com/tonyjoanes/dotnet-purity/actions/workflows/ci.yml/badge.svg)](https://github.com/tonyjoanes/dotnet-purity/actions/workflows/ci.yml)
[![Known Vulnerabilities](https://snyk.io/test/github/tonyjoanes/dotnet-purity/badge.svg)](https://snyk.io/test/github/tonyjoanes/dotnet-purity)

**Purity** is a code analysis tool that scans C# repositories to detect code quality issues and enforce functional programming best practices. Built with .NET 8, it uses Roslyn analyzers to identify common anti-patterns that can lead to performance issues, deadlocks, and maintainability problems.

## Features

- 🔍 **Static Code Analysis**: Scans entire C# repositories using Roslyn analyzers
- 🎯 **Functional Programming Rules**: Enforces functional programming best practices
- 🌐 **Web-Based Interface**: Blazor WebAssembly frontend for easy interaction
- 🔐 **GitHub OAuth**: Secure authentication via GitHub
- 📊 **Detailed Diagnostics**: Provides precise location and severity information for each issue

## Analyzer Rules

Purity includes three built-in analyzers:

- **PURITY001** - **Await Inside Loop**: Detects await expressions inside loop constructs that can serialize asynchronous work
- **PURITY002** - **Sync Over Async**: Flags synchronous waits on asynchronous operations (e.g., `.Result`, `.Wait()`, `GetAwaiter().GetResult()`)
- **PURITY003** - **Static Collection Leak**: Detects static mutable collection fields that leak shared state

## Architecture

Purity is built with a modular, functional architecture:

- **Purity.Frontend**: Blazor WebAssembly application providing the user interface
- **Purity.Api**: .NET 8 Minimal API serving as the backend orchestrator
- **Purity.Engine**: Core analyzer logic with Roslyn integration
- **Purity.Analyzers**: Roslyn analyzer rules as standalone, extractable libraries

The codebase follows functional programming principles using `language-ext` for:
- `Option<T>` for nullable values
- `Either<L, R>` for error handling
- Immutable types and pure functions
- Separation of side-effects from business logic

## Quick Start

```bash
# Clone the repository
git clone https://github.com/tonyjoanes/dotnet-purity.git
cd dotnet-purity

# Restore dependencies
dotnet restore Purity.sln

# Build the solution
dotnet build Purity.sln --configuration Release

# Run the API
cd src/Purity.Api
dotnet run

# Run the Frontend (in a separate terminal)
cd src/Purity.Frontend
dotnet run
```

For detailed setup instructions, see the [Setup Guide](docs/setup-guide.md).

## Documentation

- [Setup Guide](docs/setup-guide.md) - Complete installation and configuration instructions
- [Architecture and Deployment](docs/architecture-and-deployment.md) - System architecture and deployment details
- [CI/CD Setup](docs/ci-setup.md) - GitHub Actions, Snyk, and Dependabot configuration
- [Style Guide](docs/style-guide.md) - Coding standards and functional programming practices
- [GitHub OAuth Setup](docs/github-oauth-setup.md) - Authentication configuration
- [Sentry Setup](docs/sentry-setup.md) - Error tracking and monitoring setup

## Requirements

- .NET 8.0 SDK or later
- Git
- GitHub account (for OAuth authentication)
- Snyk account (optional, for security scanning in CI)

## Contributing

Purity follows functional programming principles and modular architecture. When contributing:

- Follow the [Style Guide](docs/style-guide.md)
- Use functional types from `language-ext` (`Option<T>`, `Either<L, R>`)
- Keep analyzers as independent, extractable modules
- Document architectural decisions
- Write tests for analyzer rules

## License

[Add your license here]

## Acknowledgments

Built with:
- [.NET 8](https://dotnet.microsoft.com/)
- [Roslyn](https://github.com/dotnet/roslyn) - .NET Compiler Platform
- [language-ext](https://github.com/louthy/language-ext) - Functional programming library for C#
- [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) - Web UI framework
