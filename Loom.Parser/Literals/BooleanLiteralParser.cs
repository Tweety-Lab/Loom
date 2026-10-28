using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Common.Diagnostics;
using Loom.Parser.Tokenizer;
using System.Diagnostics.CodeAnalysis;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Literals;

/// <summary> Parses the 'true' and 'false' literals. </summary>
public class BooleanLiteralParser : ILiteralParser<bool>
{
    public static readonly DiagnosticDescription ExpectedBoolean = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Expected 'true' or 'false', found '{0}'.");

    /// <inheritdoc/>
    public bool TryParse(Token token, [NotNullWhen(true)] out bool result, out DiagnosticDescription? diagnostic)
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
                diagnostic = ExpectedBoolean with { Format = $"Expected 'true' or 'false', found '{token.Text}'." };
                result = false;
                return false;
        }
    }
}
