using Loom.Parser.AST;
using Loom.Parser.Tokenizer;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record ImportNode(IdentifierNameNode ModuleName) : ASTNode
{
    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => [ModuleName];
}

[ParserRule]
public class ImportRule : ParserRule<ImportNode>
{
    /// <inheritdoc/>
    public ImportRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ImportNode ParseNode()
    {
        Parser.Reader.Expect(TokenType.Import); // import
        var moduleName = Parser.Reader.ExpectQualifiedName(); // name
        Parser.Reader.Expect(TokenType.Semicolon); // ;

        return new ImportNode(new IdentifierNameNode(moduleName));
    }
}
