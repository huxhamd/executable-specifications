using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace acceptance.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DslAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor UnnamedArgument = new(
        id: "ACC001",
        title: "Specs name every argument they pass to the DSL",
        messageFormat: "Name this argument to '{0}': specs pass every DSL argument by name, so an unnamed argument is always a mistake",
        category: "Acceptance",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every value in a spec is labelled, and a parameter added anywhere in a DSL method can't silently change what an existing spec means.",
        customTags: WellKnownDiagnosticTags.NotConfigurable);

    public static readonly DiagnosticDescriptor DefaultOnImplementation = new(
        id: "ACC002",
        title: "DSL defaults belong on the interface",
        messageFormat: "Move the default for '{0}' to the DSL interface: specs call through the interface, so C# ignores a default declared here",
        category: "Acceptance",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "C# takes a default from the type a call is made through. Specs call the DSL through its interfaces, so a default on the class would be ignored by specs and used only by the DSL itself.",
        customTags: WellKnownDiagnosticTags.NotConfigurable);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(UnnamedArgument, DefaultOnImplementation);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(CheckArgumentsAreNamed, SyntaxKind.InvocationExpression);
        context.RegisterSymbolAction(CheckNoDefaultsOnImplementation, SymbolKind.Method);
    }

    private static void CheckArgumentsAreNamed(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol method) return;
        if (!IsInDslNamespace(method.ContainingType)) return;
        if (SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, context.Compilation.Assembly)) return;

        foreach (var argument in invocation.ArgumentList.Arguments.Where(a => a.NameColon is null))
            context.ReportDiagnostic(Diagnostic.Create(UnnamedArgument, argument.GetLocation(), method.Name));
    }

    private static void CheckNoDefaultsOnImplementation(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        var type = method.ContainingType;
        if (type.TypeKind == TypeKind.Interface || !IsInDslNamespace(type)) return;

        var implementsDsl = type.AllInterfaces
            .Where(IsInDslNamespace)
            .SelectMany(dsl => dsl.GetMembers())
            .Any(member => SymbolEqualityComparer.Default.Equals(
                type.FindImplementationForInterfaceMember(member), method));
        if (!implementsDsl) return;

        foreach (var parameter in method.Parameters.Where(p => p.HasExplicitDefaultValue))
            context.ReportDiagnostic(Diagnostic.Create(
                DefaultOnImplementation, parameter.Locations.FirstOrDefault(), parameter.Name));
    }

    private static bool IsInDslNamespace(INamedTypeSymbol type) =>
        type.ContainingNamespace is { IsGlobalNamespace: false, Name: "Dsl" };
}
