
namespace Loom.LIR.Objects;

public sealed class LIRFunction : LIRValueObject
{
    /// <summary> The name of the function. </summary>
    public string Name { get; }

    /// <summary> Whether this function is only a declaration (e.g. extern) with no body to emit. </summary>
    public bool IsDeclaration { get; set; }

    /// <summary> The basic blocks of this function. </summary>
    public List<LIRBasicBlock> Blocks { get; } = new List<LIRBasicBlock>();

    /// <summary> The parameters (as values) of this function. </summary>
    public IReadOnlyList<LIRValue> ParameterValues { get; }

    /// <summary> The LIR generator used for this function. </summary>
    public LIRGenerator? LIRGenerator { get; private set; }

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

    /// <summary> Converts this declaration into a definition, creating the body generator on demand. </summary>
    public void StartBody()
    {
        if (LIRGenerator == null)
            LIRGenerator = new LIRGenerator(this);

        IsDeclaration = false;
    }

    /// <summary> Creates a new <see cref="LIRFunction"/> that only declares a signature (e.g. extern), with no body. </summary>
    public static LIRFunction Declare(string name, LIRFunctionType type) => new(name, type, true);

    /// <summary> Creates a new <see cref="LIRFunction"/> with an empty body ready for instruction emission. </summary>
    public static LIRFunction Define(string name, LIRFunctionType type) => new(name, type, false);
}
