using Loom.Parser.AST;

namespace Loom.Parser.Rules.Default;

/// <summary> Parses a statement-based body. Member declarations are not valid here. </summary>
[ParserRule]
public class MethodBlockRule : BlockRule
{
    /// <inheritdoc/>
    public MethodBlockRule(LoomParser parser) : base(parser) { }
}