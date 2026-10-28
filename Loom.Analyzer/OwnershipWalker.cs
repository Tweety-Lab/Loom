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
    public static Diagnostic UseOfMovedValue = new("LM0047", Diagnostic.DiagnosticLevel.Error, "Cannot use moved value '{0}'.");

    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; private set; }

    private readonly Dictionary<Symbol, OwnershipState> states = new();

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
        VisitChildren(node);

        foreach (var (place, state) in states)
            if (state == OwnershipState.Moved)
                Context.MovedValues.Add(place);
    }

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node)
    {
        if (ResolvePlace(node) is not { } local || local.PointerType != PointerType.Unique)
        {
            VisitChildren(node);
            return;
        }

        MoveOut(node.Variable.Initializer);
        states[local] = OwnershipState.Owned;
    }

    [Visitor]
    public void Visit(MemberAccessExpressionNode node) => UseWithoutMove(node.Receiver);

    [Visitor]
    public void Visit(AssignmentStatementNode node)
    {
        MoveOut(node.Value);

        if (node.Target is IdentifierNameNode target && ResolvePlace(target) is { } place)
            states[place] = OwnershipState.Owned;
        else
            Dispatch(node.Target);
    }

    [Visitor]
    public void Visit(ReturnStatementNode node)
    {
        if (node.Expression != null)
            MoveOut(node.Expression);
    }

    [Visitor]
    public void Visit(CallExpressionNode node) => VisitArguments(node.Arguments, Context.GetSymbol(node).As<MethodSymbol>()?.Parameters);

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
                MoveOut(argument);
        }
    }

    private void MoveOut(ExpressionNode expr)
    {
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
