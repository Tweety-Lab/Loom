using Loom.Analyzer.Symbols;
using Loom.Common.Diagnostics;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.Analyzer.Analyzers;

/// <summary>
/// Checks for type mismatches.
/// </summary>
[LoomAnalyzer]
public class TypeMismatchAnalyzer : Analyzer
{
    public static Diagnostic TypeMismatch = new(Diagnostic.DiagnosticLevel.Error, "Cannot implicitly cast '{0}' to expected type '{1}'.");
    public static Diagnostic UnexpectedValue = new(Diagnostic.DiagnosticLevel.Error, "Cannot return a value from a method that returns void.");
    public static Diagnostic MissingReturn = new(Diagnostic.DiagnosticLevel.Error, "Method with return type '{0}' must return a value.");

    [Visitor]
    public void Visit(ReturnStatementNode node)
    {
        var method = Context.FirstAncestorOrSelf<MethodDeclarationNode>(node);
        if (method == null)
            return;

        if (Context.GetSymbol(method).Symbol is not MethodSymbol methodSymbol)
            return;

        var returnType = methodSymbol.ReturnType;

        if (returnType == null)
            return;

        var isVoid = returnType.KnownType == TypeSymbol.DefaultType.Void;

        // return; when return is not void
        if (node.Expression == null)
        {
            if (!isVoid)
                Context.DiagnosticContext?.Report(MissingReturn, node.StartToken?.Location, returnType.Name);

            return;
        }

        // return value; when return is void
        if (isVoid)
        {
            Context.DiagnosticContext?.Report(UnexpectedValue, node.StartToken?.Location);
            return;
        }

        Check(returnType, node.Expression);
    }

    [Visitor]
    public void Visit(LocalDeclarationStatementNode node)
    {
        var localSymbol = Context.GetSymbol(node).Symbol as LocalVariableSymbol;
        if (localSymbol == null || localSymbol.Type == null)
            return;

        var initializer = node.Variable.Initializer;

        Check(localSymbol.Type, initializer);
    }

    [Visitor]
    public void Visit(FieldDeclarationNode node)
    {
        var fieldSymbol = Context.GetSymbol(node).Symbol as FieldSymbol;
        if (fieldSymbol == null || fieldSymbol.Type == null)
            return;

        var initializer = node.Variable.Initializer;

        Check(fieldSymbol.Type, initializer);
    }

    [Visitor]
    public void Visit(AssignmentStatementNode node)
    {
        if (Context.ExpressionTypes.TryGetValue(node.Target, out var targetType))
            Check(targetType, node.Value);
    }

    [Visitor]
    public void Visit(InstanceCreationExpresssionNode node)
    {
        if (!Context.ExpressionTypes.TryGetValue(node, out var type))
            return;

        var constructor = type.Members.OfType<MethodSymbol>().FirstOrDefault(member => member.Kind == MethodSymbol.MethodKind.Constructor);

        CheckArguments(constructor?.Parameters, node.Arguments);
    }

    [Visitor]
    public void Visit(CallExpressionNode node)
    {
        MethodSymbol? method = null;

        if (node.Callee is MemberAccessExpressionNode member)
        {
            if (!Context.ExpressionTypes.TryGetValue(member.Receiver, out var receiverType))
                receiverType = Context.GetSymbol(member.Receiver).Symbol as TypeSymbol;

            method = receiverType?.Members.OfType<MethodSymbol>().FirstOrDefault(candidate => candidate.Name == member.Name.BaseName);
        }
        else if (node.Callee is IdentifierNameNode identifier)
        {
            var symbol = Context.GetSymbol(identifier).Symbol;

            method = symbol as MethodSymbol
                ?? (symbol as TypeSymbol)?.Members.OfType<MethodSymbol>().FirstOrDefault(candidate => candidate.Name == identifier.BaseName);
        }

        CheckArguments(method?.Parameters, node.Arguments);
    }

    [Visitor]
    public void Visit(ArrayLiteralNode node)
    {
        if (Context.ExpressionTypes.TryGetValue(node, out var type) && type is ArrayTypeSymbol array)
            foreach (var element in node.Elements)
                Check(array.ElementType, element);
    }

    private void CheckArguments(IEnumerable<ParameterSymbol>? parameters, List<ExpressionNode> arguments)
    {
        if (parameters == null)
            return;

        var expected = parameters.ToArray();

        for (int index = 0; index < arguments.Count; index++)
        {
            // Extra arguments are reported separately, and missing ones are reported by the binding stage
            if (index >= expected.Length || expected[index].Type == null)
                continue;

            Check(expected[index].Type!, arguments[index]);
        }
    }

    private void Check(TypeSymbol assignee, ExpressionNode assigned)
    {
        if (!Context.ExpressionTypes.TryGetValue(assigned, out var assignedType))
            return;

        if (!CanImplicitlyConvert(assignedType, assignee))
            Context.DiagnosticContext?.Report(TypeMismatch, assigned.StartToken?.Location, assignedType.Name, assignee.Name);
    }

    private static bool CanImplicitlyConvert(TypeSymbol source, TypeSymbol target)
    {
        if (source == null || target == null)
            return false;

        if (source.IsGeneric || target.IsGeneric)
            return true;

        // Ugly
        if (source is ArrayTypeSymbol sourceArray || target is ArrayTypeSymbol)
            return source is ArrayTypeSymbol { ElementType: var sourceElement, Size: var sourceSize }
                && target is ArrayTypeSymbol { ElementType: var targetElement, Size: var targetSize }
                && sourceSize == targetSize
                && CanImplicitlyConvert(sourceElement, targetElement);

        if (source.KnownType == target.KnownType)
            return true;

        if (target.KnownType == TypeSymbol.DefaultType.Void)
            return false;

        if (source.KnownType == TypeSymbol.DefaultType.Void)
            return false;

        // Casting check here

        // The built-in integer types convert implicitly between one another
        if (IsIntegerType(source.KnownType) && IsIntegerType(target.KnownType))
            return true;

        return false;
    }

    private static bool IsIntegerType(TypeSymbol.DefaultType type) => type switch
    {
        TypeSymbol.DefaultType.I32 or TypeSymbol.DefaultType.I64 or TypeSymbol.DefaultType.IPtr or TypeSymbol.DefaultType.Char => true,
        _ => false
    };
}
