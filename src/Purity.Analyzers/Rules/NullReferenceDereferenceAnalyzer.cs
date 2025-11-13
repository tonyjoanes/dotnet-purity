using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Diagnostics;

namespace Purity.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NullReferenceDereferenceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule =
        new(
            id: DiagnosticIds.NullReferenceDereference,
            title: "Potential null reference dereference",
            messageFormat: "Dereferencing '{0}' which may be null. Add null check or use null-conditional operator.",
            category: DiagnosticCategories.Purity,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Dereferencing potentially null values without null checks is the most common cause of NullReferenceException in production. Use null checks or null-conditional operators (?.) to prevent crashes.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        // Skip if already using null-conditional operator (ConditionalAccessExpression)
        if (memberAccess.Parent is ConditionalAccessExpressionSyntax)
        {
            return;
        }

        var expression = memberAccess.Expression;
        if (expression == null)
        {
            return;
        }

        // Skip static member access (static types/members are never null)
        var symbolInfo = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken);
        if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol && typeSymbol.IsStatic)
        {
            return;
        }

        // Skip well-known framework types that are never null
        if (IsWellKnownNonNullType(expression, context))
        {
            return;
        }

        // Skip syntax node properties - they're guaranteed to exist
        if (IsSyntaxNodeProperty(expression))
        {
            return;
        }

        // Check if the expression might be null
        if (IsPotentiallyNull(expression, context))
        {
            var name = symbolInfo.Symbol?.Name ?? expression.ToString();
            var diagnostic = Diagnostic.Create(Rule, memberAccess.GetLocation(), name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeElementAccess(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ElementAccessExpressionSyntax elementAccess)
        {
            return;
        }

        var expression = elementAccess.Expression;
        if (expression == null)
        {
            return;
        }

        // Check if the expression might be null
        if (IsPotentiallyNull(expression, context))
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken);
            var name = symbolInfo.Symbol?.Name ?? expression.ToString();
            var diagnostic = Diagnostic.Create(Rule, elementAccess.GetLocation(), name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        // Check if we're calling a method on a potentially null expression
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            // Skip if using null-conditional operator
            if (memberAccess.Parent is ConditionalAccessExpressionSyntax)
            {
                return;
            }

            var expression = memberAccess.Expression;
            if (expression != null && IsPotentiallyNull(expression, context))
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken);
                var name = symbolInfo.Symbol?.Name ?? expression.ToString();
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsPotentiallyNull(SyntaxNode expression, SyntaxNodeAnalysisContext context)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken);
        var type = typeInfo.Type;

        if (type == null)
        {
            return false;
        }

        // Skip static types - they're never null
        if (type.IsStatic)
        {
            return false;
        }

        // Skip value types (except nullable value types)
        if (type.IsValueType && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
        {
            return false;
        }

        // Check if it's a nullable value type
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return !HasNullCheck(expression, context);
        }

        // Only flag if explicitly annotated as nullable
        if (type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return !HasNullCheck(expression, context);
        }

        // Check for local variables/parameters with nullable annotation
        if (expression is IdentifierNameSyntax identifier)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol;
            
            if (symbol is ILocalSymbol local)
            {
                // Only flag if explicitly nullable
                if (local.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    return !HasNullCheck(expression, context);
                }
                return false;
            }
            
            if (symbol is IParameterSymbol parameter)
            {
                // Only flag if explicitly nullable
                if (parameter.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    return !HasNullCheck(expression, context);
                }
                return false;
            }
            
            if (symbol is IFieldSymbol field)
            {
                // Only flag if explicitly nullable
                if (field.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    return !HasNullCheck(expression, context);
                }
                return false;
            }
        }

        // For method calls and property access, check the return type
        if (expression is InvocationExpressionSyntax invocation)
        {
            var returnType = typeInfo.Type;
            if (returnType != null && returnType.IsReferenceType && 
                returnType.NullableAnnotation == NullableAnnotation.Annotated)
            {
                return !HasNullCheck(expression, context);
            }
        }

        // Don't flag reference types without explicit nullable annotation
        // (they might be non-nullable reference types)
        return false;
    }

    private static bool HasNullCheck(SyntaxNode expression, SyntaxNodeAnalysisContext context)
    {
        // Look for null checks in the current method/block
        var method = expression.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method == null)
        {
            return false;
        }

        var expressionText = expression.ToString();
        
        // Check for null checks before this expression
        var statements = method.DescendantNodes()
            .OfType<StatementSyntax>()
            .TakeWhile(s => s.SpanStart < expression.SpanStart)
            .ToList();

        foreach (var statement in statements)
        {
            // Check for if (x != null) or if (x == null) return
            if (statement is IfStatementSyntax ifStatement)
            {
                var condition = ifStatement.Condition.ToString();
                if (condition.Contains(expressionText) && 
                    (condition.Contains("!= null") || condition.Contains("== null")))
                {
                    return true;
                }
            }

            // Check for null-conditional operator usage
            if (statement.ToString().Contains($"{expressionText}?"))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWellKnownNonNullType(SyntaxNode expression, SyntaxNodeAnalysisContext context)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken);
        var type = typeInfo.Type;

        if (type == null)
        {
            return false;
        }

        var typeName = type.ToDisplayString();
        
        // Well-known framework types that are never null
        var wellKnownTypes = new[]
        {
            "System.Console",
            "System.Threading.Tasks.Task",
            "System.Type",
            "System.Reflection",
            "Microsoft.CodeAnalysis",
            "Microsoft.CodeAnalysis.CSharp",
            "Microsoft.CodeAnalysis.CSharp.Syntax",
            "Microsoft.CodeAnalysis.Diagnostics",
            "System.Collections.Immutable.ImmutableArray",
            "System.Diagnostics.Debug",
            "System.Diagnostics.Trace"
        };

        return wellKnownTypes.Any(knownType => typeName.StartsWith(knownType, System.StringComparison.Ordinal));
    }

    private static bool IsSyntaxNodeProperty(SyntaxNode expression)
    {
        // Syntax node properties are guaranteed to exist and are never null
        // Check if we're accessing a property on a syntax node
        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            var propertyNames = new[]
            {
                "Name", "Identifier", "Text", "Kind", "Span", "Location",
                "Parent", "Ancestors", "DescendantNodes", "GetLocation",
                "AwaitKeyword", "Expression", "Left", "Right", "Condition",
                "Statements", "Block", "Body", "ReturnType", "Parameters"
            };

            if (memberAccess.Name is IdentifierNameSyntax identifier &&
                propertyNames.Contains(identifier.Identifier.Text))
            {
                return true;
            }
        }

        return false;
    }
}

