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
        var parent = Context.GetParent(node);

        if (parent is ArrayLiteralNode array)
        {
            var contextual = ResolveContextualType(array);

            return contextual is ArrayTypeSymbol arrayType ? arrayType.ElementType : TypeConversions.GetVectorElementType(contextual);
        }

        if (parent is PropertyDeclarationNode property)
            return Context.GetSymbol(property).As<PropertySymbol>()?.Type;

        if (parent is ReturnStatementNode && Context.FirstAncestorOrSelf<MethodDeclarationNode>(node) is { } method)
            return Context.GetSymbol(method).As<MethodSymbol>()?.ReturnType;

        if (parent is ReturnStatementNode && Context.FirstAncestorOrSelf<OperatorDeclarationNode>(node) is { } op)
            return Context.GetSymbol(op).As<MethodSymbol>()?.ReturnType;

        if (parent is ArrayAccessExpressionNode access && node == access.Index)
            return ResolveArgumentType(access.Index, [access.Receiver, access.Index], OverloadResolver.IndexOperatorCandidates(Context, access));

        if (parent is CallExpressionNode call && node is ExpressionNode argument && call.Arguments.Contains(argument))
        {
            var arguments = call.TypeArguments.Select(type => TypeResolver.Resolve(Context, call, type)).ToArray();
            var candidates = Context.GetSymbol(call.Callee).AsMembers().AsAll<MethodSymbol>().Where(method => method.Arity == arguments.Length);

            if (arguments.Any(type => type == null))
                return null;

            return ResolveArgumentType(argument, call.Arguments, candidates.Select(method => method.Arity == 0 ? method : method.Construct([.. arguments!])));
        }

        if (parent is InstanceCreationExpresssionNode creation && node is ExpressionNode constructorArgument && Context.GetSymbol(creation.TypeName).Symbol is TypeSymbol containingType)
        {
            if (creation.TypeArguments is { Count: > 0 } typeArguments)
            {
                if (TypeResolver.Construct(Context, creation, containingType, typeArguments) is not { } constructed)
                    return null;

                containingType = constructed;
            }

            return ResolveArgumentType(constructorArgument, creation.Arguments, containingType.Members.OfType<MethodSymbol>().Where(method => method.Kind == MethodSymbol.MethodKind.Constructor));
        }

        var declaration = Context.FirstAncestorOrSelf<VariableDeclarationNode>(node);
        if (declaration != null)
            return TypeResolver.Resolve(Context, declaration, declaration.Type);

        var assignment = Context.FirstAncestorOrSelf<AssignmentStatementNode>(node);
        if (assignment != null && Context.ExpressionTypes.TryGetValue(assignment.Target, out var targetType))
            return targetType;

        return null;
    }

    private static TypeSymbol? ResolveArgumentType(ExpressionNode argument, List<ExpressionNode> arguments, IEnumerable<MethodSymbol> candidates)
    {
        int index = arguments.IndexOf(argument);
        var types = candidates.Where(method => method.Parameters.Count == arguments.Count).Select(method => method.Parameters[index].Type).OfType<TypeSymbol>().ToArray();

        return types is [var first, ..] && types.All(type => TypeConversions.IsIdentical(first, type)) ? first : null;
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
        if (TypeResolver.Resolve(Context, node, "Slice") is not { } slice || TypeResolver.Resolve(Context, node, "char") is not { } charType)
            return;

        // String literal = Slice<char>
        Context.ExpressionTypes[node] = slice.Construct(charType);
    }

    [Visitor]
    public void Visit(RangeExpressionNode node)
    {
        if (TypeResolver.Resolve(Context, node, "Range") is not { } range || TypeResolver.Resolve(Context, node, "i32") is not { } intType)
            return;

        // Range expression = Range<i32>
        Context.ExpressionTypes[node] = range.Construct(intType);

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
            PropertySymbol property => property.Type,
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

        else if (member is PropertySymbol property && property.Type is { } propertyType && (receiverIsValue || property.IsStatic))
            Context.ExpressionTypes[node] = propertyType;
    }

    [Visitor]
    public void Visit(ArrayAccessExpressionNode node)
    {
        if (TryGetArrayType(node.Receiver, out var array))
        {
            Context.ExpressionTypes[node] = array.ElementType;
            return;
        }

        var invocation = OverloadResolver.ResolveIndexOperator(Context, node);
        Context.Bind(node, invocation);

        if (invocation.Symbol is MethodSymbol { ReturnType: { } returnType })
            Context.ExpressionTypes[node] = returnType;
    }

    [Visitor]
    public void Visit(IncrementExpressionNode node)
    {
        if (Context.ExpressionTypes.TryGetValue(node.Target, out var type))
            Context.ExpressionTypes[node] = type;
    }

    [Visitor]
    public void Visit(ArrayLiteralNode node)
    {
        if (ResolveContextualType(node) is ArrayTypeSymbol contextual)
        {
            Context.ExpressionTypes[node] = contextual;
            return;
        }

        var elementType = node.Elements.Select(e => Context.ExpressionTypes.TryGetValue(e, out var type) ? type : null).FirstOrDefault(t => t != null) ?? TypeConversions.GetVectorElementType(ResolveContextualType(node));

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

        if (invocation.Symbol is MethodSymbol method && method.ReturnType is { } returnType)
            Context.ExpressionTypes[node] = returnType;
    }

    [Visitor]
    public void Visit(InstanceCreationExpresssionNode node)
    {
        if (Context.GetSymbol(node.TypeName).Symbol is not TypeSymbol type || (type.KnownType != TypeSymbol.DefaultType.Struct && type.KnownType != TypeSymbol.DefaultType.Class))
            return;

        if (node.TypeArguments is { Count: > 0 } typeArguments)
        {
            if (TypeResolver.Construct(Context, node, type, typeArguments) is not { } constructed)
                return;

            type = constructed;
        }

        Context.ExpressionTypes[node] = type;

        var constructors = type.Members.OfType<MethodSymbol>().Where(m => m.Kind == MethodSymbol.MethodKind.Constructor).ToArray();

        Context.Bind(node, OverloadResolver.Resolve(Context, new CallExpressionNode(node.TypeName, new TypeArgumentListNode([]), node.Arguments), constructors));
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
                PropertySymbol property => property.Type,
                ParameterSymbol parameter => parameter.Type,
                _ => null
            }) as ArrayTypeSymbol;

        return array != null;
    }
}
