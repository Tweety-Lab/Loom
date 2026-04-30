
namespace Loom.LIR.OpCodes;

/// <summary>
/// A Loom Intermediate Representation (LIR) operation code.
/// </summary>
public readonly struct LIROpCode
{
    public enum CodeType
    {
        Binary,
        Memory,
        Control,
        Call
    }

    /// <summary> The <see cref="string"/> name of the instruction. </summary>
    public string Name {  get; }

    /// <summary> The <see cref="CodeType"/> of the instruction. </summary>
    public CodeType Type { get; }

    /// <summary> Whether the instruction produces a value. </summary>
    public bool HasResult { get; }

    /// <summary> Initializes a new instance of the <see cref="LIROpCode"/> struct. </summary>
    private LIROpCode(string name, CodeType type, bool hasResult)
    {
        Name = name;
        Type = type;
        HasResult = hasResult;
    }
    
    public static readonly LIROpCode Add = new LIROpCode("add", CodeType.Binary, true);
    public static readonly LIROpCode Sub = new LIROpCode("sub", CodeType.Binary, true);
    public static readonly LIROpCode Mul = new LIROpCode("mul", CodeType.Binary, true);
    public static readonly LIROpCode Div = new LIROpCode("div", CodeType.Binary, true);
    public static readonly LIROpCode Ret = new LIROpCode("return", CodeType.Control, false);
    public static readonly LIROpCode Call = new LIROpCode("call", CodeType.Call, true);
    public static readonly LIROpCode Alloca = new LIROpCode("alloca", CodeType.Memory, true);
    public static readonly LIROpCode Load = new LIROpCode("load", CodeType.Memory, true);
    public static readonly LIROpCode Store = new LIROpCode("store", CodeType.Memory, false);
    public static readonly LIROpCode Const = new LIROpCode("const", CodeType.Memory, true);


    /// <inheritdoc/>
    public override string ToString() => Name;
}
