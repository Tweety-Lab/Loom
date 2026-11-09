using Loom.Analyzer;
using Loom.CodeGen.LLVM;
using Loom.Common;
using Loom.LIR;
using Loom.LIR.Metadata;
using Loom.LIR.Objects;
using Loom.LIR.OpCodes;
using Loom.Parser;

namespace Loom.Tests;

[CollectionDefinition("Static fields", DisableParallelization = true)]
public class StaticFieldCollection;

[Collection("Static fields")]
public class StaticFieldTests
{
    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("static class")]
    public void StaticField_UsesSharedWritableStorage(string declaration)
    {
        var context = new CompilationContext().Parse($$"""
            module Test
            {
                {{declaration}} Counter
                {
                    public static i32 Value = default;
                    public static i32 Increment() { Value++; return Value; }
                }
                export i32 Main() { Counter.Value = 41; Counter.Increment(); return Counter.Value; }
            }
            """).Analyze();

        Assert.Empty(context.DiagnosticContext.Diagnostics);
        var unit = Assert.Single(context.EmitLIR().CompilationUnits);
        var storage = Assert.Single(unit.Metadata[MetadataType.Static]);
        Assert.Equal("Test::Counter::Value", storage.Name);
        Assert.Equal(LIRType.Int32, storage.ValueType);
        Assert.False(storage.IsConstant);
        Assert.Empty(Assert.Single(unit.TypeDeclarations).Fields);
        var instructions = unit.AllFunctions.SelectMany(function => function.Blocks).SelectMany(block => block.Instructions).ToArray();
        Assert.DoesNotContain(instructions, instruction => instruction.OpCode == LIROpCode.GetField);
        Assert.Contains(instructions, instruction => instruction.OpCode == LIROpCode.Load && instruction.Operands.OfType<LIRMetadataReference>().Any(reference => reference.Name == storage.Name));
        Assert.Contains(instructions, instruction => instruction.OpCode == LIROpCode.Store && instruction.Operands.OfType<LIRMetadataReference>().Any(reference => reference.Name == storage.Name));

        var translator = new LLVMTranslatorPass();
        translator.Run(unit);
        using var module = translator.Result;
        Assert.Contains("internal global i32 0", module.PrintToString());
    }

    [Fact]
    public void StaticField_IsExcludedFromInstanceLayoutAndInitialization()
    {
        var context = new CompilationContext().Parse("""
            module Test
            {
                struct Counter
                {
                    public static i32 Shared = 99;
                    public i32 Value = 7;
                    public i32 ReadShared() { return Shared; }
                }
            }
            """).Analyze();

        Assert.Empty(context.DiagnosticContext.Diagnostics);
        var unit = Assert.Single(context.EmitLIR().CompilationUnits);
        var type = Assert.Single(unit.TypeDeclarations);
        var field = Assert.Single(type.Fields);
        Assert.Equal("Value", field.Name);
        Assert.Equal(0, field.Index);
        var constructor = Assert.Single(type.Methods, method => method.Name.EndsWith("::.ctor"));
        var stores = constructor.Blocks.SelectMany(block => block.Instructions).Where(instruction => instruction.OpCode == LIROpCode.Store);
        Assert.Single(stores);
        Assert.IsType<LIRDefaultValue>(Assert.Single(unit.Metadata[MetadataType.Static]).Initializer);
    }
}
