using System;
using System.Collections.Generic;
using System.Text;

namespace Loom.LIR.Objects;

public sealed class LIRFunction : LIRObject
{
    public string Name { get; }
    public LIRFunctionType Signature { get; }
    public List<LIRBasicBlock> Blocks { get; }

    /// <summary> Initializes a new instance of the <see cref="LIRFunction"/> class. </summary>
    public LIRFunction(string name, LIRFunctionType signature, List<LIRBasicBlock> blocks)
    {
        Name = name;
        Signature = signature;
        Blocks = blocks;
    }
}
