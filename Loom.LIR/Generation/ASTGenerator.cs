using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

/// <summary>
/// Converts an Abstract Syntax Tree (AST) into Loom Intermediate Representation (LIR).
/// </summary>
public class ASTGenerator
{
    private LIRCompilationUnit unit = null!;
    private CompilationContext context;

    /// <summary> Initializes a new instance of the <see cref="ASTGenerator"/> class. </summary>
    public ASTGenerator(CompilationContext context) => this.context = context;

    /// <summary> Generates Loom Intermediate Representation (LIR) from all Abstract Syntax Tree (AST) roots in the program. </summary>
    /// <param name="roots"> All roots of the AST, one per source file. </param>
    /// <returns> The generated LIR unit. </returns>
    public LIRCompilationUnit Generate(IEnumerable<ProgramNode> roots)
    {
        // Eventually, we want to link units instead of compiling into just one

        var rootList = roots.ToList();
        unit = new LIRCompilationUnit(string.Join("+", rootList.Select(r => r.Name)));

        var contents = rootList.SelectMany(root => root.Modules).SelectMany(module => module.Body.Contents).ToList();

        TypeDeclarationGenerator typeGenerator = new(context, unit);
        MethodGenerator methodGenerator = new(context, unit);

        foreach (var content in contents)
        {
            if (content is ITypeDeclarationNode typeDeclaration)
                typeGenerator.DeclareTypeDeclaration(typeDeclaration);

            else if (content is MethodDeclarationNode method)
                methodGenerator.GenerateMethodDeclaration(method);
        }

        foreach (var content in contents)
        {
            if (content is ITypeDeclarationNode typeDeclaration)
                typeGenerator.GenerateTypeDeclarationBodies(typeDeclaration);

            else if (content is MethodDeclarationNode method)
                methodGenerator.GenerateMethodBody(method);
        }

        return unit;
    }

    public static LIRType ConvertType(TypeSymbol type, TypeSubstitution? substitution = null)
    {
        if (substitution != null)
            type = substitution.Resolve(type);

        if (type is ArrayTypeSymbol array)
        {
            var arrayElementType = ConvertType(array.ElementType, substitution);
            bool elementIsValue = array.ElementType is TypeParameterSymbol || array.ElementType.IsValueType;
            return new LIRArrayType(elementIsValue ? arrayElementType : new LIRPointerType(arrayElementType), array.Size);
        }

        LIRType elementType = type.KnownType switch
        {
            TypeSymbol.DefaultType.Void => LIRType.Void,
            TypeSymbol.DefaultType.Bool => LIRType.Boolean,
            TypeSymbol.DefaultType.I32 => LIRType.Int32,
            TypeSymbol.DefaultType.I64 => LIRType.Int64,
            TypeSymbol.DefaultType.Char => LIRType.Char,
            TypeSymbol.DefaultType.IPtr => LIRType.IntPtr,
            TypeSymbol.DefaultType.Struct => new LIRTypeDeclarationType(type.FullyQualifiedName, true),
            TypeSymbol.DefaultType.Class => new LIRTypeDeclarationType(type.FullyQualifiedName, false),
            TypeSymbol.DefaultType.TypeParameter => new LIRTypeParameter(type.Name),
            _ => throw new Exception($"Unknown type {type.KnownType}")
        };

        return elementType;
    }

    public static LIRFunctionType BuildFunctionType(MethodSymbol symbol, LIRType? instancePointerType = null, TypeSubstitution? substitution = null)
    {
        var parameters = new List<LIRParameter>();

        if (instancePointerType != null)
            parameters.Add(new LIRParameter("self", instancePointerType));

        parameters.AddRange(symbol.Parameters.Select(p => new LIRParameter(p.Name, ConvertStorageType(p.Type!, substitution))));

        // An instantiation binds every type parameter, leaving none behind on its signature
        var typeParameters = substitution == null
            ? symbol.TypeParameters.Select(tp => new LIRTypeParameter(tp.Name)).ToArray()
            : [];

        return new LIRFunctionType(ConvertStorageType(symbol.ReturnType!, substitution), parameters.ToArray(), typeParameters);
    }

    /// <summary> Converts <paramref name="type"/> to the LIR type it is stored as. </summary>
    public static LIRType ConvertStorageType(TypeSymbol type, TypeSubstitution? substitution = null)
    {
        if (substitution != null)
            type = substitution.Resolve(type);

        // An unsubstituted type parameter is stored like a value type; its instantiation decides the real storage
        return type is ArrayTypeSymbol || type is TypeParameterSymbol || type.IsValueType || type.KnownType == TypeSymbol.DefaultType.Void
            ? ConvertType(type, substitution)
            : new LIRPointerType(ConvertType(type, substitution));
    }
}