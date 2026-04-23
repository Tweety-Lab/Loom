
using Loom.Common.Reflection;
using Loom.Parser.Tokenizer.Rules;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Loom.Parser.Tokenizer;

public class LoomTokenizer
{
    public StringReader Reader { get; set; }

    /// <summary> The currently parsed Tokens. </summary>
    public List<Token> Tokens { get; } = new();

    /// <summary> All registered rules the tokenizer uses. </summary>
    public List<ITokenizerRule> Rules
    {
        get
        {
            if (field != null)
                return field;

            field = new List<ITokenizerRule>();
            List<Type> types = LoomReflection.GetTypesWithAttribute<TokenizerRuleAttribute>(Assembly.GetExecutingAssembly()).ToList();
            foreach (var type in types)
            {
                var rule = (ITokenizerRule)Activator.CreateInstance(type)!;
                field.Add(rule);
            }

            return field;
        }
    }

    /// <summary> Initializes a new instance of the <see cref="LoomTokenizer"/> class. </summary>
    public LoomTokenizer(string source) => Reader = new StringReader(source);

    /// <summary> Tokenizes the given source code. </summary>
    public List<Token> Tokenize()
    {
        while (Reader.Peek() != -1)
        {
            if (char.IsWhiteSpace((char)Reader.Peek()))
            {
                Reader.Read();
                continue;
            }

            var current = (char)Reader.Peek();
            var matched = false;

            foreach (var rule in Rules)
            {
                if (rule.CanHandle(current))
                {
                    Tokens.Add(rule.Read(Reader));
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                
                Reader.Read();
            }
        }

        Tokens.Add(new Token(Token.TokenType.EOF, ""));
        return Tokens;
    }
}
