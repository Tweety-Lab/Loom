
using System.Xml.Linq;

namespace Loom.LIR;

internal static class StringExtensions
{
    extension(string str)
    {
        /// <summary> Converts this <see cref="string"/> to an <see cref="IEnumerable{LIRValue}"/> representing characters. </summary>
        public IEnumerable<LIRValue> ToLIRString() => str.EnumerateRunes().Select(character => (LIRValue)new LIRConstantCharValue(character.Value));

        /// <summary> Converts this <see cref="string"/> to an <see cref="LIRConstantArrayValue"/> containing the characters of the <see cref="string"/>. </summary>
        public LIRConstantArrayValue ToLIRConstantArray() => new LIRConstantArrayValue(LIRType.Char, str.ToLIRString().ToList());
    }
}
