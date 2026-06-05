# Loom
![MIT](https://img.shields.io/badge/License-MIT-blue)

Loom is an object-oriented systems programming language that combines the control, performance, and portability of low-level languages with the developer experience of high-level languages.

```Loom
module MyModule
{
    // Classes are reference types that are allocated on the heap and are interacted with via the Loom smart pointer memory model
    export class MyClass
    {
        // Properties are wrappers for fields that run custom logic when getting or setting.
        public i32 Value { get; set; }

        public MyClass(i32 value)
        {
            Value = value;
        }
    }

    // Structs are value types that are allocated on the stack and reconstructed between scopes
    export struct MyStruct
    {
        public i32 Value { get; set; }
    }

    // Returning reference type + no pointer type = error!
    export MyClass ReturnImplicitOwnedRefType()
    {
        MyClass obj = new MyClass(); // No pointer type + reference type = error!
        return obj;
    }

    export MyStruct ReturnStackAllocatedValueType()
    {
        MyStruct obj = new MyStruct(); // No pointer type + value type = stack alloc
        return obj;
    }

    export unique MyClass ReturnUniqueRefType()
    {
        unique MyClass obj = new MyClass(); // Unique pointer type = heap alloc
        return obj;
    }

    // Ref = Read from unique pointer without taking ownership
    export i32 BorrowUniqueRef(ref MyClass obj)
    {
        return obj.Value;
    }

    // Ref Mut = Read/Write from unique pointer without taking ownership
    export i32 BorrowMutableUniqueRef(ref mut MyClass obj)
    {
        obj.Value = 100;
        return obj.Value;
    }


    unsafe
    {
        export raw MyClass ReturnRaw()
        {
            raw MyClass obj = new MyClass();
            return obj;
        }
    }
}
```

## Compiler Architecture
The Loom Compiler works via a Linear Pipeline system where each step in the pipeline mutates a compilation context using data provided by the previous steps.

### Loom.Parser
The first step is the Parser which takes raw source code, tokenizes it, and converts it into an in-memory [Abstract Syntax Tree](https://en.wikipedia.org/wiki/Abstract_syntax_tree) (AST). The Loom parser uses modular 'Rules` to determine how to parse tokens and syntax.
```csharp
public record ReturnStatementNode(ExpressionNode? Expression = null) : StatementNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Expression is not null ? new[] { Expression } : Enumerable.Empty<ASTNode>();
}

[ParserRule]
public class ReturnStatementRule : ParserRule<ReturnStatementNode>
{
    /// <inheritdoc/>
    public ReturnStatementRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ReturnStatementNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Return); // return

        if (Parser.Reader.Check(TokenType.Semicolon))
            return new ReturnStatementNode();

        return new ReturnStatementNode(RunRule<ExpressionRule, ExpressionNode>());
    }
}
```

### Loom.Analyzer
The analyzer walks the AST and maps its nodes to Symbols, which serve two purposes: enforcing language rules through semantic analysis, and driving LIR compilation in the next step.

```csharp
/// <summary>
/// Checks for imports that do not exist.
/// </summary>
[LoomAnalyzer]
public class UnresolvedImportAnalyzer : Analyzer
{
    public static Diagnostic UnresolvedImportDiagnostic = new(Diagnostic.DiagnosticLevel.Error, "The module '{0}' could not be resolved.");

    [Visitor]
    public void Visit(ImportNode node)
    {
        ModuleSymbol? symbol = Context.GetSymbol(node.ModuleName).Symbol as ModuleSymbol;

        if (symbol == null)
            Context.DiagnosticContext?.Report(UnresolvedImportDiagnostic, node.ModuleName.BaseName);
    }
}
```

### Loom.LIR
Loom then compiles its AST higher representation down to a linear [Intermediate Representation](https://en.wikipedia.org/wiki/Intermediate_representation) called LIR. This lowered form makes cross-target compilation very easy, the same LIR can target both LLVM and .NET.

```LIR
[Name: Test]

define Consumer::Main() -> i32 {
entry:
  %0 = alloca i32
  %1 = call i32 @Consumer::Add, 2, 2
  store i32 %1, %0
  %2 = load i32, %0
  return %2

}


define Consumer::Add(i32 %a, i32 %b) -> i32 {
entry:
  %0 = alloca i32
  store i32 %a, %0
  %1 = alloca i32
  store i32 %b, %1
  %2 = load i32, %0
  %3 = load i32, %1
  %4 = add i32 %2, %3
  return %4

}
```

### Loom.CodeGen
The constructed LIR is then passed into a `Loom.CodeGen.*` project. The default implementation uses [LLVM](https://llvm.org/docs/LangRef.html) to support extreme portability and code optimisation.
