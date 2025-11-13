using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Purity.Engine.Application;

public interface IAnalyzerCatalog
{
    ImmutableArray<DiagnosticAnalyzer> GetAnalyzers();
}


