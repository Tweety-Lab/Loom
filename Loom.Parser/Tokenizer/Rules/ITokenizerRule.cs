
namespace Loom.Parser.Tokenizer.Rules;

public interface ITokenizerRule
{
    /// <summary> Checks if the current character can be handled by the rule. </summary>
    bool CanHandle(char current);

    /// <summary> Reads the token from the tokenizer. </summary>
    Token Read(StringReader reader);
}
