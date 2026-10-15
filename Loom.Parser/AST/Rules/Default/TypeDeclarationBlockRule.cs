using System.Diagnostics.CodeAnalysis;

namespace Loom.Parser.AST.Rules.Default;

/// <summary>
/// Parses the body of a type, whose members can be methods, fields, properties, or nested structs.
/// </summary>
[ParserRule]
public class TypeDeclarationBlockRule : BlockRule
{
    /// <inheritdoc/>
    public TypeDeclarationBlockRule(LoomParser parser) : base(parser) { }

    /// <inheritdoc/>
    protected override bool TryParseMember([NotNullWhen(true)] out ASTNode? node)
    {
        if (TryRunRule<StructDeclarationRule, StructDeclarationNode>(out var structDecl))
        {
            node = structDecl;
            return true;
        }

        if (TryRunRule<ConstructorDeclarationRule, ConstructorDeclarationNode>(out var ctorDecl))
        {
            node = ctorDecl;
            return true;
        }

        if (TryRunRule<MethodDeclarationRule, MethodDeclarationNode>(out var methodDecl))
        {
            node = methodDecl;
            return true;
        }

        if (TryRunRule<FieldDeclarationRule, FieldDeclarationNode>(out var fieldDecl))
        {
            node = fieldDecl;
            return true;
        }

        if (TryRunRule<PropertyDeclarationRule, PropertyDeclarationNode>(out var propertyDecl))
        {
            node = propertyDecl;
            return true;
        }

        node = null;
        return false;
    }
}