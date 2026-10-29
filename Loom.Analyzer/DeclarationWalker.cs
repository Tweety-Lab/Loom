using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Common.Diagnostics;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer;

/// <summary>
/// Walks the AST and builds scoped <see cref="Binder"/>s, defining symbols for all declarations.
/// </summary>
internal class DeclarationWalker : ASTVisitor
{
    private static readonly DiagnosticDescription EnumValueOutOfRange = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Enum value exceeds the signed 32-bit range.");
    private static readonly DiagnosticDescription DuplicateEnumMember = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Duplicate enum member '{0}'.");

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
        var symbol = CurrentTable.Symbols.OfType<ModuleSymbol>().FirstOrDefault(s => s.Name == node.Name.Text);

        if (symbol == null)
        {
            symbol = new ModuleSymbol(node.Name.Text);
            symbol.DeclaringNode = node;
            CurrentTable.Define(symbol);
        }

        var mergedScope = symbol.DeclaringNode is ModuleNode declaring && Context.Binders.TryGetValue(declaring, out var existing) ? existing : null;

        WithScope(node, () => VisitChildren(node), symbol, mergedScope);
    }

    [Visitor]
    public void Visit(MethodDeclarationNode node)
    {
        var symbol = new MethodSymbol(node.Name.Text, MethodSymbol.MethodKind.Method);
        symbol.ContainingType = CurrentSymbol as TypeSymbol;
        symbol.IsStatic = node.HasModifier(Parser.Tokenizer.Token.TokenType.Static);
        symbol.IsExported = node.HasModifier(Parser.Tokenizer.Token.TokenType.Export);
        symbol.IsExtern = node.HasModifier(Parser.Tokenizer.Token.TokenType.Extern);
        
        symbol.ReturnPointerType = ResolvePointerType(node.ReturnType);
        symbol.ReturnIsMutable = ResolveMutability(node.ReturnType);

        symbol.Accessibility = ResolveAccessibility(node);
        if (symbol.ContainingType?.KnownType == TypeSymbol.DefaultType.Interface && !node.HasModifier(Parser.Tokenizer.Token.TokenType.Private))
            symbol.Accessibility = MemberAccessibility.Public;

        symbol.DeclaringNode = node;

        if (symbol.IsExtern && symbol.ContainingType == null)
            symbol.FullyQualifiedName = node.Name.Text;
        else
            symbol.FullyQualifiedName = BuildQualifiedName(node.Name.Text);

        CurrentTable.Define(symbol);

        if (CurrentSymbol is TypeSymbol type)
            type.Members = [.. type.Members, symbol];

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(TypeParameterNode node)
    {
        var symbol = new TypeParameterSymbol(node.BaseName)
        {
            DeclaringSymbol = CurrentSymbol,
            Ordinal = (CurrentSymbol as IGenericSymbol)?.TypeParameters.Count ?? 0
        };

        symbol.DeclaringNode = node;

        CurrentTable.Define(symbol);

        if (CurrentSymbol is MethodSymbol method)
            method.TypeParameters = [.. method.TypeParameters, symbol];
        else if (CurrentSymbol is TypeSymbol type)
            type.TypeParameters = [.. type.TypeParameters, symbol];

        Context.Bind(node, symbol);

        VisitChildren(node);
    }

    [Visitor]
    public void Visit(ParameterNode node)
    {
        var paramSymbol = new ParameterSymbol(node.Name.Text)
        {
            DeclaringSymbol = CurrentSymbol
        };

        paramSymbol.IsMutable = ResolveMutability(node.Type);
        paramSymbol.PointerType = ResolvePointerType(node.Type);
        paramSymbol.DeclaringNode = node;
        
        CurrentTable.Define(paramSymbol);
        Context.Bind(node, paramSymbol);

        if (CurrentSymbol is MethodSymbol method)
            method.Parameters = [.. method.Parameters, paramSymbol];

        VisitChildren(node);
    }

    [Visitor]
    public void Visit(ConstructorDeclarationNode node)
    {
        var symbol = new MethodSymbol(".ctor", MethodSymbol.MethodKind.Constructor);
        symbol.ContainingType = CurrentSymbol as TypeSymbol;
        symbol.IsStatic = node.HasModifier(Parser.Tokenizer.Token.TokenType.Static);
        symbol.IsExported = node.HasModifier(Parser.Tokenizer.Token.TokenType.Export);
        symbol.IsExtern = node.HasModifier(Parser.Tokenizer.Token.TokenType.Extern);
        symbol.Accessibility = ResolveAccessibility(node);

        // Constructors implicitly return void
        symbol.ReturnType = CurrentTable.Lookup("void")?.OfType<TypeSymbol>().FirstOrDefault(t => t.KnownType == TypeSymbol.DefaultType.Void);

        symbol.DeclaringNode = node;

        symbol.FullyQualifiedName = BuildQualifiedName(".ctor");

        CurrentTable.Define(symbol);

        if (CurrentSymbol is TypeSymbol type)
            type.Members = [.. type.Members, symbol];

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(DestructorDeclarationNode node)
    {
        var symbol = new MethodSymbol(".dtor", MethodSymbol.MethodKind.Destructor)
        {
            ContainingType = CurrentSymbol as TypeSymbol,
            ReturnType = CurrentTable.Lookup("void")?.OfType<TypeSymbol>().FirstOrDefault(),
            Accessibility = MemberAccessibility.Public,
            DeclaringNode = node,
            FullyQualifiedName = BuildQualifiedName(".dtor")
        };

        CurrentTable.Define(symbol);

        if (CurrentSymbol is TypeSymbol type)
            type.Members = [.. type.Members, symbol];

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(OperatorDeclarationNode node)
    {
        var symbol = new MethodSymbol($".{node.Operator.Text}", MethodSymbol.MethodKind.Operator)
        {
            ContainingType = CurrentSymbol as TypeSymbol,
            IsStatic = node.HasModifier(Parser.Tokenizer.Token.TokenType.Static),
            IsExported = node.HasModifier(Parser.Tokenizer.Token.TokenType.Export),
            IsExtern = node.HasModifier(Parser.Tokenizer.Token.TokenType.Extern)
        };

        symbol.ReturnPointerType = ResolvePointerType(node.ReturnType);
        symbol.ReturnIsMutable = ResolveMutability(node.ReturnType);
        symbol.Accessibility = ResolveAccessibility(node);

        symbol.DeclaringNode = node;

        symbol.FullyQualifiedName = BuildQualifiedName($".{node.Operator.Text}");

        CurrentTable.Define(symbol);

        if (CurrentSymbol is TypeSymbol type)
            type.Members = [.. type.Members, symbol];

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(EnumDeclarationNode node)
    {
        var symbol = new TypeSymbol(node.Name.Text, TypeSymbol.DefaultType.Enum)
        {
            FullyQualifiedName = BuildQualifiedName(node.Name.Text),
            IsExported = node.HasModifier(Parser.Tokenizer.Token.TokenType.Export),
            DeclaringNode = node,
            IsValueType = true
        };

        CurrentTable.Define(symbol);

        WithScope(node, () =>
        {
            long next = 0;
            foreach (var member in node.Body.Contents.OfType<EnumMemberNode>())
            {
                long value = member.Value ?? next;
                if (value > int.MaxValue)
                {
                    Context.DiagnosticContext?.Report(EnumValueOutOfRange, member.Name.Location);
                    value = 0;
                }

                var constant = new FieldSymbol(member.Name.Text)
                {
                    Type = symbol,
                    IsStatic = true,
                    ConstantValue = (int)value,
                    ContainingType = symbol,
                    Accessibility = MemberAccessibility.Public,
                    DeclaringNode = member,
                    FullyQualifiedName = BuildQualifiedName(member.Name.Text)
                };

                CurrentTable.Define(constant);
                if (symbol.Members.Any(existing => existing.Name == constant.Name))
                    Context.DiagnosticContext?.Report(DuplicateEnumMember, member.Name.Location, constant.Name);

                symbol.Members = [.. symbol.Members, constant];
                Context.Bind(member, constant);
                next = value + 1;
            }
        }, symbol);
    }

    [Visitor]
    public void Visit(StructDeclarationNode node)
    {
        var symbol = new TypeSymbol(node.Name.Text, TypeSymbol.DefaultType.Struct);
        symbol.FullyQualifiedName = BuildQualifiedName(node.Name.Text);
        symbol.IsExported = node.HasModifier(Parser.Tokenizer.Token.TokenType.Export);
        symbol.IsStatic = node.HasModifier(Parser.Tokenizer.Token.TokenType.Static);
        symbol.IsStackOnly = node.HasModifier(Parser.Tokenizer.Token.TokenType.Stack);
        symbol.DeclaringNode = node;
        symbol.IsValueType = true;

        CurrentTable.Define(symbol);

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(ClassDeclarationNode node)
    {
        var symbol = new TypeSymbol(node.Name.Text, TypeSymbol.DefaultType.Class);
        symbol.FullyQualifiedName = BuildQualifiedName(node.Name.Text);
        symbol.IsExported = node.HasModifier(Parser.Tokenizer.Token.TokenType.Export);
        symbol.IsStatic = node.HasModifier(Parser.Tokenizer.Token.TokenType.Static);
        symbol.DeclaringNode = node;
        symbol.IsValueType = false;

        CurrentTable.Define(symbol);

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(InterfaceDeclarationNode node)
    {
        var symbol = new TypeSymbol(node.Name.Text, TypeSymbol.DefaultType.Interface);
        symbol.FullyQualifiedName = BuildQualifiedName(node.Name.Text);
        symbol.IsExported = node.HasModifier(Parser.Tokenizer.Token.TokenType.Export);
        symbol.DeclaringNode = node;
        symbol.IsValueType = false;
        symbol.IsStatic = false;

        CurrentTable.Define(symbol);

        WithScope(node, () => VisitChildren(node), symbol);
    }

    [Visitor]
    public void Visit(FieldDeclarationNode node)
    {
        var symbol = new FieldSymbol(node.Variable.Name.Text);
        symbol.PointerType = ResolvePointerType(node.Variable.Type);
        symbol.IsMutable = ResolveMutability(node.Variable.Type);
        symbol.ContainingType = CurrentSymbol as TypeSymbol;
        symbol.FullyQualifiedName = BuildQualifiedName(node.Variable.Name.Text);
        symbol.Accessibility = ResolveAccessibility(node);

        symbol.DeclaringNode = node;
        CurrentTable.Define(symbol);
        Context.Bind(node, symbol);

        if (CurrentSymbol is TypeSymbol typeSymbol)
            typeSymbol.Members = [.. typeSymbol.Members, symbol];

        VisitChildren(node);
    }

    [Visitor]
    public void Visit(PropertyDeclarationNode node)
    {
        var symbol = new PropertySymbol(node.Name.Text);
        symbol.PointerType = ResolvePointerType(node.Type);
        symbol.IsMutable = ResolveMutability(node.Type);
        symbol.ContainingType = CurrentSymbol as TypeSymbol;
        symbol.IsStatic = node.HasModifier(Parser.Tokenizer.Token.TokenType.Static);
        symbol.DeclaringNode = node;

        symbol.Accessibility = ResolveAccessibility(node);

        if (symbol.ContainingType?.KnownType == TypeSymbol.DefaultType.Interface && !node.HasModifier(Parser.Tokenizer.Token.TokenType.Private))
            symbol.Accessibility = MemberAccessibility.Public;

        symbol.FullyQualifiedName = BuildQualifiedName(node.Name.Text);

        if (symbol.ContainingType?.KnownType != TypeSymbol.DefaultType.Interface && node.Accessors.Any(accessor => accessor.Body == null))
        {
            var backingFieldName = $"{node.Name.Text}.field";
            symbol.BackingField = new FieldSymbol(backingFieldName)
            {
                Accessibility = MemberAccessibility.Private,
                ContainingType = CurrentSymbol as TypeSymbol,
                PointerType = symbol.PointerType,
                IsMutable = symbol.IsMutable,
                DeclaringNode = node,
                FullyQualifiedName = BuildQualifiedName(backingFieldName)
            };
        }

        foreach (var accessor in node.Accessors)
        {
            var method = new MethodSymbol($"{accessor.AccessorType.Text}.{node.Name.Text}", MethodSymbol.MethodKind.Accessor)
            {
                IsStatic = symbol.IsStatic,
                ContainingType = CurrentSymbol as TypeSymbol,
                Accessibility = symbol.Accessibility,
                DeclaringNode = accessor,
                FullyQualifiedName = BuildQualifiedName($"{accessor.AccessorType.Text}.{node.Name.Text}")
            };

            if (accessor.AccessorType.Type == Parser.Tokenizer.Token.TokenType.Get)
            {
                method.ReturnPointerType = symbol.PointerType;
                method.ReturnIsMutable = symbol.IsMutable;
                symbol.Getter = method;
            }
            else
            {
                symbol.Setter = method;
                method.Parameters = [new ParameterSymbol("value") { DeclaringSymbol = method, DeclaringNode = accessor, IsMutable = symbol.IsMutable, PointerType = symbol.PointerType }];
            }

            Context.Bind(accessor, method);
        }

        CurrentTable.Define(symbol);
        Context.Bind(node, symbol);

        if (CurrentSymbol is TypeSymbol typeSymbol)
            typeSymbol.Members = [.. typeSymbol.Members, symbol];

        VisitChildren(node);
    }

    [Visitor]
    public void Visit(AccessorNode node)
    {
        var method = Context.GetSymbol(node).As<MethodSymbol>()!;
        WithScope(node, () =>
        {
            foreach (var parameter in method.Parameters)
                CurrentTable.Define(parameter);

            VisitChildren(node);
        }, method);
    }

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node)
    {
        var symbol = new LocalVariableSymbol(node.Variable.Name.Text);
        symbol.FullyQualifiedName = BuildQualifiedName(node.Variable.Name.Text);

        symbol.PointerType = ResolvePointerType(node.Variable.Type);
        symbol.IsMutable = ResolveMutability(node.Variable.Type);

        symbol.DeclaringNode = node;
        CurrentTable.Define(symbol);
        Context.Bind(node, symbol);

        VisitChildren(node);
    }

    /// <inheritdoc/>
    protected override void OnUnhandled(ASTNode node) => VisitChildren(node);

    private static PointerType ResolvePointerType(TypeNode type) =>
        type.HasModifier(Parser.Tokenizer.Token.TokenType.Unique) ? PointerType.Unique :
        type.HasModifier(Parser.Tokenizer.Token.TokenType.Ref) ? PointerType.Ref : PointerType.None;

    private static bool ResolveMutability(TypeNode type) =>
        ResolvePointerType(type) != PointerType.Ref || type.HasModifier(Parser.Tokenizer.Token.TokenType.Mut);
    private static MemberAccessibility ResolveAccessibility(IModifiableNode node)
    {
        if (node.HasModifier(Parser.Tokenizer.Token.TokenType.Public))
            return MemberAccessibility.Public;

        if (node.HasModifier(Parser.Tokenizer.Token.TokenType.Private))
            return MemberAccessibility.Private;

        return MemberAccessibility.None;
    }


    private void WithScope(ASTNode node, Action body, Symbol? symbol = null, Binder? scope = null)
    {
        var parentTable = CurrentTable;
        var previousSymbol = CurrentSymbol;

        CurrentTable = scope ?? new Binder(parentTable);
        Context.Binders[node] = CurrentTable;

        if (symbol is not null)
        {
            CurrentSymbol = symbol;
            Context.Bind(node, symbol);
        }

        body();

        CurrentTable = parentTable;
        CurrentSymbol = previousSymbol;
    }

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
