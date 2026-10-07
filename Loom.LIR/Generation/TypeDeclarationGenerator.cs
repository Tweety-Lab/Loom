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
            }
        }

        // A type without a declared constructor gets an implicit constructor which initializes its fields
        if (!node.Body.Contents.Any(c => c is ConstructorDeclarationNode))
            methodGenerator.GenerateDefaultConstructorBody((ASTNode)node, declaredType);
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