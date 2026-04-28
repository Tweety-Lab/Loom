using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;
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
    public void Visit(MethodDefinitionNode node)
    {
        if (node.ReturnType.Type == Token.TokenType.Identifier)
        {
            var symbol = Context.Binders.First().Value.Lookup(node.ReturnType.Value) as TypeSymbol;

            if (symbol == null)
                Context.DiagnosticContext?.Report(UnresolvedTypeDiagnostic, node.ReturnType.Value);
        }
    }
}
