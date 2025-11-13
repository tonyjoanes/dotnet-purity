using Microsoft.CodeAnalysis;

namespace Purity.Engine.Domain;

public sealed record AnalyzerDiagnostic(
    string Id,
    string Title,
    string Message,
    DiagnosticSeverity Severity,
    string FilePath,
    LocationSpan Span);


