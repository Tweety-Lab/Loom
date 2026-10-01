using Loom.Common.Diagnostics;
using Loom.Parser.Tokenizer;
using System.Diagnostics.CodeAnalysis;

namespace Loom.Parser.Literals;

/// <summary>
/// Defines how to convert a <see cref="Token"/> to a literal type. (i,e., true -> boolean literal.)
/// </summary>
/// <typeparam name="T">The output type.</typeparam>
public interface ILiteralParser<T>
{
    /// <summary> Tries to parse the token. </summary>
    bool TryParse(Token token, [NotNullWhen(true)] out T? result, out Diagnostic? diagnostic);
}
