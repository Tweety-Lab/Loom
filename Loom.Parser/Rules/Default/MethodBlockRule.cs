using Loom.Parser.AST;

namespace Loom.Parser.Rules.Default;

/// <summary>
/// Parses a statement-based body.
/// </summary>
[ParserRule]
public class MethodBlockRule : BlockRule
{
    /// <inheritdoc/>
    public MethodBlockRule(LoomParser parser) : base(parser) { }
}