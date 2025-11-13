using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Diagnostics;

namespace Purity.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StringConcatenationInLoopAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule =
        new(
            id: DiagnosticIds.StringConcatenationInLoop,
            title: "String concatenation in loop",
            messageFormat: "String concatenation using '+' or '+=' in loop detected. Use StringBuilder for better performance.",
            category: DiagnosticCategories.Purity,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "String concatenation in loops creates many intermediate string objects, causing significant performance and memory overhead. Use StringBuilder instead.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.AddAssignmentExpression);
        context.RegisterSyntaxNodeAction(AnalyzeBinaryExpression, SyntaxKind.AddExpression);
    }

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AssignmentExpressionSyntax assignment ||
            !assignment.IsKind(SyntaxKind.AddAssignmentExpression))
        {
            return;
        }

        // Check if left side is a string variable
        if (assignment.Left is not IdentifierNameSyntax identifier)
        {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(identifier, context.CancellationToken);
        if (!IsStringType(typeInfo.Type))
        {
            return;
        }

        // Check if we're inside a loop
        if (IsInsideLoop(assignment))
        {
            var diagnostic = Diagnostic.Create(Rule, assignment.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeBinaryExpression(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not BinaryExpressionSyntax binaryExpression ||
            !binaryExpression.IsKind(SyntaxKind.AddExpression))
        {
            return;
        }

        // Check if either side is a string
        var leftType = context.SemanticModel.GetTypeInfo(binaryExpression.Left, context.CancellationToken).Type;
        var rightType = context.SemanticModel.GetTypeInfo(binaryExpression.Right, context.CancellationToken).Type;

        if (!IsStringType(leftType) && !IsStringType(rightType))
        {
            return;
        }

        // Check if we're inside a loop
        if (IsInsideLoop(binaryExpression))
        {
            // Check if this is part of an assignment to a string variable
            var parent = binaryExpression.Parent;
            if (parent is AssignmentExpressionSyntax assignment &&
                assignment.Right == binaryExpression)
            {
                var assignmentType = context.SemanticModel.GetTypeInfo(assignment.Left, context.CancellationToken).Type;
                if (IsStringType(assignmentType))
                {
                    var diagnostic = Diagnostic.Create(Rule, binaryExpression.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
            else if (parent is EqualsValueClauseSyntax equalsValue)
            {
                // Variable declaration with string concatenation
                var variableDeclarator = equalsValue.Parent as VariableDeclaratorSyntax;
                if (variableDeclarator != null)
                {
                    var variableType = context.SemanticModel.GetTypeInfo(variableDeclarator, context.CancellationToken).Type;
                    if (IsStringType(variableType))
                    {
                        var diagnostic = Diagnostic.Create(Rule, binaryExpression.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }

    private static bool IsStringType(ITypeSymbol? type)
    {
        if (type == null)
        {
            return false;
        }

        return type.SpecialType == SpecialType.System_String ||
               type.ToDisplayString() == "string" ||
               type.ToDisplayString() == "System.String";
    }

    private static bool IsInsideLoop(SyntaxNode node)
    {
        return node.Ancestors().Any(ancestor =>
            ancestor.IsKind(SyntaxKind.ForStatement) ||
            ancestor.IsKind(SyntaxKind.ForEachStatement) ||
            ancestor.IsKind(SyntaxKind.ForEachVariableStatement) ||
            ancestor.IsKind(SyntaxKind.WhileStatement) ||
            ancestor.IsKind(SyntaxKind.DoStatement));
    }
}

