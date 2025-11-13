using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Diagnostics;

namespace Purity.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StaticCollectionLeakAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule =
        new(
            id: DiagnosticIds.StaticCollectionLeak,
            title: "Avoid static mutable collections",
            messageFormat: "Static collection '{0}' can leak shared mutable state. Prefer immutable or scoped instances.",
            category: DiagnosticCategories.Purity,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Static collections introduce global mutable state that breaks purity and makes reasoning about code difficult. Prefer immutable structures or inject scoped dependencies.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IFieldSymbol fieldSymbol)
        {
            return;
        }

        if (!fieldSymbol.IsStatic || fieldSymbol.IsConst)
        {
            return;
        }

        if (fieldSymbol.Type.SpecialType == SpecialType.System_String)
        {
            return;
        }

        if (fieldSymbol.Type is not INamedTypeSymbol namedType)
        {
            return;
        }

        if (!IsCollectionLike(namedType))
        {
            return;
        }

        var location = fieldSymbol.Locations.FirstOrDefault();
        if (location is null || !location.IsInSource)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, location, fieldSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsCollectionLike(INamedTypeSymbol symbol)
    {
        if (symbol.AllInterfaces.Any(IsEnumerableInterface))
        {
            return true;
        }

        if (symbol.OriginalDefinition.SpecialType == SpecialType.System_Array)
        {
            return true;
        }

        if (IsKnownImmutableCollection(symbol))
        {
            return false;
        }

        return IsMutableCollection(symbol);
    }

    private static bool IsMutableCollection(INamedTypeSymbol symbol)
    {
        var metadataName = symbol.ConstructedFrom.ToDisplayString();
        return metadataName switch
        {
            "System.Collections.Generic.List<T>" => true,
            "System.Collections.Generic.Dictionary<TKey, TValue>" => true,
            "System.Collections.Generic.HashSet<T>" => true,
            "System.Collections.Generic.Queue<T>" => true,
            "System.Collections.Generic.Stack<T>" => true,
            "System.Collections.ObjectModel.Collection<T>" => true,
            "System.Collections.Generic.LinkedList<T>" => true,
            _ => symbol.ContainingNamespace?.ToDisplayString() == "System.Collections" && symbol.Name is "ArrayList" or "Hashtable"
        };
    }

    private static bool IsKnownImmutableCollection(INamedTypeSymbol symbol)
    {
        var constructed = symbol.ConstructedFrom.ToDisplayString();
        return constructed is
            "System.Collections.Immutable.ImmutableArray<T>"
            or "System.Collections.Immutable.ImmutableList<T>"
            or "System.Collections.Immutable.ImmutableHashSet<T>"
            or "System.Collections.Immutable.ImmutableDictionary<TKey, TValue>"
            or "System.Collections.Immutable.ImmutableQueue<T>"
            or "System.Collections.Immutable.ImmutableStack<T>"
            or "System.Collections.Immutable.ImmutableSortedSet<T>"
            or "System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>";
    }

    private static bool IsEnumerableInterface(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.SpecialType switch
        {
            SpecialType.System_Collections_IEnumerable => true,
            SpecialType.System_Collections_Generic_IEnumerable_T => true,
            _ => typeSymbol.OriginalDefinition.SpecialType switch
            {
                SpecialType.System_Collections_Generic_IEnumerable_T => true,
                _ => typeSymbol.ToDisplayString() switch
                {
                    "System.Collections.Generic.IReadOnlyCollection<T>" => true,
                    "System.Collections.Generic.IReadOnlyList<T>" => true,
                    _ => false
                }
            }
        };
    }
}


