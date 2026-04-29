
using Loom.Parser.AST;

namespace Loom.Analyzer.Symbols;

public abstract record Symbol(string Name)
{
    /// <summary> The fully qualified name of this symbol, i.e. "MyModule::MyMethod". </summary>
    public string FullyQualifiedName { get; set; } = Name;
    

    /// <summary> The <see cref="ASTNode"/> that declared this symbol. </summary>
    public ASTNode? DeclaringNode { get; set; }
}
