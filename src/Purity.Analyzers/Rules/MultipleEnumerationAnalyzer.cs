using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Diagnostics;

namespace Purity.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MultipleEnumerationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule =
        new(
            id: DiagnosticIds.MultipleEnumeration,
            title: "IEnumerable may be enumerated multiple times",
            messageFormat: "IEnumerable '{0}' is potentially enumerated multiple times. Materialize with ToList() or ToArray() if the sequence should be reused.",
            category: DiagnosticCategories.Purity,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Enumerating an IEnumerable multiple times can cause performance issues and unexpected behavior if the source is not idempotent. Materialize the sequence if it needs to be used multiple times.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDeclaration)
        {
            return;
        }

        var methodBody = methodDeclaration.Body ?? methodDeclaration.ExpressionBody?.Expression as SyntaxNode;
        if (methodBody == null)
        {
            return;
        }

        // Find all variable declarations and assignments that create IEnumerable
        var variableDeclarations = methodBody.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .ToList();

        foreach (var variableDeclarator in variableDeclarations)
        {
            var variableName = variableDeclarator.Identifier.Text;
            var initializer = variableDeclarator.Initializer?.Value;

            if (initializer == null)
            {
                continue;
            }

            // Check if the initializer creates an IEnumerable
            var typeInfo = context.SemanticModel.GetTypeInfo(initializer, context.CancellationToken);
            if (!IsIEnumerable(typeInfo.Type))
            {
                continue;
            }

            // Skip if already materialized (ToList, ToArray, etc.)
            if (IsMaterialized(initializer))
            {
                continue;
            }

            // Count how many times this variable is used in enumeration contexts
            var enumerationCount = CountEnumerations(variableName, methodBody);

            if (enumerationCount > 1)
            {
                var diagnostic = Diagnostic.Create(Rule, variableDeclarator.GetLocation(), variableName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsIEnumerable(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
        {
            return false;
        }

        // Check if it implements IEnumerable or IEnumerable<T>
        if (typeSymbol.AllInterfaces.Any(i =>
            i.ToDisplayString() == "System.Collections.IEnumerable" ||
            i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>"))
        {
            return true;
        }

        // Check if it's an array
        if (typeSymbol is IArrayTypeSymbol)
        {
            return true;
        }

        return false;
    }

    private static bool IsMaterialized(SyntaxNode expression)
    {
        // Check if the expression calls ToList(), ToArray(), or similar materialization methods
        if (expression is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;
                return methodName is "ToList" or "ToArray" or "ToHashSet" or "ToDictionary";
            }
        }

        return false;
    }

    private static int CountEnumerations(string variableName, SyntaxNode methodBody)
    {
        var count = 0;

        // Count foreach loops
        count += methodBody.DescendantNodes()
            .OfType<ForEachStatementSyntax>()
            .Count(forEach => forEach.Expression is IdentifierNameSyntax identifier &&
                             identifier.Identifier.Text == variableName);

        // Count LINQ operations that enumerate
        count += methodBody.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Count(invocation =>
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var enumeratingMethods = new[]
                    {
                        "Count", "Any", "First", "FirstOrDefault", "Last", "LastOrDefault",
                        "Single", "SingleOrDefault", "ElementAt", "ElementAtOrDefault",
                        "ToList", "ToArray", "ToDictionary", "ToHashSet", "ToLookup",
                        "Sum", "Average", "Min", "Max", "Aggregate"
                    };

                    if (enumeratingMethods.Contains(memberAccess.Name.Identifier.Text))
                    {
                        // Check if the receiver is our variable
                        if (memberAccess.Expression is IdentifierNameSyntax identifier &&
                            identifier.Identifier.Text == variableName)
                        {
                            return true;
                        }

                        // Check for chained calls like variable.Where(...).Count()
                        var receiver = memberAccess.Expression;
                        while (receiver is MemberAccessExpressionSyntax chainedAccess)
                        {
                            if (chainedAccess.Expression is IdentifierNameSyntax id &&
                                id.Identifier.Text == variableName)
                            {
                                return true;
                            }
                            receiver = chainedAccess.Expression;
                        }
                    }
                }

                return false;
            });

        // Count direct usage in method calls that might enumerate
        count += methodBody.DescendantNodes()
            .OfType<ArgumentSyntax>()
            .Count(argument =>
            {
                if (argument.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == variableName)
                {
                    // Check if the parameter type is IEnumerable
                    // This is a simplified check - in practice, you'd need semantic analysis
                    return true; // Conservative: assume it might enumerate
                }
                return false;
            });

        return count;
    }
}

