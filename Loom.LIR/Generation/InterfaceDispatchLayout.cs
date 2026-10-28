using Loom.Analyzer.Symbols;

namespace Loom.LIR.Generation;

internal static class InterfaceDispatchLayout
{
    public static IEnumerable<Symbol> Members(TypeSymbol contract) => contract.AllInterfaces.Prepend(contract).Distinct().SelectMany(type => type.Members);

    public static IEnumerable<MethodSymbol> Methods(TypeSymbol contract)
    {
        foreach (var member in Members(contract))
        {
            if (member is MethodSymbol { Kind: MethodSymbol.MethodKind.Method, IsStatic: false, Accessibility: MemberAccessibility.Public } method)
                yield return method;

            else if (member is PropertySymbol { IsStatic: false, Accessibility: MemberAccessibility.Public } property)
            {
                if (property.Getter is { } getter)
                    yield return getter;

                if (property.Setter is { } setter)
                    yield return setter;
            }
        }
    }
}