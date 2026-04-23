using Loom.Parser.AST;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public record ProgramNode(List<ImportNode> Imports, List<ModuleNode> Modules) : ASTNode
{
    /// <inheritdoc/>
    public override void Accept(ASTVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Imports.Cast<ASTNode>().Concat(Modules.Cast<ASTNode>());
}

public class ProgramRule : ParserRule<ProgramNode>
{
    /// <inheritdoc/>
    public ProgramRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ProgramNode Parse()
    {
        var imports = new List<ImportNode>();
        var modules = new List<ModuleNode>();

        ParseUntil(TokenType.EOF, new()
        {
            [TokenType.Import] = () => imports.Add(RunRule<ImportRule, ImportNode>()), // Imports
            [TokenType.Module] = () => modules.Add(RunRule<ModuleRule, ModuleNode>()), // Modules
        });

        return new ProgramNode(imports, modules);
    }
}
