using System.Collections.Immutable;

namespace Purity.Engine.Domain;

public sealed record AnalyzerReport(ImmutableArray<AnalyzerDiagnostic> Diagnostics)
{
    public static AnalyzerReport Empty { get; } = new(ImmutableArray<AnalyzerDiagnostic>.Empty);
}


