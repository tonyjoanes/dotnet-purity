using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Diagnostics;

namespace Purity.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SyncOverAsyncAnalyzer : DiagnosticAnalyzer
{
    private static readonly string[] BlockingMembers = { "Result", "GetResult", "Wait" };

    private static readonly DiagnosticDescriptor Rule =
        new(
            id: DiagnosticIds.SyncOverAsync,
            title: "Avoid blocking on asynchronous operations",
            messageFormat: "Synchronous wait detected on asynchronous operation via '{0}'. Prefer awaiting the task instead.",
            category: DiagnosticCategories.Purity,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Blocking on asynchronous operations defeats concurrency, risks deadlocks, and breaks purity. Use await and asynchronous composition instead.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var memberName = memberAccess.Name.Identifier.Text;
        if (memberName is not "Result")
        {
            return;
        }

        if (!IsTaskLike(memberAccess.Expression, context))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, memberAccess.Name.GetLocation(), $"{memberName} property");
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        switch (invocation.Expression)
        {
            case MemberAccessExpressionSyntax memberAccess when BlockingMembers.Contains(memberAccess.Name.Identifier.Text):
                if (IsTaskLike(memberAccess.Expression, context))
                {
                    var report = Diagnostic.Create(
                        Rule,
                        memberAccess.Name.GetLocation(),
                        $"{memberAccess.Name.Identifier.Text} method");
                    context.ReportDiagnostic(report);
                }
                break;
            case MemberBindingExpressionSyntax memberBinding when BlockingMembers.Contains(memberBinding.Name.Identifier.Text):
                var reportFromBinding = Diagnostic.Create(
                    Rule,
                    memberBinding.Name.GetLocation(),
                    $"{memberBinding.Name.Identifier.Text} method");
                context.ReportDiagnostic(reportFromBinding);
                break;
            case MemberAccessExpressionSyntax chainedAccess
                when chainedAccess.Name.Identifier.Text is "GetResult"
                     && invocation.Expression is MemberAccessExpressionSyntax:
                if (IsTaskAwaiterChain(chainedAccess.Expression, context))
                {
                    var chainedReport = Diagnostic.Create(
                        Rule,
                        chainedAccess.Name.GetLocation(),
                        "GetAwaiter().GetResult()");
                    context.ReportDiagnostic(chainedReport);
                }
                break;
        }
    }

    private static bool IsTaskLike(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).Type;
        return typeInfo is INamedTypeSymbol symbol && ImplementsTaskLike(symbol);
    }

    private static bool IsTaskAwaiterChain(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
    {
        if (expression is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        if (memberAccess.Name.Identifier.Text is not "GetAwaiter")
        {
            return false;
        }

        return IsTaskLike(memberAccess.Expression, context);
    }

    private static bool ImplementsTaskLike(INamedTypeSymbol symbol)
    {
        return symbol.OriginalDefinition.ToDisplayString() switch
        {
            "System.Threading.Tasks.Task" => true,
            "System.Threading.Tasks.Task<TResult>" => true,
            "System.Threading.Tasks.ValueTask" => true,
            "System.Threading.Tasks.ValueTask<TResult>" => true,
            _ => symbol.AllInterfaces.Any(i => i.ToDisplayString() == "System.Threading.Tasks.Sources.IValueTaskSource")
        };
    }
}


