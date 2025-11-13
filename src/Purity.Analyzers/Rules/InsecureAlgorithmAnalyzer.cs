using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Purity.Analyzers.Diagnostics;

namespace Purity.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InsecureAlgorithmAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule =
        new(
            id: DiagnosticIds.InsecureAlgorithm,
            title: "Insecure cryptographic algorithm detected",
            messageFormat: "Use of insecure cryptographic algorithm '{0}' detected. Use secure alternatives (SHA256, AES, etc.).",
            category: DiagnosticCategories.Purity,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Insecure cryptographic algorithms (MD5, SHA1, DES, RC2, etc.) are vulnerable to attacks and should not be used. Use modern, secure alternatives.");

    private static readonly ImmutableHashSet<string> InsecureAlgorithmTypes = ImmutableHashSet.Create(
        "MD5", "MD5CryptoServiceProvider", "MD5Cng",
        "SHA1", "SHA1CryptoServiceProvider", "SHA1Cng", "SHA1Managed",
        "DES", "DESCryptoServiceProvider",
        "RC2", "RC2CryptoServiceProvider",
        "TripleDES", "TripleDESCryptoServiceProvider"
    );

    private static readonly ImmutableHashSet<string> InsecureAlgorithmNamespaces = ImmutableHashSet.Create(
        "System.Security.Cryptography.MD5",
        "System.Security.Cryptography.SHA1",
        "System.Security.Cryptography.DES",
        "System.Security.Cryptography.RC2",
        "System.Security.Cryptography.TripleDES"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax objectCreation)
        {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
        if (typeInfo.Type is not INamedTypeSymbol typeSymbol)
        {
            return;
        }

        var typeName = typeSymbol.Name;
        var fullTypeName = typeSymbol.ToDisplayString();

        // Check if it's an insecure algorithm
        if (IsInsecureAlgorithm(typeName, fullTypeName))
        {
            var diagnostic = Diagnostic.Create(Rule, objectCreation.GetLocation(), typeName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        // Check for Create() calls on insecure algorithm types
        if (memberAccess.Name.Identifier.Text == "Create")
        {
            var expression = memberAccess.Expression;
            if (expression is IdentifierNameSyntax identifier)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken);
                if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
                {
                    var typeName = typeSymbol.Name;
                    var fullTypeName = typeSymbol.ToDisplayString();

                    if (IsInsecureAlgorithm(typeName, fullTypeName))
                    {
                        var diagnostic = Diagnostic.Create(Rule, memberAccess.GetLocation(), typeName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }

    private static bool IsInsecureAlgorithm(string typeName, string fullTypeName)
    {
        // Check type name
        if (InsecureAlgorithmTypes.Contains(typeName))
        {
            return true;
        }

        // Check full type name (namespace + type)
        foreach (var insecureNamespace in InsecureAlgorithmNamespaces)
        {
            if (fullTypeName.Contains(insecureNamespace))
            {
                return true;
            }
        }

        // Check for common patterns
        if (fullTypeName.Contains("System.Security.Cryptography"))
        {
            var insecurePatterns = new[] { "MD5", "SHA1", "DES", "RC2" };
            foreach (var pattern in insecurePatterns)
            {
                if (fullTypeName.Contains(pattern) && !fullTypeName.Contains("SHA256") && !fullTypeName.Contains("SHA384") && !fullTypeName.Contains("SHA512"))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

