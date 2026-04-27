
namespace Loom.Parser.Tokenizer.Rules;

/// <summary>
/// Registers a <see cref="TokenizerRule"/> into the <see cref="LoomTokenizer"/> as a tokenizer rule.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TokenizerRuleAttribute : Attribute { }
