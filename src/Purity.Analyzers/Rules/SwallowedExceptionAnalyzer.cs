using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Diagnostics;

namespace Purity.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SwallowedExceptionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule =
        new(
            id: DiagnosticIds.SwallowedException,
            title: "Exception is caught but not handled",
            messageFormat: "Exception is caught but not logged, handled, or rethrown. This hides errors and delays root cause finding.",
            category: DiagnosticCategories.Purity,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Catching exceptions without logging, handling, or rethrowing them hides errors and makes debugging extremely difficult. Always log exceptions or rethrow them.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);
    }

    private static void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not CatchClauseSyntax catchClause)
        {
            return;
        }

        var block = catchClause.Block;
        if (block == null || block.Statements.Count == 0)
        {
            // Empty catch block - definitely swallowed
            var diagnostic = Diagnostic.Create(Rule, catchClause.GetLocation());
            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Check if the exception is handled properly
        if (!IsExceptionHandled(block, catchClause))
        {
            var diagnostic = Diagnostic.Create(Rule, catchClause.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsExceptionHandled(BlockSyntax block, CatchClauseSyntax catchClause)
    {
        var statements = block.Statements;
        
        foreach (var statement in statements)
        {
            // Check for logging
            if (ContainsLogging(statement))
            {
                return true;
            }

            // Check for rethrowing
            if (IsRethrowing(statement, catchClause))
            {
                return true;
            }

            // Check for throwing a new exception (wrapping)
            if (IsThrowingNewException(statement))
            {
                return true;
            }

            // Check for meaningful handling (not just empty or comment-only)
            if (IsMeaningfulHandling(statement))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsLogging(StatementSyntax statement)
    {
        var statementText = statement.ToString().ToLowerInvariant();
        
        // Common logging patterns
        var loggingKeywords = new[]
        {
            "log", "logger", "logging", "trace", "debug", "info", "warn", "error",
            "console.write", "console.writeline", "system.diagnostics.debug",
            "sentry", "applicationinsights", "telemetry"
        };

        return loggingKeywords.Any(keyword => statementText.Contains(keyword));
    }

    private static bool IsRethrowing(StatementSyntax statement, CatchClauseSyntax catchClause)
    {
        if (statement is ThrowStatementSyntax throwStatement)
        {
            // Check if it's rethrowing the caught exception
            if (throwStatement.Expression == null)
            {
                // Bare throw - rethrows the caught exception
                return true;
            }

            // Check if it's throwing the caught exception variable
            if (catchClause.Declaration != null)
            {
                var exceptionName = catchClause.Declaration.Identifier.Text;
                var throwText = throwStatement.Expression.ToString();
                if (throwText.Contains(exceptionName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsThrowingNewException(StatementSyntax statement)
    {
        if (statement is ThrowStatementSyntax throwStatement &&
            throwStatement.Expression != null)
        {
            var expression = throwStatement.Expression.ToString();
            // Check if it's creating a new exception (wrapping the original)
            return expression.Contains("new ") && 
                   (expression.Contains("Exception") || 
                    expression.Contains("throw new"));
        }

        return false;
    }

    private static bool IsMeaningfulHandling(StatementSyntax statement)
    {
        // Check if statement does something meaningful beyond just catching
        // This is a heuristic - could be improved
        
        // Skip empty statements, comments-only, etc.
        if (statement is EmptyStatementSyntax)
        {
            return false;
        }

        // If there are multiple statements, likely some handling
        if (statement is BlockSyntax block && block.Statements.Count > 1)
        {
            return true;
        }

        // Check for common handling patterns
        var statementText = statement.ToString().ToLowerInvariant();
        var handlingPatterns = new[]
        {
            "return", "break", "continue", "if", "switch", "try"
        };

        return handlingPatterns.Any(pattern => statementText.Contains(pattern));
    }
}

