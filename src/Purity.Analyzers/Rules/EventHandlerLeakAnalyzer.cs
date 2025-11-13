using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Diagnostics;

namespace Purity.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventHandlerLeakAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule =
        new(
            id: DiagnosticIds.EventHandlerLeak,
            title: "Event handler may cause memory leak",
            messageFormat: "Event '{0}' is subscribed but never unsubscribed. Unsubscribe in Dispose or when no longer needed.",
            category: DiagnosticCategories.Purity,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Event subscriptions keep references to handlers, preventing garbage collection. Always unsubscribe from events to prevent memory leaks.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeEventSubscription, SyntaxKind.AddAssignmentExpression);
    }

    private static void AnalyzeEventSubscription(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AssignmentExpressionSyntax assignment ||
            assignment.IsKind(SyntaxKind.AddAssignmentExpression) == false)
        {
            return;
        }

        if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        // Check if this is an event subscription (left side is an event)
        var eventSymbol = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol;
        if (eventSymbol is not IEventSymbol)
        {
            return;
        }

        var eventName = memberAccess.Name.Identifier.Text;

        // Skip if in a Dispose method (likely being unsubscribed)
        if (IsInDisposeMethod(assignment))
        {
            return;
        }

        // Check if there's a corresponding unsubscribe (-=) in the same scope
        if (HasCorrespondingUnsubscribe(assignment, eventName, context))
        {
            return;
        }

        // Check if the class implements IDisposable (should unsubscribe in Dispose)
        var containingType = context.ContainingSymbol?.ContainingType;
        if (containingType != null && ImplementsIDisposable(containingType))
        {
            // If class implements IDisposable, check if there's an unsubscribe in Dispose
            if (!HasUnsubscribeInDispose(containingType, eventName, context))
            {
                var diagnostic = Diagnostic.Create(Rule, assignment.GetLocation(), eventName);
                context.ReportDiagnostic(diagnostic);
            }
        }
        else
        {
            // Class doesn't implement IDisposable, warn about potential leak
            var diagnostic = Diagnostic.Create(Rule, assignment.GetLocation(), eventName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsInDisposeMethod(SyntaxNode node)
    {
        var methodDeclaration = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration == null)
        {
            return false;
        }

        return methodDeclaration.Identifier.Text == "Dispose" ||
               methodDeclaration.Identifier.Text.StartsWith("Dispose", System.StringComparison.Ordinal);
    }

    private static bool HasCorrespondingUnsubscribe(AssignmentExpressionSyntax subscription, string eventName, SyntaxNodeAnalysisContext context)
    {
        var containingMethod = subscription.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (containingMethod == null)
        {
            return false;
        }

        // Look for -= assignment for the same event in the same method
        return containingMethod.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Any(assignment =>
            {
                if (!assignment.IsKind(SyntaxKind.SubtractAssignmentExpression))
                {
                    return false;
                }

                if (assignment.Left is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.Text == eventName)
                {
                    return true;
                }

                return false;
            });
    }

    private static bool ImplementsIDisposable(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == "System.IDisposable");
    }

    private static bool HasUnsubscribeInDispose(INamedTypeSymbol containingType, string eventName, SyntaxNodeAnalysisContext context)
    {
        // This is a simplified check - in a real implementation, you'd need to
        // find the Dispose method and check for unsubscriptions
        // For now, we'll be conservative and assume if IDisposable is implemented,
        // the developer should handle it properly
        return false; // Conservative: assume not handled unless proven otherwise
    }
}

