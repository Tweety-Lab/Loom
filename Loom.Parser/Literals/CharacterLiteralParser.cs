using static Loom.Common.Diagnostics.DiagnosticDescription;
using Loom.Common.Diagnostics;
using Loom.Parser.Tokenizer;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Loom.Parser.Tokenizer.Token;

namespace Loom.Parser.Literals;

/// <summary> Parses character literals into the Unicode scalar values they represent. </summary>
public class CharacterLiteralParser : ILiteralParser<int>
{
    public static readonly DiagnosticDescription ExpectedCharacter = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Expected a character literal, found '{0}'.");
    public static readonly DiagnosticDescription UnterminatedCharacter = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "Unterminated character literal '{0}'.");
    public static readonly DiagnosticDescription EmptyCharacter = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "A character literal must contain a single Unicode scalar value.");
    public static readonly DiagnosticDescription InvalidEscape = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "{0}");
    public static readonly DiagnosticDescription MultipleCharacters = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "A character literal must contain a single Unicode scalar value, found '{0}'.");
    public static readonly DiagnosticDescription InvalidCharacter = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "{0}");
    public static readonly DiagnosticDescription InvalidScalar = new($"LM{Code:D4}", Diagnostic.DiagnosticLevel.Error, "U+{0:X} is not a Unicode scalar value.");

    /// <summary> The highest Unicode scalar value. </summary>
    public const int MAX_SCALAR_VALUE = 0x10FFFF;

    /// <inheritdoc/>
    public bool TryParse(Token token, [NotNullWhen(true)] out int result, out DiagnosticDescription? diagnostic)
    {
        result = 0;
        diagnostic = null;

        if (token.Type != TokenType.CharacterLiteral)
        {
            diagnostic = ExpectedCharacter with { Format = $"Expected a character literal, found '{token.Text}'." };
            return false;
        }

        var literal = token.Text;

        if (literal.Length < 2 || literal[0] != '\'' || literal[^1] != '\'')
        {
            diagnostic = UnterminatedCharacter with { Format = $"Unterminated character literal '{literal}'." };
            return false;
        }

        var body = literal[1..^1];

        if (body.Length == 0)
        {
            diagnostic = EmptyCharacter;
            return false;
        }

        if (body[0] == '\\')
        {
            if (!TryDecodeEscape(body, out result, out var consumed, out var error))
            {
                diagnostic = InvalidEscape with { Format = error };

                result = 0;
                return false;
            }

            if (consumed != body.Length)
            {
                diagnostic = MultipleCharacters with { Format = $"A character literal must contain a single Unicode scalar value, found '{body}'." };

                result = 0;
                return false;
            }
        }
        else if (!TryDecodeRaw(body, out result, out var error))
        {
            diagnostic = InvalidCharacter with { Format = error };

            result = 0;
            return false;
        }

        if (!IsScalarValue(result))
        {
            diagnostic = InvalidScalar with { Format = $"U+{result:X} is not a Unicode scalar value." };

            result = 0;
            return false;
        }

        return true;
    }


    /// <summary> Checks whether <paramref name="value"/> is a Unicode scalar value, i.e. not a surrogate and within the Unicode range. </summary>
    public static bool IsScalarValue(int value) => value is >= 0 and <= MAX_SCALAR_VALUE and not (>= 0xD800 and <= 0xDFFF);

    /// <summary> Formats <paramref name="value"/> as the source text of a character literal, escaping it where required. </summary>
    public static string Format(int value)
    {
        var escape = value switch
        {
            0x00 => @"\0",
            0x07 => @"\a",
            0x08 => @"\b",
            0x09 => @"\t",
            0x0A => @"\n",
            0x0B => @"\v",
            0x0C => @"\f",
            0x0D => @"\r",
            '\\' => @"\\",
            '\'' => @"\'",
            _ => null
        };

        if (escape != null)
            return $"'{escape}'";

        // Control characters have no printable form, everything else can be written verbatim.
        if (!IsScalarValue(value) || (value < 0x80 && char.IsControl((char)value)))
            return $@"'\u{{{value:x}}}'";

        return $"'{char.ConvertFromUtf32(value)}'";
    }

    private static bool TryDecodeRaw(string body, out int result, out string error)
    {
        if (body.Length == 1)
        {
            result = body[0];
            error = "";
            return true;
        }

        if (body.Length == 2 && char.IsHighSurrogate(body[0]) && char.IsLowSurrogate(body[1]))
        {
            result = char.ConvertToUtf32(body[0], body[1]);
            error = "";
            return true;
        }

        result = 0;
        error = $"A character literal must contain a single Unicode scalar value, found '{body}'.";
        return false;
    }

    private static bool TryDecodeEscape(string body, out int result, out int consumed, out string error)
    {
        consumed = body.Length;
        result = 0;

        if (body.Length < 2)
        {
            error = $"Incomplete escape sequence in character literal '{body}'.";
            return false;
        }

        consumed = 2;

        switch (body[1])
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
            case '\'': result = '\''; break;
            case 'u': return TryDecodeUnicodeEscape(body, out result, out consumed, out error);
            default:
                error = $@"Unknown escape sequence '\{body[1]}'.";
                return false;
        }

        error = "";
        return true;
    }

    private static bool TryDecodeUnicodeEscape(string body, out int result, out int consumed, out string error)
    {
        if (body.Length < 3 || body[2] != '{')
        {
            result = 0;
            consumed = body.Length;
            error = @"Expected '{' after '\u', e.g. '\u{1F600}'.";
            return false;
        }

        var close = body.IndexOf('}', 3);

        if (close < 0)
        {
            result = 0;
            consumed = body.Length;
            error = @"Unterminated '\u{...}' escape sequence.";
            return false;
        }

        var digits = body[3..close];
        consumed = close + 1;

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
