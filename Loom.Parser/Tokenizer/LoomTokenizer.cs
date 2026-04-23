using Loom.Common.Reflection;
using Loom.Parser.Tokenizer.Rules;
using System.Reflection;

namespace Loom.Parser.Tokenizer;

public class LoomTokenizer
{
    /// <summary> The <see cref="LoomStringReader"/> used to read the source code. </summary>
    public LoomStringReader Reader { get; set; }

    /// <summary> The currently parsed Tokens. </summary>
    public List<Token> Tokens { get; } = new();

    /// <summary> All registered rules the tokenizer uses. </summary>
    public List<ITokenizerRule> Rules
    {
        get
        {
            if (field != null)
                return field;

            field = LoomReflection.InstansiateAllWithAttribute<TokenizerRuleAttribute>(Assembly.GetExecutingAssembly()).Cast<ITokenizerRule>().ToList();

            return field;
        }
    }

    /// <summary> Initializes a new instance of the <see cref="LoomTokenizer"/> class. </summary>
    public LoomTokenizer(string source) => Reader = new LoomStringReader(source);

    /// <summary> Tokenizes the given source code. </summary>
    public List<Token> Tokenize()
    {
        while (!Reader.IsEnd)
        {
            var current = Reader.PeekChar();

            if (char.IsWhiteSpace(current))
            {
                Reader.Read();
                continue;
            }

            if (current == '/' && Reader.Peek(1) == '/')
            {
                while (Reader.Peek() != '\n' && !Reader.IsEnd)
                    Reader.Read();

                continue;
            }

            var matched = false;

            foreach (var rule in Rules)
                if (rule.CanHandle(current))
                {
                    Tokens.Add(rule.Read(Reader));
                    matched = true;
                    break;
                }

            if (!matched)
                Reader.Read();
        }

        Tokens.Add(new Token(Token.TokenType.EOF, ""));
        return Tokens;
    }
}
