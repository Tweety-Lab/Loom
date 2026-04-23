using Loom.Common.Exceptions;
using Loom.Parser.AST;
using Loom.Parser.Tokenizer;

using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Rules.Default;

public class ProgramRule : ParserRule<ProgramNode>
{
    /// <inheritdoc/>
    public ProgramRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    public override ProgramNode Parse()
    {
        var imports = new List<ImportNode>();
        var modules = new List<ModuleNode>();

        while (!Parser.Reader.Check(TokenType.EOF))
        {
            if (Parser.Reader.Check(TokenType.Import))
                imports.Add(RunRule<ImportRule, ImportNode>()); // imports
            else if (Parser.Reader.Check(TokenType.Module))
                modules.Add(RunRule<ModuleRule, ModuleNode>()); // modules
            else
                throw new LoomException($"Unexpected token: {Parser.Reader.Peek().Value}");
        }

        return new ProgramNode(imports, modules);
    }
}
