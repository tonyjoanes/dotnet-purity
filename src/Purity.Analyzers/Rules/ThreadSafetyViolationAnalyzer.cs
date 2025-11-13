using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Diagnostics;

namespace Purity.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ThreadSafetyViolationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ThreadSafetyViolation,
        title: "Potential thread safety violation",
        messageFormat: "Shared mutable state '{0}' is accessed without synchronization. Use locks, concurrent collections, or immutable structures.",
        category: DiagnosticCategories.Purity,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Updating shared mutable state without proper synchronization can cause race conditions, data corruption, and subtle concurrency bugs. Use thread-safe patterns."
    );

    private static readonly ImmutableHashSet<string> ThreadSafeCollectionTypes =
        ImmutableHashSet.Create(
            "ConcurrentDictionary",
            "ConcurrentQueue",
            "ConcurrentStack",
            "ConcurrentBag",
            "ImmutableList",
            "ImmutableDictionary",
            "ImmutableHashSet",
            "ImmutableArray"
        );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        context.RegisterSyntaxNodeAction(
            AnalyzeMemberAccess,
            SyntaxKind.SimpleMemberAccessExpression
        );
    }

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AssignmentExpressionSyntax assignment)
        {
            return;
        }

        // Check if we're assigning to a field (not a local variable)
        if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(
            memberAccess,
            context.CancellationToken
        );
        if (symbolInfo.Symbol is not IFieldSymbol fieldSymbol)
        {
            return;
        }

        // Check if it's a static or instance field (shared state)
        if (!fieldSymbol.IsStatic && !IsSharedInstanceField(fieldSymbol, context))
        {
            return;
        }

        // Skip if it's a readonly field (immutable)
        if (fieldSymbol.IsReadOnly)
        {
            return;
        }

        // Skip if it's a thread-safe collection
        if (IsThreadSafeCollection(fieldSymbol.Type))
        {
            return;
        }

        // Check if we're inside a lock or other synchronization
        if (IsInsideSynchronization(assignment))
        {
            return;
        }

        var fieldName = fieldSymbol.Name;
        var diagnostic = Diagnostic.Create(Rule, assignment.GetLocation(), fieldName);
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        // Check for method calls that modify collections
        var methodName = memberAccess.Name.Identifier.Text;
        var mutatingMethods = new[]
        {
            "Add",
            "Remove",
            "Clear",
            "Insert",
            "RemoveAt",
            "AddRange",
            "RemoveRange",
            "InsertRange",
            "Pop",
            "Push",
            "Enqueue",
            "Dequeue",
        };

        if (!mutatingMethods.Contains(methodName))
        {
            return;
        }

        var expression = memberAccess.Expression;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken);

        if (symbolInfo.Symbol is IFieldSymbol fieldSymbol)
        {
            // Check if it's shared state
            if (fieldSymbol.IsStatic || IsSharedInstanceField(fieldSymbol, context))
            {
                // Skip if thread-safe collection
                if (!IsThreadSafeCollection(fieldSymbol.Type))
                {
                    // Check if inside synchronization
                    if (!IsInsideSynchronization(memberAccess))
                    {
                        var fieldName = fieldSymbol.Name;
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            memberAccess.GetLocation(),
                            fieldName
                        );
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }

    private static bool IsSharedInstanceField(
        IFieldSymbol fieldSymbol,
        SyntaxNodeAnalysisContext context
    )
    {
        // Check if the containing type could be accessed from multiple threads
        // This is a heuristic - instance fields of non-sealed classes or public fields
        // are more likely to be shared
        var containingType = fieldSymbol.ContainingType;

        // Public or protected fields are more likely shared
        if (
            fieldSymbol.DeclaredAccessibility == Accessibility.Public
            || fieldSymbol.DeclaredAccessibility == Accessibility.Protected
        )
        {
            return true;
        }

        // Fields of singleton or service classes are likely shared
        // This is a simplified check
        return false;
    }

    private static bool IsThreadSafeCollection(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        var typeName = namedType.Name;
        return ThreadSafeCollectionTypes.Contains(typeName)
            || namedType.ToDisplayString().Contains("System.Collections.Concurrent")
            || namedType.ToDisplayString().Contains("System.Collections.Immutable");
    }

    private static bool IsInsideSynchronization(SyntaxNode node)
    {
        // Check if we're inside a lock statement
        if (node.Ancestors().Any(ancestor => ancestor.IsKind(SyntaxKind.LockStatement)))
        {
            return true;
        }

        // Check for Monitor.Enter/Exit
        var methodCalls = node.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .ToList();

        foreach (var call in methodCalls)
        {
            if (call.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;
                var expression = memberAccess.Expression.ToString();

                if (
                    (methodName == "Enter" || methodName == "Exit" || methodName == "TryEnter")
                    && expression.Contains("Monitor")
                )
                {
                    return true;
                }

                // Check for SemaphoreSlim, Mutex, etc.
                if (methodName == "Wait" || methodName == "WaitAsync" || methodName == "Release")
                {
                    if (expression.Contains("Semaphore") || expression.Contains("Mutex"))
                    {
                        return true;
                    }
                }
            }
        }

        // Check for async synchronization (SemaphoreSlim.WaitAsync, etc.)
        // This is a simplified check
        return false;
    }
}