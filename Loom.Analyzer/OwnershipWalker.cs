using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// The ownership state of a unique smart pointer at a point in the program.
/// </summary>
internal enum OwnershipState
{
    Owned,
    Moved
}

/// <summary>
/// Walks the AST and enforces ownership rules for smart pointers.
/// </summary>
internal class OwnershipWalker : ASTVisitor
{
    public static DiagnosticDescription UseOfMovedValue = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Cannot use moved value '{0}'.");
    public static DiagnosticDescription CannotMoveMember = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Cannot transfer ownership of member '{0}' because member access borrows its value.");
    public static DiagnosticDescription CannotMoveBorrow = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Cannot transfer ownership of borrowed parameter '{0}'.");
    public static DiagnosticDescription CannotMakeSharedUnique = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Cannot transfer a shared reference into unique ownership.");

    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; private set; }

    private readonly Dictionary<Symbol, OwnershipState> states = new();
    private MethodSymbol? currentFunction;

    /// <summary> Initializes a new instance of the <see cref="OwnershipWalker"/> class. </summary>
    public OwnershipWalker(AnalysisContext context) => Context = context;

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);

    [Visitor]
    public void Visit(MethodDeclarationNode node) => VisitFunction(node);

    [Visitor]
    public void Visit(ConstructorDeclarationNode node) => VisitFunction(node);

    [Visitor]
    public void Visit(DestructorDeclarationNode node) => VisitFunction(node);

    [Visitor]
    public void Visit(OperatorDeclarationNode node) => VisitFunction(node);

    [Visitor]
    public void Visit(AccessorNode node) => VisitFunction(node);

    private void VisitFunction(ASTNode node)
    {
        states.Clear();
        currentFunction = Context.GetSymbol(node).As<MethodSymbol>();
        VisitChildren(node);

        foreach (var (place, state) in states)
            if (state == OwnershipState.Moved)
                Context.MovedValues.Add(place);
    }

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node)
    {
        if (ResolvePlace(node) is not { } local || local.PointerType == PointerType.None)
        {
            VisitChildren(node);
            return;
        }

        MoveOut(node.Variable.Initializer, local.PointerType);
        if (local.PointerType == PointerType.Unique)
            states[local] = OwnershipState.Owned;
    }

    [Visitor]
    public void Visit(MemberAccessExpressionNode node) => UseWithoutMove(node.Receiver);

    [Visitor]
    public void Visit(AssignmentStatementNode node)
    {
        MoveOut(node.Value, ExpressionPointerType(node.Target));

        if (node.Target is IdentifierNameNode target && ResolvePlace(target) is { } place)
            states[place] = OwnershipState.Owned;
        else
            Dispatch(node.Target);
    }

    [Visitor]
    public void Visit(ReturnStatementNode node)
    {
        if (node.Expression == null)
            return;

        if (node.Expression is IdentifierNameNode && Context.GetSymbol(node.Expression).Symbol is ParameterSymbol { IsBorrow: true, Type: { IsValueType: false } } parameter)
        {
            Context.DiagnosticContext?.Report(CannotMoveBorrow, node.Expression.Span.StartToken?.Location, parameter.Name);
            return;
        }

        MoveOut(node.Expression, currentFunction?.ReturnPointerType ?? PointerType.None);
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        UseWithoutMove(node.Callee);
        VisitArguments(node.Arguments, Context.GetSymbol(node).As<MethodSymbol>()?.Parameters);
    }

    [Visitor]
    public void Visit(InstanceCreationExpresssionNode node) => VisitArguments(node.Arguments, Context.GetSymbol(node).As<MethodSymbol>()?.Parameters);

    [Visitor]
    public void Visit(BinaryExpressionNode node)
    {
        if (Context.GetSymbol(node).Symbol is MethodSymbol { Kind: MethodSymbol.MethodKind.Operator } method)
            VisitArguments([node.Left, node.Right], method.Parameters);
        else
        {
            UseWithoutMove(node.Left);
            UseWithoutMove(node.Right);
        }
    }

    [Visitor]
    public void Visit(ArrayAccessExpressionNode node)
    {
        if (Context.GetSymbol(node).Symbol is MethodSymbol { Kind: MethodSymbol.MethodKind.Operator } method)
            VisitArguments([node.Receiver, node.Index], method.Parameters);
        else
        {
            UseWithoutMove(node.Receiver);
            Dispatch(node.Index);
        }
    }

    private void VisitArguments(IReadOnlyList<ExpressionNode> arguments, IReadOnlyList<ParameterSymbol>? parameters)
    {
        for (int index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            var parameter = parameters is not null && index < parameters.Count ? parameters[index] : null;

            if (parameter is { IsBorrow: true })
                UseWithoutMove(argument);
            else
                MoveOut(argument, parameter?.PointerType ?? PointerType.None);
        }
    }

    private void MoveOut(ExpressionNode expr, PointerType destination = PointerType.None)
    {
        if (ExpressionPointerType(expr) == PointerType.Shared)
        {
            if (destination == PointerType.Unique)
                Context.DiagnosticContext?.Report(CannotMakeSharedUnique, expr.Span.StartToken?.Location);

            UseWithoutMove(expr);
            return;
        }

        if (BorrowedMember(expr) is { } member)
        {
            Context.DiagnosticContext?.Report(CannotMoveMember, expr.Span.StartToken?.Location, member.Name);
            UseWithoutMove(expr);
            return;
        }

        if (expr is IdentifierNameNode identifier && ResolvePlace(identifier) is { } place)
        {
            if (TryGetState(identifier, out var state) && state == OwnershipState.Moved)
                Context.DiagnosticContext?.Report(UseOfMovedValue, identifier.Span.StartToken?.Location, identifier.BaseName);
            else
                states[place] = OwnershipState.Moved;
        }
        else
            Dispatch(expr);
    }

    private PointerType ExpressionPointerType(ExpressionNode expr) => Context.GetSymbol(expr).Symbol switch
    {
        PropertySymbol property => property.PointerType,
        FieldSymbol field => field.PointerType,
        LocalVariableSymbol local => local.PointerType,
        ParameterSymbol parameter => parameter.PointerType,
        MethodSymbol method when expr is CallExpressionNode => method.ReturnPointerType,
        _ => PointerType.None
    };

    private Symbol? BorrowedMember(ExpressionNode expr)
    {
        if (expr is not (IdentifierNameNode or MemberAccessExpressionNode))
            return null;

        var member = Context.GetSymbol(expr).Symbol;
        var type = member switch
        {
            FieldSymbol field => field.Type,
            PropertySymbol property => property.Type,
            _ => null
        };

        return type is { KnownType: TypeSymbol.DefaultType.Class or TypeSymbol.DefaultType.Interface } ? member : null;
    }

    private void UseWithoutMove(ExpressionNode expr)
    {
        if (expr is IdentifierNameNode identifier && ResolvePlace(identifier) is { } place)
        {
            if (TryGetState(identifier, out var state) && state == OwnershipState.Moved)
                Context.DiagnosticContext?.Report(UseOfMovedValue, identifier.Span.StartToken?.Location, identifier.BaseName);
        }
        else
            Dispatch(expr);
    }

    private LocalVariableSymbol? ResolvePlace(ASTNode declaration) => Context.GetSymbol(declaration).Symbol as LocalVariableSymbol;

    private LocalVariableSymbol? ResolvePlace(IdentifierNameNode node)
    {
        var symbol = Context.GetSymbol(node).Symbol as LocalVariableSymbol;
        return symbol != null && symbol.PointerType == PointerType.Unique ? symbol : null;
    }

    private bool TryGetState(IdentifierNameNode node, out OwnershipState state)
    {
        if (ResolvePlace(node) is { } place && states.TryGetValue(place, out state))
            return true;

        state = default;
        return false;
    }
}
