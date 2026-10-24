using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for invalid usage of interfaces.
/// </summary>
[LoomAnalyzer]
public class InterfaceAnalyzer : Analyzer
{
    public static Diagnostic InvalidMember = new(Diagnostic.DiagnosticLevel.Error, "Interface '{0}' can only declare method and property signatures.");
    public static Diagnostic MemberHasBody = new(Diagnostic.DiagnosticLevel.Error, "Interface member '{0}' cannot declare a body.");
    public static Diagnostic PropertyHasInitializer = new(Diagnostic.DiagnosticLevel.Error, "Interface property '{0}' cannot declare an initializer.");
    public static Diagnostic CannotInstantiate = new(Diagnostic.DiagnosticLevel.Error, "Cannot instantiate interface '{0}'.");

    [Visitor]
    public void Visit(InterfaceDeclarationNode node)
    {
        foreach (var member in node.Body.Contents)
        {
            switch (member)
            {
                case MethodDeclarationNode method:
                    if (method.Body != null)
                        Context.DiagnosticContext?.Report(MemberHasBody, method.Span.StartToken?.Location, method.Name.Text);

                    break;
                case PropertyDeclarationNode property:
                    if (property.Initializer != null)
                        Context.DiagnosticContext?.Report(PropertyHasInitializer, property.Span.StartToken?.Location, property.Name.Text);

                    if (property.Accessors.Any(accessor => accessor.Body != null))
                        Context.DiagnosticContext?.Report(MemberHasBody, property.Span.StartToken?.Location, property.Name.Text);

                    break;
                default:
                    Context.DiagnosticContext?.Report(InvalidMember, member.Span.StartToken?.Location, node.Name.Text);
                    break;
            }
        }
    }

    [Visitor]
    public void Visit(InstanceCreationExpresssionNode node)
    {
        if (Context.GetSymbol(node.TypeName).Symbol is TypeSymbol { KnownType: TypeSymbol.DefaultType.Interface } type)
            Context.DiagnosticContext?.Report(CannotInstantiate, node.Span.StartToken?.Location, type.Name);
    }
}
