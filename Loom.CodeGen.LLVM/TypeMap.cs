using LLVMSharp.Interop;
using Loom.LIR;

namespace Loom.CodeGen.LLVM;

internal static class TypeMap
{
    /// <summary> Maps LIR types to LLVM types. </summary>
    public static IReadOnlyDictionary<LIRType, LLVMTypeRef> Map => map;

    private static Dictionary<LIRType, LLVMTypeRef> map = new Dictionary<LIRType, LLVMTypeRef>();
    
    /// <summary> Maps a LIR type to an LLVM type. </summary>
    public static void Add(LIRType type, LLVMTypeRef llvmType) => map.Add(type, llvmType);
}
