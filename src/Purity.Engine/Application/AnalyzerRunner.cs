using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Engine.Domain;
using static LanguageExt.Prelude;

namespace Purity.Engine.Application;

public sealed class AnalyzerRunner : IAnalyzerRunner
{
    private readonly IAnalyzerCatalog _catalog;

    public AnalyzerRunner(IAnalyzerCatalog catalog)
    {
        _catalog = catalog;
    }

    public Task<Either<AnalyzerFailure, AnalyzerReport>> RunAsync(ScanRequest request, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(request.RepositoryPath))
        {
            return Task.FromResult<Either<AnalyzerFailure, AnalyzerReport>>(
                Left<AnalyzerFailure, AnalyzerReport>(
                    new AnalyzerFailure(
                        "ENGINE-REPO-NOT-FOUND",
                        $"Repository path '{request.RepositoryPath}' does not exist.")));
        }

        return ExecuteAsync();

        async Task<Either<AnalyzerFailure, AnalyzerReport>> ExecuteAsync()
        {
            try
            {
                var analyzers = _catalog.GetAnalyzers();
                if (analyzers.IsDefaultOrEmpty)
                {
                    return Right<AnalyzerFailure, AnalyzerReport>(AnalyzerReport.Empty);
                }

                var sourceFiles = Directory
                    .EnumerateFiles(request.RepositoryPath, "*.cs", SearchOption.AllDirectories)
                    .ToArray();

                if (sourceFiles.Length == 0)
                {
                    return Right<AnalyzerFailure, AnalyzerReport>(AnalyzerReport.Empty);
                }

                var syntaxTrees = await Task.WhenAll(
                    sourceFiles.Select(file => ParseSyntaxTreeAsync(file, cancellationToken)));

                var compilation = CreateCompilation(syntaxTrees);
                var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
                var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken);

                var issues = diagnostics
                    .Where(diagnostic => diagnostic.Location.IsInSource)
                    .Select(ToAnalyzerDiagnostic)
                    .Where(diagnostic => diagnostic != null)
                    .Cast<AnalyzerDiagnostic>()
                    .ToImmutableArray();

                return Right<AnalyzerFailure, AnalyzerReport>(new AnalyzerReport(issues));
            }
            catch (Exception exception)
            {
                return Left<AnalyzerFailure, AnalyzerReport>(MapFailure(exception));
            }
        }
    }

    private static AnalyzerFailure MapFailure(Exception exception) =>
        new("ENGINE-UNEXPECTED", "Unexpected failure while running analyzers.", exception);

    private static async Task<SyntaxTree> ParseSyntaxTreeAsync(string file, CancellationToken cancellationToken)
    {
        var sourceText = await File.ReadAllTextAsync(file, cancellationToken);
        return CSharpSyntaxTree.ParseText(sourceText, path: file);
    }

    private static CSharpCompilation CreateCompilation(IEnumerable<SyntaxTree> syntaxTrees)
    {
        var references = CreateMetadataReferences();
        return CSharpCompilation.Create(
            assemblyName: "PurityAnalyzerWorkspace",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static ImmutableArray<MetadataReference> CreateMetadataReferences()
    {
        var assemblies = new[]
        {
            typeof(object).Assembly.Location,
            typeof(Enumerable).Assembly.Location,
            typeof(Task).Assembly.Location,
            typeof(Unit).Assembly.Location
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        var builder = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (var assembly in assemblies)
        {
            builder.Add(MetadataReference.CreateFromFile(assembly));
        }

        return builder.ToImmutable();
    }

    private static AnalyzerDiagnostic? ToAnalyzerDiagnostic(Diagnostic diagnostic)
    {
        try
        {
            if (!diagnostic.Location.IsInSource)
            {
                return null;
            }

            var mappedSpan = diagnostic.Location.GetMappedLineSpan();
            if (mappedSpan.Path == null)
            {
                return null;
            }

            var span = new LocationSpan(
                mappedSpan.StartLinePosition.Line + 1,
                mappedSpan.StartLinePosition.Character + 1,
                mappedSpan.EndLinePosition.Line + 1,
                mappedSpan.EndLinePosition.Character + 1);

            var title = diagnostic.Descriptor?.Title?.ToString() ?? diagnostic.Id;
            var message = diagnostic.GetMessage() ?? string.Empty;

            return new AnalyzerDiagnostic(
                diagnostic.Id,
                title,
                message,
                diagnostic.Severity,
                mappedSpan.Path,
                span);
        }
        catch
        {
            // If we can't convert a diagnostic, skip it rather than failing the entire scan
            // This can happen with malformed diagnostics or edge cases in Roslyn
            return null;
        }
    }
}


