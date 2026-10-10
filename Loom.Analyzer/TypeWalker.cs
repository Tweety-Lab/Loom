using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;
using System.Diagnostics.CodeAnalysis;

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

    /// <summary> Resolves the contextual type of <paramref name="node"/>, i.e. the type it is being initialized as. </summary>
    /// <remarks> This is what <c>default</c> literals resolve to. </remarks>
    public TypeSymbol? ResolveContextualType(ASTNode node)
    {
        var declaration = Context.FirstAncestorOrSelf<VariableDeclarationNode>(node);
        if (declaration != null)
            return TypeResolver.Resolve(Context, declaration, declaration.Type);

        var assignment = Context.FirstAncestorOrSelf<AssignmentStatementNode>(node);
        if (assignment != null && Context.ExpressionTypes.TryGetValue(assignment.Target, out var targetType))
            return targetType;

        return null;
    }

    [Visitor]
    public void Visit(NumberLiteralNode node) => Context.ExpressionTypes[node] = (TypeSymbol)Context.Binders.First().Value.Lookup("i32")!.First();

    [Visitor]
    public void Visit(DefaultExpressionNode node)
    {
        var type = node.Type is { } declared ? TypeResolver.Resolve(Context, node, declared) ?? ResolveContextualType(node) : ResolveContextualType(node);
        Context.ExpressionTypes[node] = type ?? (TypeSymbol)Context.Binders.First().Value.Lookup("void")!.First();
    }

    [Visitor]
    public void Visit(SizeOfExpressionNode node)
    {
        TypeResolver.Resolve(Context, node, node.Type);
        Context.ExpressionTypes[node] = (TypeSymbol)Context.Binders.First().Value.Lookup("i64")!.First();
    }

    [Visitor]
    public void Visit(CharacterLiteralNode node) => Context.ExpressionTypes[node] = (TypeSymbol)Context.Binders.First().Value.Lookup("char")!.First();

    [Visitor]
    public void Visit(StringLiteralNode node)
    {
        if (TypeResolver.Resolve(Context, node, "Slice") is { } slice)
            Context.ExpressionTypes[node] = slice;
    }

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
            ParameterSymbol parameter => parameter.Type,
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
    public void Visit(ArrayAccessExpressionNode node)
    {
        if (TryGetArrayType(node.Receiver, out var array))
            Context.ExpressionTypes[node] = array.ElementType;
    }

    [Visitor]
    public void Visit(ArrayLiteralNode node)
    {
        if (ResolveContextualType(node) is ArrayTypeSymbol contextual)
        {
            Context.ExpressionTypes[node] = contextual;
            return;
        }

        var elementType = node.Elements.Select(e => Context.ExpressionTypes.TryGetValue(e, out var type) ? type : null).FirstOrDefault(t => t != null);

        if (elementType == null)
            return;

        Context.ExpressionTypes[node] = new ArrayTypeSymbol(elementType, node.Elements.Count);
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        // Type arguments are not children of the call, so they must be resolved against the call's scope explicitly
        foreach (var typeArgument in node.TypeArguments)
            TypeResolver.Resolve(Context, node, typeArgument);

        var invocation = OverloadResolver.Resolve(Context, node);
        Context.Bind(node, invocation);

        if (invocation.Symbol is MethodSymbol method && ResolveReturnType(node, method) is { } returnType)
            Context.ExpressionTypes[node] = returnType;
    }

    [Visitor]
    public void Visit(InstanceCreationExpresssionNode node)
    {
        if (Context.GetSymbol(node.TypeName).Symbol is TypeSymbol type && (type.KnownType == TypeSymbol.DefaultType.Struct || type.KnownType == TypeSymbol.DefaultType.Class))
        {
            Context.ExpressionTypes[node] = type;

            var constructors = type.Members.OfType<MethodSymbol>().Where(m => m.Kind == MethodSymbol.MethodKind.Constructor).ToArray();

            Context.Bind(node, OverloadResolver.Resolve(Context, new CallExpressionNode(node.TypeName, [], node.Arguments), constructors));
        }
    }

    // Resolves the type an overloaded method returns, with the type arguments of the call bound to its type parameters
    private TypeSymbol? ResolveReturnType(CallExpressionNode node, MethodSymbol method)
    {
        if (method.ReturnType is not { } returnType)
            return null;

        if (!method.IsGeneric || node.TypeArguments.Count != method.TypeParameters.Count)
            return returnType;

        var arguments = node.TypeArguments.Select(t => Context.GetSymbol(t).Symbol as TypeSymbol).ToList();

        if (arguments.Any(a => a == null))
            return returnType;

        return new TypeSubstitution(method.TypeParameters.Select((p, i) => new KeyValuePair<string, TypeSymbol>(p.Name, arguments[i]!))).Resolve(returnType);
    }

    private bool TryGetArrayType(ExpressionNode node, [NotNullWhen(true)] out ArrayTypeSymbol? array)
    {
        array = null;

        if (Context.ExpressionTypes.TryGetValue(node, out var type))
            array = type as ArrayTypeSymbol;

        // Bare field names are bound to their field, which may not have a resolved type yet.
        else if (node is IdentifierNameNode identifier)
            array = (Context.GetSymbol(identifier).Symbol switch
            {
                LocalVariableSymbol local => local.Type,
                FieldSymbol field => field.Type,
                ParameterSymbol parameter => parameter.Type,
                _ => null
            }) as ArrayTypeSymbol;

        return array != null;
    }
}
