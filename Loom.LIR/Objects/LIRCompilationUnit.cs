using System;
using System.Collections.Generic;
using System.Text;

namespace Loom.LIR.Objects;

public class LIRCompilationUnit : ILIRObject
{
    public List<LIRFunction> Functions { get; } = new List<LIRFunction>();

    /// <inheritdoc/>
    public Dictionary<string, string> MetaData { get; init; } = new();

    /// <summary> Initializes a new instance of the <see cref="LIRCompilationUnit"/> class. </summary>
    public LIRCompilationUnit(List<LIRFunction> functions) => Functions = functions;
}
