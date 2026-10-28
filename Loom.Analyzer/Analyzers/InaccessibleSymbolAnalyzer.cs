using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for usage of symbols that aren't exported or members that are private.
/// </summary>
[LoomAnalyzer]
public class InaccessibleSymbolAnalyzer : Analyzer
{
    public static DiagnosticDescription UnexportedSymbolDiagnostic = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "The symbol '{0}' cannot be accessed as it is not exported.");
    public static DiagnosticDescription InaccessibleMemberDiagnostic = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "The member '{0}' cannot be accessed as it is not public.");

    [Visitor]
    public void Visit(IdentifierNameNode node)
    {
        var symbol = Context.GetSymbol(node).Symbol;

        if (symbol is null or ModuleSymbol)
            return;

        CheckExported(node, symbol);
    }

    [Visitor]
    public void Visit(MemberAccessExpressionNode node)
    {
        var receiverType = Context.ExpressionTypes.TryGetValue(node.Receiver, out var type) ? type : Context.GetSymbol(node.Receiver).Symbol as TypeSymbol;

        if (receiverType?.Members.FirstOrDefault(m => m.Name == node.Name.BaseName) is not Symbol member)
            return;

        MemberAccessibility? accessibility = (member as IAccessibleSymbol)?.Accessibility;
        if (accessibility != MemberAccessibility.Private)
            return;

        var declaringType = Context.FirstAncestorOrSelf<ITypeDeclarationNode>(member.DeclaringNode!);
        var usageType = Context.FirstAncestorOrSelf<ITypeDeclarationNode>(node);

        if (declaringType != null && declaringType != usageType)
            Context.DiagnosticContext?.Report(InaccessibleMemberDiagnostic, node.Span.StartToken?.Location, member.Name);
    }

    [Visitor]
    public void Visit(InheritanceListNode node)
    {
        foreach (var syntax in node.SelectMany(type => type.Descendants.OfType<TypeNode>().Prepend(type)))
            if (Context.GetSymbol(syntax).Symbol is TypeSymbol type)
                CheckExported(syntax, type);
    }

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node) => CheckTypeAccess(node, node.Variable.Type.Base.Text);

    [Visitor]
    public void Visit(BinaryExpressionNode node)
    {
        if (Context.GetSymbol(node).Symbol is not MethodSymbol { Kind: MethodSymbol.MethodKind.Operator, Accessibility: MemberAccessibility.Private } method)
            return;

        var declaringType = Context.FirstAncestorOrSelf<ITypeDeclarationNode>(method.DeclaringNode!);
        if (declaringType != Context.FirstAncestorOrSelf<ITypeDeclarationNode>(node))
            Context.DiagnosticContext?.Report(InaccessibleMemberDiagnostic, node.Operator.Location, "operator+");
    }

    [Visitor]
    public void Visit(ArrayAccessExpressionNode node)
    {
        if (Context.GetSymbol(node).Symbol is not MethodSymbol { Kind: MethodSymbol.MethodKind.Operator, Accessibility: MemberAccessibility.Private } method)
            return;

        var declaringType = Context.FirstAncestorOrSelf<ITypeDeclarationNode>(method.DeclaringNode!);
        var usageType = Context.FirstAncestorOrSelf<ITypeDeclarationNode>(node);
        if (declaringType != null && declaringType != usageType)
            Context.DiagnosticContext?.Report(InaccessibleMemberDiagnostic, node.Span.StartToken?.Location ?? node.Receiver.Span.StartToken?.Location, "operator[]");
    }

    [Visitor]
    public void Visit(FieldDeclarationNode node) => CheckTypeAccess(node, node.Variable.Type.Base.Text);

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        CheckTypeAccess(node, node.ReturnType.Base.Text);

        foreach (var parameter in node.Parameters)
            CheckTypeAccess(node, parameter.Type.Base.Text);
    }

    [Visitor]
    public void Visit(InstanceCreationExpresssionNode node)
    {
        var type = Context.ExpressionTypes.TryGetValue(node, out var resolvedType) ? resolvedType : null;

        if (type == null)
            return;

        var constructor = type.Members.OfType<MethodSymbol>().FirstOrDefault(m => m.Kind == MethodSymbol.MethodKind.Constructor);

        if (constructor == null)
            return;

        if ((constructor as IAccessibleSymbol)?.Accessibility == MemberAccessibility.Private)
        {
            var declaringType = Context.FirstAncestorOrSelf<ITypeDeclarationNode>(constructor.DeclaringNode!);
            var usageType = Context.FirstAncestorOrSelf<ITypeDeclarationNode>(node);

            if (declaringType != null && declaringType != usageType)
                Context.DiagnosticContext?.Report(InaccessibleMemberDiagnostic, node.Span.StartToken?.Location, constructor.Name);
        }
    }

    private void CheckTypeAccess(ASTNode node, string typeName)
    {
        if (TypeResolver.Resolve(Context, node, typeName) is { } type)
            CheckExported(node, type);
    }

    private void CheckExported(ASTNode node, Symbol symbol)
    {
        if (symbol.DeclaringNode == null || symbol is MemberSymbol { ContainingType: not null })
            return;

        ModuleNode? usageModuleNode = Context.FirstAncestorOrSelf<ModuleNode>(node);
        ModuleNode? symbolModuleNode = Context.FirstAncestorOrSelf<ModuleNode>(symbol.DeclaringNode);

        if (symbol is not IExportableSymbol exportable)
            return;

        var usageModule = usageModuleNode is not null ? Context.GetSymbol(usageModuleNode).Symbol as ModuleSymbol : null;
        var symbolModule = symbolModuleNode is not null ? Context.GetSymbol(symbolModuleNode).Symbol as ModuleSymbol : null;

        if (usageModule != symbolModule && !exportable.IsExported)
            Context.DiagnosticContext?.Report(UnexportedSymbolDiagnostic, node.Span.StartToken?.Location, symbol.Name);
    }
}
