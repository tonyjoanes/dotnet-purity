using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers;
using Purity.Engine.Application;

namespace Purity.Api.Infrastructure;

public sealed class DefaultAnalyzerCatalog : IAnalyzerCatalog
{
    public ImmutableArray<DiagnosticAnalyzer> GetAnalyzers() =>
        PurityAnalyzerCatalog.CreateDefaultSet();
}


