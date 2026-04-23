
namespace Loom.Parser.Tokenizer.Rules;

/// <summary>
/// Registers a <see cref="ITokenizerRule"/> into the <see cref="LoomTokenizer"/> as a tokenizer rule.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class TokenizerRuleAttribute : Attribute { }
