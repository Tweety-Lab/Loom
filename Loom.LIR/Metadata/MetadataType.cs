
namespace Loom.LIR.Metadata;

public enum MetadataType : byte
{
    /// <summary> Contains information about the compiled program. </summary>
    Assembly,

    /// <summary> Contains constant strings. </summary>
    Strings,

    /// <summary> Contains mutable storage for static members. </summary>
    Static,

    /// <summary> Contains VTable information. </summary>
    VirtualDispatch
}
