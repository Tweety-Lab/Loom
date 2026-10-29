
namespace Loom.Analyzer.Symbols;

public enum MemberAccessibility
{
    /// <summary> No accessibility (i.e., top level) </summary>
    None,

    Public,
    Private
}

public enum PointerType
{
    None,

    Unique,
    Shared,
    Weak,
    Ref
}

/// <summary>
/// Represents a symbol that can be exported.
/// </summary>
public interface IExportableSymbol
{
    /// <summary> Whether this symbol is exported for use beyond it's owning module. </summary>
    bool IsExported { get; }
}

/// <summary>
/// Represents a symbol that can have accessibility.
/// </summary>
public interface IAccessibleSymbol
{
    /// <summary> The accessibility of this symbol. </summary>
    MemberAccessibility Accessibility { get; }
}
