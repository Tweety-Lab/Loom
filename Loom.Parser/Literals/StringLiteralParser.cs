using Loom.Common.Diagnostics;
using Loom.Parser.Tokenizer;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Literals;

public class StringLiteralParser : ILiteralParser<int[]>
{
    /// <inheritdoc/>
    public bool TryParse(Token token, [NotNullWhen(true)] out int[]? result, out Diagnostic? diagnostic)
    {
        result = null;
        diagnostic = null;

        if (token.Type != TokenType.StringLiteral)
        {
            diagnostic = new Diagnostic(Diagnostic.DiagnosticLevel.Error, $"Expected a string literal, found '{token.Text}'.", token.Location);
            return false;
        }

        var literal = token.Text;

        if (literal.Length < 2 || literal[0] != '"' || literal[^1] != '"')
        {
            diagnostic = new Diagnostic(Diagnostic.DiagnosticLevel.Error, $"Unterminated string literal '{literal}'.", token.Location);
            return false;
        }

        var body = literal[1..^1];

        if (!TryDecode(body, out result, out var error))
        {
            diagnostic = new Diagnostic(Diagnostic.DiagnosticLevel.Error, error, token.Location);

            result = null;
            return false;
        }

        return true;
    }

    private static bool TryDecode(string body, [NotNullWhen(true)] out int[]? result, out string error)
    {
        var values = new List<int>();

        for (int index = 0; index < body.Length;)
        {
            if (body[index] == '\\')
            {
                if (!TryDecodeEscape(body, ref index, out var scalar, out var escapeError))
                {
                    result = null;
                    error = escapeError;
                    return false;
                }

                if (!CharacterLiteralParser.IsScalarValue(scalar))
                {
                    result = null;
                    error = $"U+{scalar:X} is not a Unicode scalar value.";
                    return false;
                }

                values.Add(scalar);
            }
            else if (char.IsHighSurrogate(body[index]))
            {
                if (index + 1 < body.Length && char.IsLowSurrogate(body[index + 1]))
                {
                    values.Add(char.ConvertToUtf32(body[index], body[index + 1]));
                    index += 2;
                    continue;
                }

                result = null;
                error = $"U+{(int)body[index]:X} is not a Unicode scalar value.";
                return false;
            }
            else if (char.IsLowSurrogate(body[index]))
            {
                result = null;
                error = $"U+{(int)body[index]:X} is not a Unicode scalar value.";
                return false;
            }
            else
            {
                values.Add(body[index]);
                index++;
            }
        }

        result = values.ToArray();
        error = "";
        return true;
    }

    private static bool TryDecodeEscape(string body, ref int index, out int result, out string error)
    {
        var start = index;
        index += 2; // The backslash and the escape character

        if (index > body.Length)
        {
            result = 0;
            error = $"Incomplete escape sequence in string literal '{body.Substring(start)}'.";
            return false;
        }

        switch (body[start + 1])
        {
            case '0': result = 0x00; break;
            case 'a': result = 0x07; break;
            case 'b': result = 0x08; break;
            case 't': result = 0x09; break;
            case 'n': result = 0x0A; break;
            case 'v': result = 0x0B; break;
            case 'f': result = 0x0C; break;
            case 'r': result = 0x0D; break;
            case '\\': result = '\\'; break;
            case '"': result = '"'; break;
            case '\'': result = '\''; break;
            case 'u': return TryDecodeUnicodeEscape(body, ref index, out result, out error);
            default:
                result = 0;
                error = $@"Unknown escape sequence '\{body[start + 1]}'.";
                return false;
        }

        error = "";
        return true;
    }

    private static bool TryDecodeUnicodeEscape(string body, ref int index, out int result, out string error)
    {
        if (index >= body.Length || body[index] != '{')
        {
            result = 0;
            error = @"Expected '{' after '\u', e.g. '\u{1F600}'.";
            return false;
        }

        var close = body.IndexOf('}', index + 1);

        if (close < 0)
        {
            result = 0;
            index = body.Length;
            error = @"Unterminated '\u{...}' escape sequence.";
            return false;
        }

        var digits = body[(index + 1)..close];
        index = close + 1;

        if (digits.Length == 0 || !int.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result))
        {
            result = 0;
            error = $"'{digits}' is not a valid hexadecimal Unicode scalar value.";
            return false;
        }

        error = "";
        return true;
    }
}