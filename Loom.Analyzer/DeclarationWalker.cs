using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and builds scoped <see cref="Binder"/>s, defining symbols for all declarations.
/// </summary>
internal class DeclarationWalker : ASTVisitor
{
    /// <summary> The owning <see cref="AnalysisContext"/>. </summary>
    public AnalysisContext Context { get; private set; }

    /// <summary> The current <see cref="Binder"/>. </summary>
    public Binder CurrentTable { get; private set; }

    /// <summary> The current <see cref="Symbol"/>. </summary>
    public Symbol? CurrentSymbol { get; private set; }

    /// <summary> Initializes a new instance of the <see cref="DeclarationWalker"/> class. </summary>
    public DeclarationWalker(AnalysisContext context, Binder currentTable)
    {
        Context = context;
        CurrentTable = currentTable;
    }

    [Visitor]
    public void Visit(ModuleNode node)
    {
        var symbol = new ModuleSymbol(node.Name.Text);
        symbol.DeclaringNode = node;

        CurrentTable.Define(symbol);

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        var returnType = CurrentTable.Lookup(node.ReturnType.Text)?.First() as TypeSymbol;

        List<ParameterSymbol> parameters = new List<ParameterSymbol>();

        var symbol = new MethodDefinitionSymbol(node.MethodName.Text, returnType, parameters);

        if (node.Modifiers.Any(m => m.Type == Parser.Tokenizer.Token.TokenType.Extern))
            symbol.FullyQualifiedName = node.MethodName.Text;
        else
            symbol.FullyQualifiedName = BuildQualifiedName(node.MethodName.Text);

        CurrentTable.Define(symbol);

        if (CurrentSymbol is TypeSymbol type)
            type.Members.Add(symbol);

        WithScope(node, () =>
        {
            foreach (var param in node.Parameters)
            {
                var type = CurrentTable.Lookup(param.Type.Text)?.First() as TypeSymbol;
                var paramSymbol = new ParameterSymbol(param.Name.Text, type);
                parameters.Add(paramSymbol);
                CurrentTable.Define(paramSymbol);
                Context.BoundSymbols[param] = paramSymbol;
            }

            VisitChildren(node);
        }, symbol);
    }

    [Visitor]
    public void Visit(StructDeclarationNode node)
    {
        var symbol = new TypeSymbol(node.StructName.Text, TypeSymbol.DefaultType.Struct);
        symbol.FullyQualifiedName = BuildQualifiedName(node.StructName.Text);
        symbol.DeclaringNode = node;

        CurrentTable.Define(symbol);

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(VariableDeclarationNode node)
    {
        var type = CurrentTable.Lookup(node.Type.Text)?.First() as TypeSymbol;
        var symbol = new LocalVariableSymbol(node.Name.Text, type);
        symbol.FullyQualifiedName = BuildQualifiedName(node.Name.Text);
        symbol.DeclaringNode = node;
        CurrentTable.Define(symbol);
        Context.BoundSymbols[node] = symbol;

        VisitChildren(node);
    }

    private void WithScope(ASTNode node, Action body, Symbol? symbol = null)
    {
        var parentTable = CurrentTable;
        var previousSymbol = CurrentSymbol;

        CurrentTable = new Binder(parentTable);
        Context.Binders[node] = CurrentTable;

        if (symbol is not null)
        {
            CurrentSymbol = symbol;
            Context.BoundSymbols[node] = symbol;
        }

        body();

        CurrentTable = parentTable;
        CurrentSymbol = previousSymbol;
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);

    private string BuildQualifiedName(string name)
    {
        var parts = new Stack<string>();
        parts.Push(name);

        var current = CurrentSymbol;
        if (current != null)
            parts.Push(current.FullyQualifiedName);

        return string.Join("::", parts);
    }
}
