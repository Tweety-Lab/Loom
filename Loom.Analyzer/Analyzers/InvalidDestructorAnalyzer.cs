using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks that a destructor is defined so that it can be called.
/// </summary>
[LoomAnalyzer]
public class InvalidDestructorAnalyzer : Analyzer
{
    public static DiagnosticDescription InvalidParameters = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Destructor '{0}' cannot declare parameters.");
    public static DiagnosticDescription InvalidModifiers = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Destructor '{0}' cannot be static or extern.");
    public static DiagnosticDescription InvalidName = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Destructor '{0}' must match its containing type's name '{1}'.");
    public static DiagnosticDescription DuplicateDestructor = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Type '{0}' cannot declare multiple destructors.");

    [Visitor]
    public void Visit(DestructorDeclarationNode node)
    {
        if (Context.GetSymbol(node).Symbol is not MethodSymbol { ContainingType: { } type })
            return;

        if (node.Parameters.Count != 0)
            Context.DiagnosticContext?.Report(InvalidParameters, node.Span.StartToken?.Location, node.Name.Text);

        if (node.HasModifier(TokenType.Static) || node.HasModifier(TokenType.Extern))
            Context.DiagnosticContext?.Report(InvalidModifiers, node.Span.StartToken?.Location, node.Name.Text);

        if (type.IsStatic)
            Context.DiagnosticContext?.Report(StaticTypeAnalyzer.InstanceMemberInStaticType, node.Span.StartToken?.Location, node.Name.Text, type.Name);

        if (node.Name.Text != type.Name)
            Context.DiagnosticContext?.Report(InvalidName, node.Span.StartToken?.Location, node.Name.Text, type.Name);

        if (type.Members.OfType<MethodSymbol>().Count(method => method.Kind == MethodSymbol.MethodKind.Destructor) > 1)
            Context.DiagnosticContext?.Report(DuplicateDestructor, node.Span.StartToken?.Location, type.Name);
    }
}
