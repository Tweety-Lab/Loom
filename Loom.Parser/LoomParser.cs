using Loom.Common.Reflection;
using Loom.Parser.AST;
using Loom.Parser.Rules;
using Loom.Parser.Rules.Default;
using Loom.Parser.Tokenizer;
using System.Reflection;

namespace Loom.Parser;

public class LoomParser
{
    /// <summary> The underlying <see cref="TokenReader"/>. />
    public TokenReader Reader { get;  }

    /// <summary> All registered rules the parser uses. </summary>
    public List<IParserRule> Rules
    {
        get
        {
            if (field != null)
                return field;

            field = LoomReflection.InstansiateAllWithAttribute<ParserRuleAttribute>(Assembly.GetExecutingAssembly(), this).OfType<IParserRule>().ToList();

            return field;
        }
    }

    /// <summary> Initializes a new instance of the <see cref="LoomParser"/> class. </summary>
    public LoomParser(List<Token> tokens) => Reader = new TokenReader(tokens);

    /// <summary> Gets the rule for the given type. </summary>
    public T GetRule<T>() where T : IParserRule => (T)Rules.First(x => x.GetType() == typeof(T));

    /// <summary> Parses the given source code into a root <see cref="ProgramNode"/>. </summary>
    public ProgramNode ParseProgram() => new ProgramRule(this).Parse();
}
