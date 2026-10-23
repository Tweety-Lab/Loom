namespace Loom.Parser.Tokenizer.Rules.Default;

[TokenizerRule]
public class DocumentCommentTokenRule : TokenizerRule
{
    /// <inheritdoc/>
    public override bool CanHandle(char current) => current == '/' && Reader.Peek(1) == '/' && Reader.Peek(2) == '/';

    /// <inheritdoc/>
    public override Token Read()
    {
        var start = Reader.Position;
        var location = new TokenLocation(Reader.Line, Reader.Column);

        while (!Reader.IsEnd && Reader.PeekChar() is not '\r' and not '\n')
            Reader.Read();

        return new Token(Token.TokenType.DocumentComment, Reader.Source[start..Reader.Position], location);
    }
}
