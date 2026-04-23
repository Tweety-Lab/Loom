
namespace Loom.Parser.Rules;

/// <summary>
/// Registers a <see cref="ParserRule{T}"/> into the <see cref="LoomParser"/> as a parser rule.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ParserRuleAttribute : Attribute { }