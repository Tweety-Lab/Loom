using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class MethodGenerator
{
    public void GenerateConstructorDeclaration(LIRTypeDeclaration owningType)
    {
        var selfType = new LIRPointerType(owningType.Type);

        // A constructor of a generic type carries the type's parameters so that it is only emitted for its instantiations
        var functionType = new LIRFunctionType(LIRType.Void, [new LIRParameter("self", selfType)], [.. owningType.TypeParameters]);

        owningType.DefineMethod($"{owningType.Name}::.ctor", functionType);
    }

    public void GenerateConstructorDeclaration(ConstructorDeclarationNode node, LIRTypeDeclaration owningType)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.Name}!");
            return;
        }

        if (symbol.IsGeneric)
            return;

        var selfType = new LIRPointerType(owningType.Type);

        var parameters = new List<LIRParameter>
        {
            new("self", selfType)
        };

        parameters.AddRange(symbol.Parameters.Select(parameter => new LIRParameter(parameter.Name, ASTGenerator.ConvertStorageType(parameter.Type!))));

        List<LIRTypeParameter> typeParameters = [.. owningType.TypeParameters, .. symbol.TypeParameters.Select(tp => new LIRTypeParameter(tp.Name))];

        var functionType = new LIRFunctionType(LIRType.Void, [.. parameters], typeParameters.ToArray());

        if (symbol.IsExtern)
            owningType.DeclareMethod(LinkageNameResolver.LinkageName(symbol), functionType);
        else
            owningType.DefineMethod(LinkageNameResolver.LinkageName(symbol), functionType);
    }

    public void GenerateConstructorDeclaration(MethodSymbol symbol, LIRTypeDeclaration owningType, TypeSubstitution substitution)
    {
        var selfType = new LIRPointerType(owningType.Type);

        var parameters = new List<LIRParameter>
        {
            new("self", selfType)
        };

        parameters.AddRange(symbol.Parameters.Select(parameter => new LIRParameter(parameter.Name, ASTGenerator.ConvertStorageType(parameter.Type!, substitution))));

        var typeParameters = symbol.TypeParameters.Select(tp => new LIRTypeParameter(tp.Name)).ToArray();
        var functionType = new LIRFunctionType(LIRType.Void, [.. parameters], typeParameters);

        if (symbol.IsExtern)
            owningType.DeclareMethod(LinkageNameResolver.LinkageName(symbol), functionType);
        else
            owningType.DefineMethod(LinkageNameResolver.LinkageName(symbol), functionType);
    }

    /// <summary> Emits the body of the constructor of an instantiated generic type. </summary>
    public void GenerateConstructorBody(MethodSymbol symbol, LIRTypeDeclaration owningType, TypeSubstitution substitution, TypeSymbol ownerType)
    {
        if (symbol.IsExtern || symbol.DeclaringNode is not ConstructorDeclarationNode node)
            return;

        LIRFunction function = GetFunction(LinkageNameResolver.LinkageName(symbol), owningType);

        StatementGenerator statementGenerator = new StatementGenerator(context, unit, function, this, substitution, ownerType);

        statementGenerator.EmitFieldInitializers(node);

        foreach (var content in node.Body?.Contents ?? Enumerable.Empty<ASTNode>())
            if (content is StatementNode statement)
                statementGenerator.EmitStatement(statement);

        statementGenerator.EndScope();

        EmitImplicitReturn(function);
    }

    /// <summary> Emits the body of the implicit constructor of an instantiated generic type. </summary>
    public void GenerateDefaultConstructorBody(ASTNode typeNode, LIRTypeDeclaration owningType, TypeSymbol ownerType, TypeSubstitution substitution)
    {
        LIRFunction? constructor = owningType.Methods.FirstOrDefault(method => method.Name == $"{owningType.Name}::.ctor");
        if (constructor == null)
            return;

        StatementGenerator statementGenerator = new StatementGenerator(context, unit, constructor, this, substitution, ownerType);

        statementGenerator.EmitFieldInitializers(typeNode);
        statementGenerator.EndScope();

        EmitImplicitReturn(constructor);
    }

    public void GenerateConstructorBody(ConstructorDeclarationNode node, LIRTypeDeclaration owningType)
    {
        MethodSymbol? symbol = (MethodSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.Name}!");
            return;
        }

        if (symbol.IsGeneric || symbol.IsExtern)
            return;

        LIRFunction function = GetFunction(LinkageNameResolver.LinkageName(symbol), owningType);

        StatementGenerator statementGenerator = new StatementGenerator(context, unit, function, this);

        statementGenerator.EmitFieldInitializers(node);

        foreach (var content in node.Body?.Contents ?? Enumerable.Empty<ASTNode>())
            if (content is StatementNode statement)
                statementGenerator.EmitStatement(statement);

        statementGenerator.EndScope();

        EmitImplicitReturn(function);
    }

    public void GenerateDefaultConstructorBody(ASTNode typeNode, LIRTypeDeclaration owningType)
    {
        LIRFunction? constructor = owningType.Methods.FirstOrDefault(method => method.Name == $"{owningType.Name}::.ctor");
        if (constructor == null)
            return;

        StatementGenerator statementGenerator = new StatementGenerator(context, unit, constructor, this);

        statementGenerator.EmitFieldInitializers(typeNode);
        statementGenerator.EndScope();

        EmitImplicitReturn(constructor);
    }
}
