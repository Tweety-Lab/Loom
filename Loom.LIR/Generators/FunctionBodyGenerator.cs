using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Builders;
using Loom.LIR.Generators;
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
    /// <summary> The value stack used during expression emission. </summary>
    public Stack<LIRValue> ValueStack { get; } = new();

    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; }

    /// <summary> The owning <see cref="CompilationUnitBuilder"/>. </summary>
    public CompilationUnitBuilder UnitBuilder { get; }

    /// <summary> The current <see cref="LIRGenerator"/>. </summary>
    public LIRGenerator? IL { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="FunctionBodyGenerator"/> class. </summary>
    public FunctionBodyGenerator(AnalysisContext context, CompilationUnitBuilder unitBuilder)
    {
        Context = context;
        UnitBuilder = unitBuilder;
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        var method = (Context.ResolveSymbol(node).Symbol as MethodDefinitionSymbol)!;
        IL = UnitBuilder.GetFunction(method.FullyQualifiedName).LIRGenerator;
        VisitChildren(node);
    }

    [Visitor]
    public void Visit(ReturnStatementNode node)
    {
        if (node.Expression != null)
        {
            Dispatch(node.Expression);
            IL!.Emit(LIROpCode.Ret, ValueStack.Pop());
        }
        else
        {
            IL!.Emit(LIROpCode.Ret);
        }
    }

    [Visitor]
    public void Visit(NumberLiteralNode node) => ValueStack.Push(new LIRConstantValue(int.Parse(node.Value)));

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

        var result = IL!.Emit(opCode, left, right);
        ValueStack.Push(result!);
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        var method = (Context.ResolveSymbol(node.MethodName).Symbol as MethodDefinitionSymbol)!;
        var builder = UnitBuilder.GetFunction(method.FullyQualifiedName);
        var result = IL!.Emit(LIROpCode.Call, builder.Build());
        ValueStack.Push(result!);
    }

    [Visitor]
    public void Visit(IdentifierNameNode node)
    {
        var symbol = Context.ResolveSymbol(node).Symbol;
        if (symbol is LocalVariableSymbol local)
        {
            var result = IL!.Emit(LIROpCode.Load, new LIRTempValue($"{local.Name}"));
            ValueStack.Push(result!);
        }
    }

    [Visitor]
    public void Visit(VariableDeclarationNode node)
    {
        Dispatch(node.Initializer);
        var value = ValueStack.Pop();
        IL!.Emit(LIROpCode.Alloca, new LIRTempValue($"{node.Name.Text}"));
        IL!.Emit(LIROpCode.Store, new LIRTempValue($"{node.Name.Text}"), value);
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);
}
