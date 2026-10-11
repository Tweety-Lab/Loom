# Loom
![MIT](https://img.shields.io/badge/License-MIT-blue)

Loom is an object-oriented systems programming language that combines the performance and portability of low-level languages with the developer experience of high-level languages.

```Loom
import Standard::IO;
import Standard::Memory;

module Standard::Program
{
    /// @brief Classes are reference types and are interfaced with through Loom's ownership and memory model (smart pointers)!
    export class Object
    {
        public i32 MutableField = default;
    }

    /// @brief Program Entry Point
    export i32 Main()
    {
        // A Unique pointer to an object can only have one owner at a time
        unique Object obj = new Object();
        SetFieldFromMutableBorrow(obj, 10);

        // 'Slice' doesn't need to use pointers because it's a value type (struct)
        // Value types use copy semantics and typically live on the stack
        Slice myString = "Hello!";
        println(myString); // "Hello!"

        // Loom also allows unsafe operations inside an 'unsafe' block via the 'Standard::Memory::Unsafe' class
        unsafe
        {
            char[3] myArray = ['a', 'b', 'c'];
            iptr addr = Unsafe.AddressOf<char[3]>(myArray);

            char letter = Unsafe.Read<char>(addr + sizeof(char) * 2);
            println(letter); // "c"
        }
        
        return GetFieldFromBorrow(obj);
    }

    /// @brief 'ref' is used to borrow a reference to an object without claiming ownership.
    export i32 GetFieldFromBorrow(ref Object input)
    {
        return input.MutableField;
    }

	/// @brief 'ref mut' is used to borrow a mutatable reference to an object without claiming ownership.
    export void SetFieldFromMutableBorrow(ref mut Object input, i32 val)
    {
        input.MutableField = val;
    }
}

module Standard::IO
{
    extern void putchar(char c);

    /// @brief Prints a char slice to the console and appends a newline.
    export void println(Slice s)
    {
        for (i32 i = 0; i < s.Length; i = i + 1)
            putchar(s.ElementAt<char>(i));

        putchar('\n');
    }

    /// @brief Prints a char to the console and appends a newline.
    export void println(char c)
    {
        putchar(c);
        putchar('\n');
    }
}
```

## Compiler Architecture
The Loom Compiler works via a Linear Pipeline system where each step in the pipeline mutates a compilation context using data provided by the previous steps.

### Loom.Parser
The first step is the Parser which takes raw source code, tokenizes it, and converts it into an in-memory [Abstract Syntax Tree](https://en.wikipedia.org/wiki/Abstract_syntax_tree) (AST). The Loom parser uses modular 'Rules' to determine how to parse tokens and syntax.
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
META Program:
META Strings:
constant [14 x char] @.str.0 = ['S', 't', 'a', 'c', 'k', ' ', 'm', 'u', 't', 'a', 't', 'i', 'o', 'n']
constant [6 x char] @.str.1 = ['H', 'e', 'l', 'l', 'o', '!']

class Standard::Program::Object
  field i32 MutableField

  define Standard::Program::Object::.ctor(Standard::Program::Object* %self) -> void
    entry:
      %0 = getfield i32 %self, MutableField
      store i32 0, %0
      return

  }

}

define Standard::Program::Main.() -> i32
  entry:
    %0 = alloca i32
    store i32 1, %0
    %1 = alloca Standard::Program::Object*
    %2 = call Standard::Program::Object* @Standard::Program::GetClass.
    store Standard::Program::Object* %2, %1
    %3 = alloca Standard::Memory::Slice
    %4 = getfield iptr %3, pointer
    %5 = ptrtoint iptr @.str.0
    store iptr %5, %4
    %6 = getfield i32 %3, Length
    store i32 14, %6
    %7 = load Standard::Memory::Slice, %3
    call @Standard::IO::println.Standard::Memory::Slice, %7
    %8 = alloca i32
    %9 = load Standard::Program::Object*, %1
    %10 = call i32 @Standard::Program::GetFieldFromBorrow.Standard::Program::Object, %9
    %11 = add i32 %10, 10
    store i32 %11, %8
    %12 = load Standard::Program::Object*, %1
    %13 = load i32, %8
    call @Standard::Program::SetFieldFromMutableBorrow.Standard::Program::Object_i32, %12, %13
    %14 = alloca Standard::Memory::Slice
    %15 = alloca Standard::Memory::Slice
    %16 = getfield iptr %15, pointer
    %17 = ptrtoint iptr @.str.1
    store iptr %17, %16
    %18 = getfield i32 %15, Length
    store i32 6, %18
    %19 = load Standard::Memory::Slice, %15
    store Standard::Memory::Slice %19, %14
    %20 = load Standard::Memory::Slice, %14
    call @Standard::IO::println.Standard::Memory::Slice, %20
    %21 = load i32, %8
    %22 = load Standard::Program::Object*, %1
    %23 = ptrtoint iptr %22
    call @Standard::Memory::Unsafe::Free.iptr, %23
    return %21

}
```

LIR is flattened, but still relatively high level. Unlike LLVM, concepts akin to classes and structs are retained and all code remains platform-agnostic.

### Loom.CodeGen
The constructed LIR is then passed into a `Loom.CodeGen.*` project. The default implementation uses [LLVM](https://llvm.org/docs/LangRef.html) to support extreme portability and code optimisation.
