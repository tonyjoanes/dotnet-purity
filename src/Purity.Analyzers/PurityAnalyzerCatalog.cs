using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Rules;

namespace Purity.Analyzers;

public static class PurityAnalyzerCatalog
{
    public static ImmutableArray<DiagnosticAnalyzer> CreateDefaultSet() =>
        ImmutableArray.Create<DiagnosticAnalyzer>(
            new AwaitInsideLoopAnalyzer(),
            new SyncOverAsyncAnalyzer(),
            new StaticCollectionLeakAnalyzer(),
            new DisposableNotDisposedAnalyzer(),
            new EventHandlerLeakAnalyzer(),
            new MultipleEnumerationAnalyzer(),
            new NullReferenceDereferenceAnalyzer(),
            new SwallowedExceptionAnalyzer(),
            new StringConcatenationInLoopAnalyzer(),
            new InsecureAlgorithmAnalyzer(),
            new ThreadSafetyViolationAnalyzer());
}


