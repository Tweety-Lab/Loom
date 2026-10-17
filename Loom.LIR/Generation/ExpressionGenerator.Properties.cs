using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class ExpressionGenerator
{
    // Resolves the property a variable-like expression reads or writes, if it is one
    private PropertySymbol? ResolveProperty(ExpressionNode node) => node switch
    {
        IdentifierNameNode identifier when !locals.ContainsKey(identifier.BaseName) && !IsParameter(identifier) => context.AnalysisContext.GetSymbol(identifier).Symbol as PropertySymbol,
        MemberAccessExpressionNode member => ResolveMember(member) as PropertySymbol,
        _ => null
    };

    private Symbol? ResolveMember(MemberAccessExpressionNode node)
    {
        var receiverType = context.AnalysisContext.ExpressionTypes.TryGetValue(node.Receiver, out var type) ? type : context.AnalysisContext.GetSymbol(node.Receiver).Symbol as TypeSymbol;
        return receiverType?.Members.FirstOrDefault(m => m.Name == node.Name.BaseName);
    }

    // Resolves the instance a property access acts on, or null when the property is static
    private LIRValue? EmitPropertySelf(PropertySymbol property, ExpressionNode node) => property.IsStatic ? null : node switch
    {
        MemberAccessExpressionNode member => EmitAddress(member.Receiver),
        _ => EmitSelfParameter()
    };

    private LIRValue EmitPropertyGet(PropertySymbol property, ASTNode node, LIRValue? self)
    {
        MethodSymbol getter = property.Getter ?? throw new Exception($"Could not resolve the get accessor of property '{property.Name}'.");
        return EmitAccessorCall(getter, self);
    }

    private LIRValue EmitPropertyAddress(PropertySymbol property, ExpressionNode node)
    {
        var value = EmitPropertyGet(property, node, EmitPropertySelf(property, node));

        // A reference type is already addressed by the pointer it holds; a value type needs storage to be addressed
        if (value.Type is LIRPointerType)
            return value;

        var address = Generator.EmitAlloca(value.Type);
        Generator.EmitStore(value, address);
        return address;
    }

    private void EmitPropertySet(PropertySymbol property, ExpressionNode target, ExpressionNode value) => EmitPropertySet(property, target, EmitPropertySelf(property, target), EmitValue(value, property.Type!));

    private void EmitPropertySet(PropertySymbol property, ExpressionNode target, LIRValue? self, LIRValue stored)
    {
        if (property.Setter != null)
        {
            EmitAccessorCall(property.Setter, self, stored);
            return;
        }

        // A get-only property is assigned through its backing field on the instance it belongs to
        TypeSymbol owner = target is MemberAccessExpressionNode member && context.AnalysisContext.ExpressionTypes.TryGetValue(member.Receiver, out var receiverType) ? receiverType : ResolveOwnerType(target);

        Generator.EmitStore(stored, EmitFieldAddress(self!, owner, property.BackingField!));
    }
}
