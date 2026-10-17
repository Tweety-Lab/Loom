using System.Diagnostics.CodeAnalysis;

namespace Loom.Parser.AST.Rules.Default;

/// <summary>
/// Base class for rules that automatically get parsed as members of a type.
/// </summary>
public abstract class TypeMemberParserRule : ParserRule<ASTNode>
{
    /// <inheritdoc/>
    protected TypeMemberParserRule(LoomParser parser) : base(parser) { }
}

/// <summary>
/// Parses the body of a type, whose members can be methods, fields, properties, or nested structs.
/// </summary>
[ParserRule]
public class TypeDeclarationBlockRule : BlockRule
{
    /// <inheritdoc/>
    public TypeDeclarationBlockRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    protected override bool TryParseMember([NotNullWhen(true)] out ASTNode? node)
    {
        foreach (var rule in Parser.GetRules<TypeMemberParserRule>())
            if (TryRunRule(rule, out node))
                return true;

        node = null;
        return false;
    }
}
