using Loom.Common.Diagnostics;
using Loom.Parser.Tokenizer;
using System.Diagnostics.CodeAnalysis;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Literals;

/// <summary> Parses the 'true' and 'false' literals. </summary>
public class BooleanLiteralParser : ILiteralParser<bool>
{
    /// <inheritdoc/>
    public bool TryParse(Token token, [NotNullWhen(true)] out bool result, out Diagnostic? diagnostic)
    {
        diagnostic = null;

        switch (token.Type)
        {
            case TokenType.True:
                result = true;
                return true;

            case TokenType.False:
                result = false;
                return true;

            default:
                diagnostic = new Diagnostic(Diagnostic.DiagnosticLevel.Error, $"Expected 'true' or 'false', found '{token.Text}'.", token.Location);
                result = false;
                return false;
        }
    }
}
