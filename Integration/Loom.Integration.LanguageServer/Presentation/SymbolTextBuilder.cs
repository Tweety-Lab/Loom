using Loom.Analyzer.Symbols;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;
using System.Diagnostics.CodeAnalysis;

namespace Loom.Integration.LanguageServer.Presentation;

internal sealed class SymbolTextBuilder(IClassifiedTextBackend backend)
{
    public bool TryBuildSymbolDescription(Symbol symbol, [NotNullWhen(true)] out ClassifiedTextBuilder? builder)
    {
        if (!TryBuildSymbolName(symbol, out builder))
            return false;

        if (TryBuildSymbolDocumentation(symbol, out var documentation))
        {
            builder.Append(TextClassification.Punctuation, "\n");
            builder.Append(documentation);
        }

        return true;
    }

    public bool TryBuildSymbolName(Symbol symbol, [NotNullWhen(true)] out ClassifiedTextBuilder? builder)
    {
        switch (symbol)
        {
            case TypeSymbol type:
                builder = BuildTypeSymbolName(type);
                return true;
            case MethodSymbol method:
                builder = BuildMethodSymbolName(method);
                return true;
            case PropertySymbol property:
                builder = BuildPropertySymbolName(property);
                return true;
            case ParameterSymbol parameter:
                builder = BuildParameterSymbolName(parameter);
                return true;
            default:
                builder = null;
                return false;
        }
    }

    public bool TryBuildSymbolDocumentation(Symbol symbol, [NotNullWhen(true)] out ClassifiedTextBuilder? builder)
    {
        builder = null;

        if (symbol.DeclaringNode is IDocumentableNode node)
        {
            if (!node.DocumentComments.Items.Any(comment => comment.Directive is DocumentCommentDirective.Brief or DocumentCommentDirective.Remark && !string.IsNullOrWhiteSpace(comment.Body)))
                return false;

            builder = BuildSymbolDocumentation(node.DocumentComments);
            return true;
        }

        if (symbol is TypeParameterSymbol typeParam && typeParam.DeclaringSymbol?.DeclaringNode is IDocumentableNode typeParamOwner)
        {
            var comments = typeParamOwner.DocumentComments.Items.Where(comment =>
                comment.Directive == DocumentCommentDirective.TParam &&
                string.Equals(comment.Target, typeParam.Name, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(comment.Body))
            .ToArray();

            if (comments.Length == 0)
                return false;

            builder = new ClassifiedTextBuilder(backend).Text(string.Join("\n\n", comments.Select(comment => comment.Body)));

            return true;
        }

        if (symbol is ParameterSymbol param && param.DeclaringSymbol?.DeclaringNode is IDocumentableNode paramOwner)
        {
            var comments = paramOwner.DocumentComments.Items.Where(comment =>
                comment.Directive == DocumentCommentDirective.Param &&
                string.Equals(comment.Target, param.Name, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(comment.Body))
            .ToArray();

            if (comments.Length == 0)
                return false;

            builder = new ClassifiedTextBuilder(backend).Text(string.Join("\n\n", comments.Select(comment => comment.Body)));

            return true;
        }

        return false;
    }

    private ClassifiedTextBuilder BuildSymbolDocumentation(DocumentCommentListNode documentation)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        foreach (var comment in documentation.Items)
        {
            switch (comment.Directive)
            {
                case DocumentCommentDirective.Brief:
                    builder.Text(comment.Body);
                    break;
                case DocumentCommentDirective.Remark:
                    builder.Append(TextClassification.Punctuation, "\n\n");
                    builder.Text(comment.Body);
                    break;
                case DocumentCommentDirective.Return:
                    builder.Append(TextClassification.Punctuation, "\n\n");
                    builder.Keyword("Returns:\n");
                    builder.Text($"     {comment.Body}");
                    break;
            }
        }

        return builder;
    }

    private ClassifiedTextBuilder BuildTypeSymbolName(TypeSymbol symbol)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        builder.Keyword(symbol.IsExported ? "export " : "");
        builder.Keyword(symbol.IsStatic ? "static " : "");
        builder.Keyword(symbol.IsStackOnly ? "stack " : "");

        switch (symbol.KnownType)
        {
            case TypeSymbol.DefaultType.TypeParameter:
                builder.Keyword("typeparam ");
                break;

            case TypeSymbol.DefaultType.Interface:
                builder.Keyword("interface ");
                break;

            case TypeSymbol.DefaultType.Enum:
                builder.Keyword("enum ");
                break;

            default:
                builder.Keyword(symbol.IsValueType ? "struct " : "class ");
                break;
        }

        builder.Append(BuildFullyQualifiedName(symbol));

        return builder;
    }

    private ClassifiedTextBuilder BuildPropertySymbolName(PropertySymbol symbol)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        switch (symbol.Accessibility)
        {
            case MemberAccessibility.Public:
                builder.Keyword("public ");
                break;
            case MemberAccessibility.Private:
                builder.Keyword("private ");
                break;
        }

        builder.Keyword(symbol.IsStatic ? "static " : "");

