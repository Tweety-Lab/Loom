using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and resolves the <see cref="TypeSymbol"/> for every <see cref="ExpressionNode"/>.
/// </summary>
internal class TypeWalker : ASTWalker
{
    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="TypeWalker"/> class. </summary>
    public TypeWalker(AnalysisContext context) => Context = context;

    [Visitor]
    public void Visit(NumberLiteralNode node) => Context.ExpressionTypes[node] = (TypeSymbol)Context.Binders.First().Value.Lookup("i32")!.First();

    [Visitor]
    public void Visit(DefaultLiteralNode node)
    {
        var declaration = Context.FirstAncestorOrSelf<VariableDeclarationNode>(node);
        var type = declaration == null ? null : TypeResolver.Resolve(Context, declaration, declaration.Type.Base.Text);

        Context.ExpressionTypes[node] = type ?? (TypeSymbol)Context.Binders.First().Value.Lookup("i32")!.First();
    }

    [Visitor]
    public void Visit(CharacterLiteralNode node) => Context.ExpressionTypes[node] = (TypeSymbol)Context.Binders.First().Value.Lookup("char")!.First();

    [Visitor]
    public void Visit(BooleanLiteralNode node) => Context.ExpressionTypes[node] = (TypeSymbol)Context.Binders.First().Value.Lookup("bool")!.First();

    [Visitor]
    public void Visit(IdentifierNameNode node)
    {
        var symbol = Context.GetSymbol(node).Symbol;

        var type = symbol switch
        {
            LocalVariableSymbol local => local.Type,
            MethodSymbol method => method.ReturnType,
            FieldSymbol field => field.Type,
            _ => null
        };

        if (type != null)
            Context.ExpressionTypes[node] = type;
    }

    [Visitor]
    public void Visit(MemberAccessExpressionNode node)
    {
        var receiverIsValue = Context.ExpressionTypes.TryGetValue(node.Receiver, out var receiverType);
        if (!receiverIsValue)
            receiverType = Context.GetSymbol(node.Receiver).Symbol as TypeSymbol;

        if (receiverType == null)
            return;

        var member = receiverType.Members.FirstOrDefault(m => m.Name == node.Name.BaseName);

        if (member is MethodSymbol method && method.ReturnType is { } returnType && (receiverIsValue || method.IsStatic))
            Context.ExpressionTypes[node] = returnType;

        else if (receiverIsValue && member is FieldSymbol field && field.Type is { } fieldType)
            Context.ExpressionTypes[node] = fieldType;
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        var returnType = node.Callee switch
        {
            IdentifierNameNode ident => (Context.GetSymbol(ident).Symbol as MethodSymbol)?.ReturnType,
            MemberAccessExpressionNode member => Context.ExpressionTypes.TryGetValue(member, out var type) ? type : null,
            _ => null
        };

        if (returnType != null)
            Context.ExpressionTypes[node] = returnType;
    }

    [Visitor]
    public void Visit(ObjectCreationExpressionNode node)
    {
        if (Context.GetSymbol(node.ObjectName).Symbol is TypeSymbol type && (type.KnownType == TypeSymbol.DefaultType.Struct || type.KnownType == TypeSymbol.DefaultType.Class))
            Context.ExpressionTypes[node] = type;
    }
}