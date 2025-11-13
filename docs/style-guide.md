# Purity Style Guide

## Principles
- Favour pure functions, immutability, and expression-based code.
- Default to functional types from `language-ext` (`Option<T>`, `Either<L, R>`, `TryAsync<T>`).
- Ban `null` in business logic; represent absence with functional abstractions.
- Keep side-effects at the edges behind interfaces and dependency injection.

## Project Organization
- Code lives under `src/` with clear boundaries: `Domain`, `Application`, `Infrastructure`, `Presentation`.
- Documentation lives under `docs/`, including ADRs and design notes.
- Analyzers reside in `Purity.Analyzers`; shared logic goes in `Purity.Engine`.

## Naming & Structure
- Use `PascalCase` for types, records, classes, and public members.
- Use `camelCase` for locals and private fields (prefix private readonly fields with `_`).
- Name async methods with the `Async` suffix; avoid `Async` for synchronous APIs.
- Co-locate related records and their pure behaviours; separate side-effecting orchestration.

## Error Handling
- Prefer returning `Either<Error, TResult>` or `Validation` from core logic.
- Map external exceptions to functional error types at the boundary.
- Avoid throwing exceptions in pure code paths; use pattern matching for recovery.

## Testing & Validation
- Unit tests should target pure functions and analyzer rules deterministically.
- Use property-based tests where feasible for analyzer logic.
- Include minimal integration tests for API and frontend authentication flows.

## References
- [Microsoft C# Coding Conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [language-ext Documentation](https://github.com/louthy/language-ext)
- Internal ADRs (`docs/adr/`) for architectural decisions.


