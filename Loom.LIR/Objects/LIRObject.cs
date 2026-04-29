
namespace Loom.LIR.Objects;

/// <summary>
/// Base class for all LIR objects (i.e., functions).
/// </summary>
public class LIRObject
{
    public Dictionary<string, string> MetaData { get; set; } = new();
}
