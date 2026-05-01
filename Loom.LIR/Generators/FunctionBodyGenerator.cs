using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;
using Loom.Parser.Tokenizer;

namespace Loom.LIR.Passes;

/// <summary>
/// Visits method bodies and emits Loom Intermediate Representation (LIR) instructions.
/// </summary>
internal class FunctionBodyGenerator : ASTVisitor
{
    public LIRSemanticContext SemanticContext { get; private set; }

    /// <summary> The value stack used during expression emission. </summary>
    public Stack<LIRValue> ValueStack { get; } = new();

    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; }

    /// <summary> The owning <see cref="LIRCompilationUnit"/>. </summary>
    public LIRCompilationUnit CompilationUnit { get; }

    /// <summary> The current <see cref="LIRGenerator"/>. </summary>
    public LIRGenerator? IL { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="FunctionBodyGenerator"/> class. </summary>
    public FunctionBodyGenerator(AnalysisContext context, LIRCompilationUnit compilationUnit, LIRSemanticContext semanticContext)
    {
        Context = context;
        CompilationUnit = compilationUnit;
        SemanticContext = semanticContext;
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        var method = (Context.GetSymbol(node).Symbol as MethodDefinitionSymbol)!;
        LIRFunction? func = CompilationUnit.GetFunction(method.FullyQualifiedName);
        IL = func.LIRGenerator;

        foreach (var (symbol, value) in method.Parameters.Zip(func.ParameterValues))
            SemanticContext.LocalVariables[symbol] = value;

        VisitChildren(node);
    }

    [Visitor]
    public void Visit(ReturnStatementNode node)
    {
        if (node.Expression != null)
        {
            Dispatch(node.Expression);
            IL!.Emit(LIROpCode.Return, null, ValueStack.Pop());
        }
        else
        {
            IL!.Emit(LIROpCode.Return);
        }
    }

    [Visitor]
    public void Visit(NumberLiteralNode node) => ValueStack.Push(new LIRConstantIntValue(int.Parse(node.Value)));

    [Visitor]
    public void Visit(BooleanLiteralNode node) => ValueStack.Push(new LIRConstantBoolValue(bool.Parse(node.Value)));

    [Visitor]
    public void Visit(BinaryExpressionNode node)
    {
        Dispatch(node.Left);
        Dispatch(node.Right);

        var right = ValueStack.Pop();
        var left = ValueStack.Pop();

        var opCode = node.Operator.Type switch
        {
            Token.TokenType.Plus => LIROpCode.Add,
            Token.TokenType.Star => LIROpCode.Mul,
            Token.TokenType.Minus => LIROpCode.Sub,
            Token.TokenType.Slash => LIROpCode.Div,
            _ => throw new Exception($"Unsupported operator: {node.Operator.Type}")
        };

        var result = IL!.Emit(opCode, LIRType.Int32, left, right);
        ValueStack.Push(result!);
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        var method = (Context.GetSymbol(node.MethodName).Symbol as MethodDefinitionSymbol)!;

        foreach (var argument in node.Arguments)
            Dispatch(argument);

        var arguments = node.Arguments.Select(_ => ValueStack.Pop()).Reverse().ToArray();

        var result = IL!.Emit(LIROpCode.Call, LIRType.Int32, [SemanticContext.Functions[method], .. arguments]);
        ValueStack.Push(result!);
    }

    [Visitor]
    public void Visit(IdentifierNameNode node)
    {
        var symbol = Context.GetSymbol(node).Symbol;

        if (symbol is ParameterSymbol parameter)
        {
            ValueStack.Push(SemanticContext.LocalVariables[parameter]);
        }
        else if (symbol is LocalVariableSymbol local)
        {
            var result = IL!.Emit(LIROpCode.Load, LIRType.Int32, SemanticContext.LocalVariables[local]);
            ValueStack.Push(result!);
        }
    }

    [Visitor]
    public void Visit(VariableDeclarationNode node)
    {
        var symbol = (Context.GetSymbol(node).Symbol as LocalVariableSymbol)!;
        var ptr = IL!.Emit(LIROpCode.Alloca, LIRType.Int32);

        SemanticContext.LocalVariables[symbol] = ptr!;

        Dispatch(node.Initializer);
        var value = ValueStack.Pop();

        IL.Emit(LIROpCode.Store, null, value, ptr);
    }

    [Visitor]
    public void Visit(AssignmentStatementNode node)
    {
        Dispatch(node.Value);
        var value = ValueStack.Pop();
        IL!.Emit(LIROpCode.Store, null, value, SemanticContext.LocalVariables[(Context.GetSymbol(node.Target).Symbol as LocalVariableSymbol)!]); // TODO
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);
}
