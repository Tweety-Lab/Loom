using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;
using Loom.Parser.Tokenizer;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for usage of types that do not exist.
/// </summary>
[LoomAnalyzer]
public class UnresolvedTypeAnalyzer : Analyzer
{
    public static Diagnostic UnresolvedTypeDiagnostic = new(Diagnostic.DiagnosticLevel.Error, "The type '{0}' could not be found.");

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        if (node.ReturnType.Base.Type == Token.TokenType.Identifier)
        {
            var symbol = TypeResolver.Resolve(Context, node, node.ReturnType.Base.Text);

            if (symbol == null)
                Context.DiagnosticContext?.Report(UnresolvedTypeDiagnostic, node.ReturnType.Base.Location, node.ReturnType.Base.Text);
        }
    }

    [Visitor]
    public void Visit(DefaultLiteralNode node)
    {
        if (node.Type is { } type && type.Base.Type == Token.TokenType.Identifier)
        {
            var symbol = TypeResolver.Resolve(Context, node, type.Base.Text);

            if (symbol == null)
                Context.DiagnosticContext?.Report(UnresolvedTypeDiagnostic, type.Base.Location, type.Base.Text);
        }
    }
}
