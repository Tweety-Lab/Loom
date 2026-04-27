using Loom.Parser.Rules.Default;
using Loom.Parser.Tokenizer;

namespace Loom.Parser.AST;

public abstract record NameNode : ExpressionNode;
public record IdentifierNameNode(Token Token) : NameNode { }