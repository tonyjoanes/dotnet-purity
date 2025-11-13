using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Diagnostics;

namespace Purity.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AwaitInsideLoopAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule =
        new(
            id: DiagnosticIds.AwaitInsideLoop,
            title: "Avoid awaiting inside loops",
            messageFormat: "Awaiting inside loops can serialize asynchronous work. Collect tasks and await once outside the loop.",
            category: DiagnosticCategories.Purity,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Await expressions inside loops introduce hidden sequential execution. Aggregate tasks inside the loop and await them together to preserve concurrency.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
    }

    private static void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
    {
        var awaitExpression = (AwaitExpressionSyntax)context.Node;
        var loopAncestor = awaitExpression
            .Ancestors()
            .FirstOrDefault(node =>
                node.IsKind(SyntaxKind.ForStatement) ||
                node.IsKind(SyntaxKind.ForEachStatement) ||
                node.IsKind(SyntaxKind.ForEachVariableStatement) ||
                node.IsKind(SyntaxKind.WhileStatement) ||
                node.IsKind(SyntaxKind.DoStatement));

        if (loopAncestor is null)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, awaitExpression.AwaitKeyword.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}


