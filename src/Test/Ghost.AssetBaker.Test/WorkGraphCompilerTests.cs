using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Core.Graphics;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.AssetForge.Test;

[TestClass]
public class WorkGraphCompilerTests
{
    [TestMethod]
    public void DXC_CanCompileSimpleWorkGraph()
    {
        var compiler = new DXCShaderCompiler();
        var hlsl = @"
struct NodeRecord
{
    uint id;
};

[Shader(""node"")]
[NodeLaunch(""broadcasting"")]
[NodeIsProgramEntry]
[NodeDispatchGrid(1, 1, 1)]
[NumThreads(32, 1, 1)]
void EntryNode(
    uint3 dispatchThreadId : SV_DispatchThreadID,
    [NodeId(""LeafNode"")] [MaxRecords(1)] NodeOutput<NodeRecord> outputQueue)
{
    if (dispatchThreadId.x == 0)
    {
        ThreadNodeOutputRecords<NodeRecord> outRec = outputQueue.GetThreadNodeOutputRecords(1);
        outRec.Get().id = 42;
        outRec.OutputComplete();
    }
}

[Shader(""node"")]
[NodeLaunch(""broadcasting"")]
[NodeDispatchGrid(1, 1, 1)]
[NumThreads(32, 1, 1)]
void LeafNode(
    DispatchNodeInputRecord<NodeRecord> inputRecord,
    uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint id = inputRecord.Get().id;
}
";

        var config = new ShaderCompilationConfig
        {
            stage = ShaderStage.Library,
            model = ShaderModel.SM_6_8,
            entryPoint = "",
            shaderCode = hlsl,
            defines = Array.Empty<string>(),
            optimizeLevel = CompilerOptimizeLevel.O0
        };

        var result = compiler.Compile(in config, AllocationHandle.Persistent);
        Assert.IsTrue(result.IsSuccess, $"Compilation failed: {result.Message}");
        Assert.IsTrue(result.Value.Length > 0, "Bytecode should not be empty");
        result.Value.Dispose();
    }

    [TestMethod]
    public void DXC_CanCompileRecursiveWorkGraph()
    {
        var compiler = new DXCShaderCompiler();
        var hlsl = @"
struct TreeNodeRecord
{
    uint nodeIndex;
    uint depth;
};

[Shader(""node"")]
[NodeLaunch(""broadcasting"")]
[NodeIsProgramEntry]
[NodeDispatchGrid(1, 1, 1)]
[NumThreads(32, 1, 1)]
void RootNode(
    uint3 dispatchThreadId : SV_DispatchThreadID,
    [NodeId(""TraverseNode"")] [MaxRecords(1)] NodeOutput<TreeNodeRecord> traverseQueue)
{
    if (dispatchThreadId.x == 0)
    {
        ThreadNodeOutputRecords<TreeNodeRecord> outRec = traverseQueue.GetThreadNodeOutputRecords(1);
        outRec.Get().nodeIndex = 0;
        outRec.Get().depth = 0;
        outRec.OutputComplete();
    }
}

[Shader(""node"")]
[NodeLaunch(""broadcasting"")]
[NodeDispatchGrid(1, 1, 1)]
[NumThreads(32, 1, 1)]
[NodeMaxRecursionDepth(16)]
void TraverseNode(
    DispatchNodeInputRecord<TreeNodeRecord> inputRecord,
    uint3 dispatchThreadId : SV_DispatchThreadID,
    [NodeId(""TraverseNode"")] [MaxRecords(2)] NodeOutput<TreeNodeRecord> selfQueue)
{
    TreeNodeRecord rec = inputRecord.Get();
    if (rec.depth < 4 && dispatchThreadId.x == 0)
    {
        GroupNodeOutputRecords<TreeNodeRecord> outRecs = selfQueue.GetGroupNodeOutputRecords(2);
        outRecs[0].nodeIndex = rec.nodeIndex * 2 + 1;
        outRecs[0].depth = rec.depth + 1;
        outRecs[1].nodeIndex = rec.nodeIndex * 2 + 2;
        outRecs[1].depth = rec.depth + 1;
        outRecs.OutputComplete();
    }
}
";

        var config = new ShaderCompilationConfig
        {
            stage = ShaderStage.Library,
            model = ShaderModel.SM_6_8,
            entryPoint = "",
            shaderCode = hlsl,
            defines = Array.Empty<string>(),
            optimizeLevel = CompilerOptimizeLevel.O0
        };

        var result = compiler.Compile(in config, AllocationHandle.Persistent);
        Assert.IsTrue(result.IsSuccess, $"Compilation failed: {result.Message}");
        Assert.IsTrue(result.Value.Length > 0, "Bytecode should not be empty");
        result.Value.Dispose();
    }
}