        if (symbol.BackingField?.Type is not { } returnType)
            builder.Text("? ");
        else
        {
            switch (returnType.KnownType)
            {
                case TypeSymbol.DefaultType.TypeParameter:
                    builder.TypeParameter(returnType.Name + " ");
                    break;

                case TypeSymbol.DefaultType.Enum:
                    builder.EnumName(returnType.Name + " ");
                    break;

                default:
                    if (returnType.IsValueType)
                        builder.StructName(returnType.Name + " ");
                    else
                        builder.ClassName(returnType.Name + " ");
                    break;
            }
        }

        builder.Append(BuildFullyQualifiedName(symbol));

        builder.Punctuation(" { ");

        if (symbol.Getter is not null)
        {
            builder.Keyword("get");
            builder.Punctuation("; ");
        }
        if (symbol.Setter is not null)
        {
            builder.Keyword("set");
            builder.Punctuation("; ");
        }

        builder.Punctuation("}");

        return builder;
    }

    private ClassifiedTextBuilder BuildParameterSymbolName(ParameterSymbol symbol)
    {
        var builder = new ClassifiedTextBuilder(backend);

        if (symbol.IsBorrow)
        {
            builder.Keyword("ref ");
            if (symbol.IsMutable)
                builder.Keyword("mut ");
        }

        if (symbol.Type is not { } type)
            builder.Text("? ");
        else if (type.KnownType == TypeSymbol.DefaultType.TypeParameter)
            builder.TypeParameter(type.Name + " ");
        else if (type.KnownType == TypeSymbol.DefaultType.Enum)
            builder.EnumName(type.Name + " ");
        else if (type.IsValueType)
            builder.StructName(type.Name + " ");
        else
            builder.ClassName(type.Name + " ");

        return builder.ParameterName(symbol.Name);
    }

    private ClassifiedTextBuilder BuildMethodSymbolName(MethodSymbol symbol)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        switch (symbol.Accessibility)
        {
            case MemberAccessibility.Public:
                builder.Keyword("public ");
                break;
            case MemberAccessibility.Private:
                builder.Keyword("private ");
                break;
        }

        builder.Keyword(symbol.IsExported ? "export " : "");
        builder.Keyword(symbol.IsStatic ? "static " : "");
        builder.Keyword(symbol.IsExtern ? "extern " : "");

        if (symbol.ReturnType is not { } returnType)
            builder.Text("? ");
        else if (returnType.KnownType == TypeSymbol.DefaultType.TypeParameter)
            builder.TypeParameter(returnType.Name + " ");
        else if (returnType.KnownType == TypeSymbol.DefaultType.Enum)
            builder.EnumName(returnType.Name + " ");
        else if (returnType.IsValueType)
            builder.StructName(returnType.Name + " ");
        else
            builder.ClassName(returnType.Name + " ");

        builder.Append(BuildFullyQualifiedName(symbol));

        builder.Punctuation("(");

        bool first = true;
        foreach (var param in symbol.Parameters)
        {
            if (!first)
                builder.Punctuation(", ");

            if (param.Type is not { } parameterType)
                builder.Text("? ");
            else if (parameterType.KnownType == TypeSymbol.DefaultType.TypeParameter)
                builder.TypeParameter(parameterType.Name + " ");
            else if (parameterType.KnownType == TypeSymbol.DefaultType.Enum)
                builder.EnumName(parameterType.Name + " ");
            else if (parameterType.IsValueType)
                builder.StructName(parameterType.Name + " ");
            else
                builder.ClassName(parameterType.Name + " ");

            builder.ParameterName(param.Name);

            first = false;
        }

        builder.Punctuation(")");

        return builder;
    }

    private ClassifiedTextBuilder BuildFullyQualifiedName(Symbol symbol)
    {
        ClassifiedTextBuilder builder = new ClassifiedTextBuilder(backend);

        ReadOnlySpan<string> parts = symbol.FullyQualifiedName.Split("::");

        for (int i = 0; i < parts.Length - 1; i++)
        {
            builder.Namespace(parts[i]);
            builder.Punctuation("::");
        }

        switch (symbol)
        {
            case MethodSymbol method:
                builder.MethodName(parts[^1]);
                break;
            case PropertySymbol property:
                builder.PropertyName(parts[^1]);
                break;
            case TypeSymbol type:
                if (type.KnownType == TypeSymbol.DefaultType.TypeParameter)
                    builder.TypeParameter(parts[^1]);
                else if (type.KnownType == TypeSymbol.DefaultType.Interface)
                    builder.InterfaceName(parts[^1]);
                else if (type.KnownType == TypeSymbol.DefaultType.Enum)
                    builder.EnumName(parts[^1]);
                else if (type.IsValueType)
                    builder.StructName(parts[^1]);
                else
                    builder.ClassName(parts[^1]);
                break;
            case FieldSymbol field:
                builder.FieldName(parts[^1]);
                break;
            default:
                builder.Text(parts[^1]);
                break;
        }

        return builder;
    }
}
