using Loom.Common.Exceptions;
using Loom.Parser.AST;
using Loom.Parser.Tokenizer;
using System.Reflection;

namespace Loom.Parser;

// TODO: Make this use modular rules akin to LoomTokenizer
public class LoomParser
{
    /// <summary> The underlying <see cref="TokenReader"/>. />
    public TokenReader Reader { get;  }

    /// <summary> Initializes a new instance of the <see cref="LoomParser"/> class. </summary>
    public LoomParser(List<Token> tokens) => Reader = new TokenReader(tokens);

    /// <summary> Parses the given source code into a root <see cref="ProgramNode"/>. </summary>
    public ProgramNode ParseProgram()
    {
        var imports = new List<ImportNode>();
        var modules = new List<ModuleNode>();

        while (!Reader.Check(Token.TokenType.EOF))
        {
            if (Reader.Check(Token.TokenType.Import))
                imports.Add(ParseImport());
            else if (Reader.Check(Token.TokenType.Module))
                modules.Add(ParseModuleDefinition());
            else
                throw new LoomException($"Unexpected token: {Reader.Peek()}");
        }

        return new ProgramNode(imports, modules);
    }

    private ImportNode ParseImport()
    {
        Reader.Expect(Token.TokenType.Import); // import
        string imported = Reader.Expect(Token.TokenType.Identifier).Value; // name
        Reader.Expect(Token.TokenType.Semicolon); // ;

        return new ImportNode(imported);
    }

    private ModuleNode ParseModuleDefinition()
    {
        Reader.Expect(Token.TokenType.Module); // module
        string name = Reader.Expect(Token.TokenType.Identifier).Value; // name

        return new ModuleNode(name, ParseBlock());
    }

    private UnsafeNode ParseUnsafeBlock()
    {
        Reader.Expect(Token.TokenType.Unsafe); // unsafe
        return new UnsafeNode(ParseBlock());
    }

    private BlockNode ParseBlock()
    {
        List<ASTNode> body = new();
        Reader.Expect(Token.TokenType.LBrace); // {

        if (Reader.Check(Token.TokenType.Module)) // Nested Modules
            body.Add(ParseModuleDefinition());
        else if (Reader.Check(Token.TokenType.Unsafe)) // Unsafe Blocks
            body.Add(ParseUnsafeBlock());

        Reader.Expect(Token.TokenType.RBrace); // }

        return new BlockNode(body);
    }
}
