using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;


/// <summary>
/// Checks instance-use of static types.
/// </summary>
[LoomAnalyzer]
public class StaticTypeAnalyzer : Analyzer
{
    public static Diagnostic InstansiatedStaticType = new(Diagnostic.DiagnosticLevel.Error, "Cannot instantiate static type '{0}'.");
    public static Diagnostic InstanceMemberInStaticType = new(Diagnostic.DiagnosticLevel.Error, "Cannot declare instance member '{0}' in static type '{1}'.");
    public static Diagnostic InstanceMemberRequiresReceiver = new(Diagnostic.DiagnosticLevel.Error, "Cannot access instance member '{0}' in a static function.");

    [Visitor]
    public void Visit(IdentifierNameNode node)
    {
        var parent = Context.GetParent(node);

        if (parent is MemberAccessExpressionNode member && member.Name == node)
            return;

        var symbol = parent is CallExpressionNode call && call.Callee == node ? Context.GetSymbol(call).Symbol : Context.GetSymbol(node).Symbol;

        if (!(symbol is FieldSymbol or PropertySymbol { IsStatic: false } or MethodSymbol { IsStatic: false, ContainingType: not null }))
            return;

        for (var enclosing = parent; enclosing != null; enclosing = Context.GetParent(enclosing))
        {
            if (enclosing is MethodDeclarationNode or OperatorDeclarationNode or ConstructorDeclarationNode or DestructorDeclarationNode && Context.GetSymbol(enclosing).Symbol is MethodSymbol function)
            {
                if (function.IsStatic)
                    Context.DiagnosticContext?.Report(InstanceMemberRequiresReceiver, node.StartToken?.Location, symbol!.Name);

                return;
            }

            if (enclosing is ITypeDeclarationNode)
                return;
        }
    }

    [Visitor]
    public void Visit(InstanceCreationExpresssionNode node)
    {
        if (Context.GetSymbol(node.TypeName).Symbol is TypeSymbol type && type.IsStatic)
            Context.DiagnosticContext?.Report(InstansiatedStaticType, node.StartToken?.Location, type.Name);
    }

    [Visitor]
    public void Visit(FieldDeclarationNode node)
    {
        if (EnclosingStaticType(node) is { } type)
            Context.DiagnosticContext?.Report(InstanceMemberInStaticType, node.Variable.StartToken?.Location, node.Variable.Name.Text, type.Name);
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        if (EnclosingStaticType(node) is { } type && Context.GetSymbol(node).Symbol is MethodSymbol { IsStatic: false } method)
            Context.DiagnosticContext?.Report(InstanceMemberInStaticType, node.StartToken?.Location, method.Name, type.Name);
    }

    [Visitor]
    public void Visit(PropertyDeclarationNode node)
    {
        if (EnclosingStaticType(node) is { } type && Context.GetSymbol(node).Symbol is PropertySymbol { IsStatic: false } property)
            Context.DiagnosticContext?.Report(InstanceMemberInStaticType, node.StartToken?.Location, property.Name, type.Name);
    }

    private TypeSymbol? EnclosingStaticType(ASTNode node) => Context.FirstAncestorOrSelf<ITypeDeclarationNode>(node) is { } declaration && Context.GetSymbol((ASTNode)declaration).Symbol is TypeSymbol { IsStatic: true } type ? type : null;
}
