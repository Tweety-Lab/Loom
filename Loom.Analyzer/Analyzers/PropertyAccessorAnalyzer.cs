using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks that a property declares the accessor an access to it requires.
/// </summary>
[LoomAnalyzer]
public class PropertyAccessorAnalyzer : Analyzer
{
    public static Diagnostic MissingGetAccessor = new("LM0025", Diagnostic.DiagnosticLevel.Error, "The property '{0}' cannot be read because it declares no 'get' accessor.");
    public static Diagnostic MissingSetAccessor = new("LM0026", Diagnostic.DiagnosticLevel.Error, "The property '{0}' cannot be assigned because it declares no 'set' accessor.");

    [Visitor]
    public void Visit(IdentifierNameNode node) => CheckRead(node, Property(node));

    [Visitor]
    public void Visit(MemberAccessExpressionNode node) => CheckRead(node, Property(node));

    [Visitor]
    public void Visit(AssignmentStatementNode node)
    {
        if (Property(node.Target) is not { Setter: null } property || IsPrivateAssignment(node))
            return;

        Context.DiagnosticContext?.Report(MissingSetAccessor, node.Target.Span.StartToken?.Location, property.Name);
    }

    private void CheckRead(ASTNode node, PropertySymbol? property)
    {
        if (property is { Getter: null } && !IsAssignmentTarget(node))
            Context.DiagnosticContext?.Report(MissingGetAccessor, node.Span.StartToken?.Location, property.Name);
    }

    private bool IsPrivateAssignment(AssignmentStatementNode node) =>node.Target is IdentifierNameNode && Property(node.Target) is { BackingField: not null, DeclaringNode: not null } property && Context.FirstAncestorOrSelf<ITypeDeclarationNode>(property.DeclaringNode) == Context.FirstAncestorOrSelf<ITypeDeclarationNode>(node);

    private bool IsAssignmentTarget(ASTNode node) => Context.GetParent(node) is AssignmentStatementNode assignment && assignment.Target == node;

    private PropertySymbol? Property(ExpressionNode node) => node switch
    {
        IdentifierNameNode identifier => Context.GetSymbol(identifier).Symbol as PropertySymbol,
        MemberAccessExpressionNode member => Member(member) as PropertySymbol,
        _ => null
    };

    private Symbol? Member(MemberAccessExpressionNode node)
    {
        var receiverType = Context.ExpressionTypes.TryGetValue(node.Receiver, out var type) ? type : Context.GetSymbol(node.Receiver).Symbol as TypeSymbol;
        return receiverType?.Members.FirstOrDefault(m => m.Name == node.Name.BaseName);
    }
}
