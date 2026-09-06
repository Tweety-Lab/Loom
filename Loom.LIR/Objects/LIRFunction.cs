
namespace Loom.LIR.Objects;

public sealed class LIRFunction : LIRValueObject
{
    public string Name { get; }

    /// <summary> Whether this function is only a declaration (e.g. extern) with no body to emit. </summary>
    public bool IsDeclaration { get; set; }

    public List<LIRBasicBlock> Blocks { get; } = new List<LIRBasicBlock>();

    public IReadOnlyList<LIRValue> ParameterValues { get; }

    public LIRGenerator? LIRGenerator { get; }

    /// <inheritdoc />
    public override LIRFunctionType Type {  get; }

    /// <summary> Initializes a new instance of the <see cref="LIRFunction"/> class. </summary>
    private LIRFunction(string name, LIRFunctionType type, bool isDeclaration)
    {
        Name = name;
        Type = type;
        IsDeclaration = isDeclaration;

        var parameters = new List<LIRValue>();
        foreach (var paramInfo in type.Parameters)
            parameters.Add(new LIRTempValue(paramInfo.Name, paramInfo.Type));

        ParameterValues = parameters;

        if (!isDeclaration)
            LIRGenerator = new LIRGenerator(this);
    }

    /// <summary> Creates a new <see cref="LIRFunction"/> that only declares a signature (e.g. extern), with no body. </summary>
    public static LIRFunction Declare(string name, LIRFunctionType type) => new(name, type, true);

    /// <summary> Creates a new <see cref="LIRFunction"/> with an empty body ready for instruction emission. </summary>
    public static LIRFunction Define(string name, LIRFunctionType type) => new(name, type, false);
}
