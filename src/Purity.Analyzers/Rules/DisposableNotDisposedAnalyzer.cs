using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Diagnostics;

namespace Purity.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DisposableNotDisposedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule =
        new(
            id: DiagnosticIds.DisposableNotDisposed,
            title: "IDisposable should be disposed",
            messageFormat: "IDisposable instance '{0}' is not disposed. Use 'using' statement or ensure disposal in finally block.",
            category: DiagnosticCategories.Purity,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "IDisposable instances must be disposed to prevent resource leaks (file handles, database connections, etc.). Use 'using' statements or ensure proper disposal.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax objectCreation)
        {
            return;
        }

        // Check if the type implements IDisposable
        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
        if (typeInfo.Type is not INamedTypeSymbol typeSymbol)
        {
            return;
        }

        if (!ImplementsIDisposable(typeSymbol))
        {
            return;
        }

        // Skip if it's in a using statement
        if (IsInUsingStatement(objectCreation))
        {
            return;
        }

        // Skip if assigned to a variable that's disposed
        if (IsAssignedAndDisposed(objectCreation, context))
        {
            return;
        }

        // Skip if returned from method (caller responsible)
        if (IsReturnedFromMethod(objectCreation))
        {
            return;
        }

        // Skip common patterns that are safe (e.g., new StringBuilder(), new List<T>())
        if (IsCommonSafePattern(typeSymbol))
        {
            return;
        }

        var typeName = typeSymbol.Name;
        var diagnostic = Diagnostic.Create(Rule, objectCreation.GetLocation(), typeName);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool ImplementsIDisposable(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == "System.IDisposable"))
        {
            return true;
        }

        // Check base types
        var baseType = typeSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.AllInterfaces.Any(i => i.ToDisplayString() == "System.IDisposable"))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool IsInUsingStatement(SyntaxNode node)
    {
        return node.Ancestors().Any(ancestor =>
            ancestor.IsKind(SyntaxKind.UsingStatement) ||
            (ancestor is LocalDeclarationStatementSyntax localDecl &&
             localDecl.UsingKeyword != default));
    }

    private static bool IsAssignedAndDisposed(ObjectCreationExpressionSyntax objectCreation, SyntaxNodeAnalysisContext context)
    {
        // Check if parent is an assignment and if the variable is disposed later
        var parent = objectCreation.Parent;
        if (parent is not EqualsValueClauseSyntax equalsValue)
        {
            return false;
        }

        var variableDeclarator = equalsValue.Parent as VariableDeclaratorSyntax;
        if (variableDeclarator == null)
        {
            return false;
        }

        var variableName = variableDeclarator.Identifier.Text;
        var methodDeclaration = objectCreation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration == null)
        {
            return false;
        }

        // Check if variable is disposed in the method
        return methodDeclaration.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation =>
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.Text == "Dispose" &&
                    memberAccess.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.Text == variableName)
                {
                    return true;
                }
                return false;
            });
    }

    private static bool IsReturnedFromMethod(SyntaxNode node)
    {
        return node.Ancestors().Any(ancestor =>
            ancestor.IsKind(SyntaxKind.ReturnStatement));
    }

    private static bool IsCommonSafePattern(INamedTypeSymbol typeSymbol)
    {
        // These are commonly used IDisposable types that are typically safe
        // (though technically they implement IDisposable, they don't hold critical resources)
        var typeName = typeSymbol.Name;
        var fullName = typeSymbol.ToDisplayString();

        return typeName switch
        {
            "StringBuilder" => true,
            "MemoryStream" when fullName.Contains("System.IO") => false, // MemoryStream should be disposed
            "StringWriter" => false, // Should be disposed
            "StringReader" => false, // Should be disposed
            _ => false
        };
    }
}

