using Loom.Common.Diagnostics;
using Loom.Parser.Tokenizer;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Literals;

/// <summary> Parses integer literals. </summary>
public class NumberLiteralParser : ILiteralParser<int>
{
    /// <inheritdoc/>
    public bool TryParse(Token token, [NotNullWhen(true)] out int result, out DiagnosticDescription? diagnostic)
    {
        diagnostic = null;

        if (token.Type != TokenType.Number)
        {
            diagnostic = new DiagnosticDescription("LM0061", Diagnostic.DiagnosticLevel.Error, $"Expected a number literal, found '{token.Text}'.");
            result = 0;
            return false;
        }

        if (!int.TryParse(token.Text, NumberStyles.None, CultureInfo.InvariantCulture, out result))
        {
            diagnostic = new DiagnosticDescription("LM0062", Diagnostic.DiagnosticLevel.Error, $"'{token.Text}' is not a valid integer literal.");
            result = 0;
            return false;
        }

        return true;
    }
}
