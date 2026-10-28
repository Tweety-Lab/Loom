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
    public static DiagnosticDescription UnresolvedType = new("LM0038", Diagnostic.DiagnosticLevel.Error, "The type '{0}' could not be found.");

    [Visitor]
    public void Visit(PropertyDeclarationNode node)
    {
        if (node.Type.Base.Type == Token.TokenType.Identifier && TypeResolver.Resolve(Context, node, node.Type.Base.Text) == null)
            Context.DiagnosticContext?.Report(UnresolvedType, node.Type.Base.Location, node.Type.Base.Text);
    }

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node)
    {
        if (node.Variable.Type.Base.Type == Token.TokenType.Identifier && TypeResolver.Resolve(Context, node, node.Variable.Type.Base.Text) == null)
            Context.DiagnosticContext?.Report(UnresolvedType, node.Variable.Type.Base.Location, node.Variable.Type.Base.Text);
    }

    [Visitor]
    public void Visit(ParameterNode node)
    {
        if (node.Type is { } type && type.Base.Type == Token.TokenType.Identifier)
        {
            var symbol = TypeResolver.Resolve(Context, node, type.Base.Text);

            if (symbol == null)
                Context.DiagnosticContext?.Report(UnresolvedType, type.Base.Location, type.Base.Text);
        }
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        if (node.ReturnType.Base.Type == Token.TokenType.Identifier)
        {
            var symbol = TypeResolver.Resolve(Context, node, node.ReturnType.Base.Text);

            if (symbol == null)
                Context.DiagnosticContext?.Report(UnresolvedType, node.ReturnType.Base.Location, node.ReturnType.Base.Text);
        }
    }

    [Visitor]
    public void Visit(DefaultExpressionNode node)
    {
        if (node.Type is { } type && type.Base.Type == Token.TokenType.Identifier)
        {
            var symbol = TypeResolver.Resolve(Context, node, type.Base.Text);

            if (symbol == null)
                Context.DiagnosticContext?.Report(UnresolvedType, type.Base.Location, type.Base.Text);
        }
    }
}
