using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Common.Diagnostics;
using Loom.Parser.Tokenizer;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Literals;

/// <summary> Parses integer literals. </summary>
public class NumberLiteralParser : ILiteralParser<int>
{
    public static readonly DiagnosticDescription ExpectedNumber = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Expected a number literal, found '{0}'.");
    public static readonly DiagnosticDescription InvalidInteger = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "'{0}' is not a valid integer literal.");

    /// <inheritdoc/>
    public bool TryParse(Token token, [NotNullWhen(true)] out int result, out DiagnosticDescription? diagnostic)
    {
        diagnostic = null;

        if (token.Type != TokenType.Number)
        {
            diagnostic = ExpectedNumber with { Format = $"Expected a number literal, found '{token.Text}'." };
            result = 0;
            return false;
        }

        if (!int.TryParse(token.Text, NumberStyles.None, CultureInfo.InvariantCulture, out result))
        {
            diagnostic = InvalidInteger with { Format = $"'{token.Text}' is not a valid integer literal." };
            result = 0;
            return false;
        }

        return true;
    }
}
