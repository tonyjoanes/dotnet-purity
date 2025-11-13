using Purity.Engine.Domain;
using System.Linq;

namespace Purity.Api.Contracts;

public sealed record ScanResponseDto(IReadOnlyList<AnalyzerDiagnosticDto> Diagnostics)
{
    public static ScanResponseDto From(AnalyzerReport report) =>
        new(report.Diagnostics.Select(AnalyzerDiagnosticDto.From).ToList());
}


