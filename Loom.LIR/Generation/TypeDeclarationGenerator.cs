using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

/// <summary>
/// Generates the Loom Intermediate Representation (LIR) of the types declared by a program.
/// </summary>
internal class TypeDeclarationGenerator
{
    private readonly CompilationContext context;
    private readonly LIRCompilationUnit unit;
    private readonly MethodGenerator methodGenerator;

    /// <summary> Initializes a new instance of the <see cref="TypeDeclarationGenerator"/> class. </summary>
    public TypeDeclarationGenerator(CompilationContext context, LIRCompilationUnit unit)
    {
        this.context = context;
        this.unit = unit;
        methodGenerator = new MethodGenerator(context, unit);
    }

    public void DeclareTypeDeclaration(ITypeDeclarationNode node)
    {
        TypeSymbol? symbol = GetTypeSymbol(node);
        if (symbol == null)
            return;

        LIRTypeDeclaration declaredType = unit.DefineTypeDeclaration(symbol.FullyQualifiedName, symbol.IsValueType);
        declaredType.TypeParameters.AddRange(symbol.TypeParameters.Select(parameter => new LIRTypeParameter(parameter.Name)));

        bool hasConstructor = node.Body.Contents.Any(content => content is ConstructorDeclarationNode);

        if (!hasConstructor)
            methodGenerator.GenerateConstructorDeclaration(declaredType);

        foreach (var content in node.Body.Contents)
        {
            switch (content)
            {
                case ConstructorDeclarationNode constructor:
                    methodGenerator.GenerateConstructorDeclaration(constructor, declaredType);
                    break;

                case MethodDeclarationNode method:
                    methodGenerator.GenerateMethodDeclaration(method, declaredType);
                    break;

                case FieldDeclarationNode field:
                    GenerateTypeDeclarationField(declaredType, field);
                    break;

                case PropertyDeclarationNode property:
                    GenerateTypeDeclarationProperty(declaredType, property, symbol);
                    break;
            }
        }
    }

    public void GenerateTypeDeclarationBodies(ITypeDeclarationNode node)
    {
        TypeSymbol? symbol = GetTypeSymbol(node);

        if (symbol == null)
            return;

        LIRTypeDeclaration? declaredType = unit.GetTypeDeclaration(symbol.FullyQualifiedName);

        if (declaredType == null)
            return;

        foreach (var content in node.Body.Contents)
        {
            switch (content)
            {
                case ConstructorDeclarationNode constructor:
                    methodGenerator.GenerateConstructorBody(constructor, declaredType);
                    break;

                case MethodDeclarationNode method:
                    methodGenerator.GenerateMethodBody(method, declaredType);
                    break;

                case PropertyDeclarationNode property when context.AnalysisContext.GetSymbol(property).Symbol is PropertySymbol propertySymbol:
                    methodGenerator.GeneratePropertyBody(propertySymbol, declaredType);
                    break;
            }
        }

        // A type without a declared constructor gets an implicit constructor which initializes its fields
        if (!node.Body.Contents.Any(c => c is ConstructorDeclarationNode))
            methodGenerator.GenerateDefaultConstructorBody((ASTNode)node, declaredType);
    }

    /// <summary> Declares the concrete LIR type for an instantiated generic type. </summary>
    public void DeclareTypeDeclaration(InstantiatedTypeSymbol symbol, TypeSubstitution substitution)
    {
        LIRTypeDeclaration declaredType = unit.DefineTypeDeclaration(symbol.FullyQualifiedName, symbol.IsValueType);

        bool hasConstructor = symbol.Members.OfType<MethodSymbol>().Any(member => member.Kind == MethodSymbol.MethodKind.Constructor);

        if (!hasConstructor)
            methodGenerator.GenerateConstructorDeclaration(declaredType);

        foreach (var member in symbol.Members)
        {
            switch (member)
            {
                case MethodSymbol { Kind: MethodSymbol.MethodKind.Constructor } constructor:
                    methodGenerator.GenerateConstructorDeclaration(constructor, declaredType, substitution);
                    break;

                case MethodSymbol method:
                    methodGenerator.GenerateMethodDeclaration(method, declaredType, substitution);
                    break;

                case FieldSymbol field:
                    declaredType.DeclareField(field.Name, ASTGenerator.ConvertType(field.Type!, substitution));
                    break;

                case PropertySymbol property:
                    methodGenerator.GeneratePropertyDeclaration(property, declaredType, null, substitution);
                    break;
            }
        }
    }

    /// <summary> Emits the bodies of the methods of an instantiated generic type. </summary>
    public void GenerateTypeDeclarationBodies(InstantiatedTypeSymbol symbol, TypeSubstitution substitution)
    {
        LIRTypeDeclaration? declaredType = unit.GetTypeDeclaration(symbol.FullyQualifiedName);

        if (declaredType == null)
            return;

        var node = (ITypeDeclarationNode)symbol.Definition.DeclaringNode!;

        foreach (var member in symbol.Members)
        {
            switch (member)
            {
                case MethodSymbol { Kind: MethodSymbol.MethodKind.Constructor } constructor:
                    methodGenerator.GenerateConstructorBody(constructor, declaredType, substitution, symbol);
                    break;

                case MethodSymbol method:
                    methodGenerator.GenerateMethodBody(method, declaredType, substitution, symbol);
                    break;

                case PropertySymbol property:
                    methodGenerator.GeneratePropertyBody(property, declaredType, substitution, symbol);
                    break;
            }
        }

        // A type without a declared constructor gets an implicit constructor which initializes its fields
        if (!symbol.Members.OfType<MethodSymbol>().Any(member => member.Kind == MethodSymbol.MethodKind.Constructor))
            methodGenerator.GenerateDefaultConstructorBody((ASTNode)node, declaredType, symbol, substitution);
    }

    private void GenerateTypeDeclarationField(LIRTypeDeclaration declaredType, FieldDeclarationNode node)
    {
        FieldSymbol? symbol = (FieldSymbol?)context.AnalysisContext.GetSymbol(node).Symbol;

        if (symbol == null)
        {
            Console.WriteLine($"Could not find symbol for {node.Variable.Name}!");
            return;
        }

        declaredType.DeclareField(symbol.Name, ASTGenerator.ConvertType(symbol.Type!));
    }

    private void GenerateTypeDeclarationProperty(LIRTypeDeclaration declaredType, PropertyDeclarationNode node, TypeSymbol ownerType)
    {
        if (context.AnalysisContext.GetSymbol(node).Symbol is PropertySymbol symbol)
            methodGenerator.GeneratePropertyDeclaration(symbol, declaredType, ownerType.TypeParameters);
    }

    private TypeSymbol? GetTypeSymbol(ITypeDeclarationNode node)
    {
        TypeSymbol? symbol = node switch
        {
            StructDeclarationNode structNode => (TypeSymbol?)context.AnalysisContext.GetSymbol(structNode).Symbol,
            ClassDeclarationNode classNode => (TypeSymbol?)context.AnalysisContext.GetSymbol(classNode).Symbol,
            _ => null
        };

        if (symbol == null)
            Console.WriteLine($"Could not find symbol for {node.Name}!");

        return symbol;
    }
}