using Loom.Analyzer.Analyzers;
using Loom.Analyzer.Symbols;
using Loom.LIR.Metadata;
using Loom.LIR.Objects;

namespace Loom.LIR.Generation;

internal partial class TypeDeclarationGenerator
{
    /// <summary> Emits a type's interface tables alongside its method bodies. </summary>
    private void GenerateInterfaceDispatch(TypeSymbol type)
    {
        if (type.ContainsTypeParameters)
            return;

        var members = InterfaceImplementationAnalyzer.ImplementationMembers(type).ToArray();
        foreach (var contract in type.AllInterfaces)
        {
            var slots = new List<LIRVirtualTableSlot>();
            foreach (var required in InterfaceDispatchLayout.Members(contract))
            {
                if (required is MethodSymbol { Kind: MethodSymbol.MethodKind.Method, IsStatic: false, Accessibility: MemberAccessibility.Public } method)
                {
                    var implementation = members.OfType<MethodSymbol>().FirstOrDefault(candidate => InterfaceImplementationAnalyzer.Implements(candidate, method));
                    AddSlot(method, implementation);
                }

                else if (required is PropertySymbol { IsStatic: false, Accessibility: MemberAccessibility.Public } property)
                {
                    var implementation = members.OfType<PropertySymbol>().FirstOrDefault(candidate => InterfaceImplementationAnalyzer.Implements(candidate, property));

                    if (property.Getter is { } getter)
                        AddSlot(getter, implementation?.Getter);

                    if (property.Setter is { } setter)
                        AddSlot(setter, implementation?.Setter);
                }
            }

            var table = new LIRVirtualTableValue(type.FullyQualifiedName, contract.FullyQualifiedName, slots);
            unit.Metadata[MetadataType.VirtualDispatch].Add(LIRMetadataValue.Define($"{type.FullyQualifiedName}::{contract.FullyQualifiedName}::vtable", table.Type, table));

            void AddSlot(MethodSymbol required, MethodSymbol? implementation)
            {
                if (required.IsGeneric)
                    throw new NotSupportedException($"Generic interface member '{required.FullyQualifiedName}' requires specialized dispatch slots.");

                var function = implementation == null ? null : unit.GetFunction(LinkageNameResolver.LinkageName(implementation));
                if (function == null)
                    throw new InvalidOperationException($"Missing interface implementation for '{required.FullyQualifiedName}' on '{type.FullyQualifiedName}'.");

                slots.Add(new(LinkageNameResolver.LinkageName(required), function));
            }
        }
    }
}
