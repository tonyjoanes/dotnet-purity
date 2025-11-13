using Purity.Engine.Domain;

namespace Purity.Api.Contracts;

public sealed record AnalyzerDiagnosticDto(
    string Id,
    string Title,
    string Message,
    string Severity,
    string FilePath,
    LocationSpanDto Location)
{
    public static AnalyzerDiagnosticDto From(AnalyzerDiagnostic diagnostic) =>
        new(
            diagnostic.Id,
            diagnostic.Title,
            diagnostic.Message,
            diagnostic.Severity.ToString(),
            diagnostic.FilePath,
            LocationSpanDto.From(diagnostic.Span));
}


