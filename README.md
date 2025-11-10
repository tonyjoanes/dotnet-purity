### Purity Project: Complete Initial Setup—Functional, Modular, Best Practices

#### 1. Clone Your Purity Repository

- Open Cursor IDE and either use the Git panel or terminal to clone your repo:
  ```
  git clone <your-purity-repo-url>
  cd purity
  ```

#### 2. Initialize Solution and Modular Projects

- In Cursor IDE, create your solution and the following projects to maximize modularity:
  - `Purity.Frontend`: Blazor WebAssembly—UI, OAuth, dashboard
  - `Purity.Api`: .NET 8 Minimal API—API surface, orchestrator
  - `Purity.Engine`: Core analyzer logic, Roslyn integration
  - `Purity.Analyzers`: Roslyn rules as standalone libraries
- Organize code in clear boundaries: `Domain`, `Application`, `Infrastructure`, `Presentation`

#### 3. Enforce Functional C# Practices

- Add the `language-ext` package to core projects:
  ```
  dotnet add package language-ext.Core
  ```
- Use `Option<T>`, `Either<L,R>`, and other functional types for data flow and error handling.
- Prefer immutable types (records, readonly structs), pure functions, and ban nulls in business logic.
- Separate side-effects from logic; use dependency injection for I/O/services.

#### 4. Enforce Coding Standards and Guidelines

- Add an `.editorconfig` and agree on formatting tools.
- Reference a C# best-practices guide (e.g., Microsoft, Clean Code C#) in your repo’s `/docs` folder.
- Adopt naming conventions and consistent use of access modifiers, immutability, and async patterns.

#### 5. Scaffold Authentication and Core Architecture

- In `Purity.Frontend`, implement GitHub OAuth using:
  ```
  dotnet add package Microsoft.AspNetCore.Components.WebAssembly.Authentication
  ```
- Scaffold login UI and wire authentication state to API calls.
- In `Purity.Api`, set up Minimal API boilerplate with secure endpoints (`/scan`, `/results`), CORS, and token validation.

#### 6. Set Up Analyzer Engine & Rules

- In `Purity.Engine`, reference Roslyn packages:
  ```
  dotnet add package Microsoft.CodeAnalysis.CSharp
  ```
- Scaffold analyzer runner against a local repo clone; inject analyzers via interfaces.
- In `Purity.Analyzers`, create functional analyzers as `static` rule classes:
  - `PURITY001`: Query await inside loop
  - `PURITY002`: Sync-over-async usage
  - `PURITY003`: Static collection leak
- Ensure all analyzers are independent modules for easy future extraction.

#### 7. Connect Components and Demonstrate End-to-End Flow

- Make authenticated HTTP calls from Blazor frontend to Minimal API backend.
- Pass OAuth token securely.
- Return analyzer results (using functional wrappers, e.g., `Option`, `Either`) and wire up UI display.

#### 8. Local Testing and First Commit

- In Cursor IDE, set up profiles to launch both frontend and backend together.
- Test logging in with GitHub, running a dummy scan, and returning results.
- Commit your setup:
  ```
  git add .
  git commit -m "Initial functional setup: modular solution, OAuth, API, analyzer skeletons, coding guidelines"
  git push
  ```

#### 9. Best-Practice Modularization Tips

- Keep core analyzers, scoring, and PR logic in libraries, not `Api` or `Frontend`—ensuring later ease-of-extraction.[1]
- Separate integrations (Roslyn, GitHub, benchmarks) behind interfaces.
- Document every architectural decision (as ADRs or in `/docs/design`).
- Regularly review with functional and modular guidelines in mind.

#### 10. Documentation and Communication

- Add README notes outlining your commitment to:
  - Functional programming standards
  - Modular, extractable architecture
  - Usage of `language-ext`
  - Coding style guides and review principles
- Encourage feedback and flag deviations in pull requests.
