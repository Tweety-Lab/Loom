
namespace Loom.Parser.AST;

public record ProgramNode(List<ImportNode> Imports, List<ModuleNode> Modules) : ASTNode;

public record ImportNode(string ModuleName) : ASTNode;

public record ModuleNode(string Name, BlockNode Body) : ASTNode;

public record UnsafeNode(BlockNode Body) : ASTNode;

public record BlockNode(List<ASTNode> Contents) : ASTNode;
