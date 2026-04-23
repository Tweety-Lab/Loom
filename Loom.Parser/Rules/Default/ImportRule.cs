using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

[ParserRule]
public class ImportRule : ParserRule<ImportNode>
{
    /// <inheritdoc/>
    public ImportRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ImportNode Parse()
    {
        Parser.Reader.Expect(TokenType.Import); // import
        var moduleName = Parser.Reader.Expect(TokenType.Identifier).Value; // name
        Parser.Reader.Expect(TokenType.Semicolon); // ;
        return new ImportNode(moduleName);
    }
}
