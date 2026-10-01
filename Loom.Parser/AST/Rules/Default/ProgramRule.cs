using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.AST.Rules.Default;

public record ProgramNode(List<ImportNode> Imports, List<ModuleNode> Modules) : ASTNode
{
    /// <summary> The name of the program. </summary>
    public string Name { get; set; } = "Program";

    /// <inheritdoc/>
    public override IEnumerable<ASTNode> Children => Imports.Cast<ASTNode>().Concat(Modules.Cast<ASTNode>());
}

public class ProgramRule : ParserRule<ProgramNode>
{
    /// <inheritdoc/>
    public ProgramRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ProgramNode ParseNode()
    {
        var imports = new List<ImportNode>();
        var modules = new List<ModuleNode>();

        ParseUntil(TokenType.EOF, new()
        {
            [TokenType.Import] = () => imports.Add(RunRule<ImportRule, ImportNode>()), // Imports
            [TokenType.Module] = () => modules.Add(RunRule<ModuleDeclarationRule, ModuleNode>()), // Modules
        });

        return new ProgramNode(imports, modules);
    }
}
