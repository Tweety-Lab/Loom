using Loom.Analyzer;
using Loom.Analyzer.Symbols;
using Loom.Common;
using Loom.LIR.Objects;
using Loom.Parser.AST;
using Loom.Parser.AST.Rules.Default;

namespace Loom.LIR.Generation;

internal partial class StatementGenerator
{
    protected sealed class Scope
    {
        public List<(LIRValue Storage, TypeSymbol Type)> Owned { get; } = [];

        public Dictionary<string, LIRTempValue?> Bindings { get; } = [];
    }

    /// <summary> Opens a scope, collecting the unique values declared within it. </summary>
    public void BeginScope() => scopes.Push(new());

    /// <summary> Closes the current scope, destroying its owned values in reverse declaration order. </summary>
    public void EndScope()
    {
        var scope = scopes.Pop();

        if (Generator.WritingBlock.Terminator == null)
            EmitCleanup(scope.Owned);

        foreach (var (name, previous) in scope.Bindings)
            if (previous == null)
                locals.Remove(name);
            else
                locals[name] = previous;
    }

    private void EmitCleanup(IEnumerable<(LIRValue Storage, TypeSymbol Type)> owned)
    {
        foreach (var (storage, type) in owned.Reverse())
            methodGenerator.DestroyInstance(function, storage, type);
    }
}
