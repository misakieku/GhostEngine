using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

[GenerateShaderProperty("Internal/UpdateGPUScene")]
public partial struct UpdateGPUSceneShaderProperty
{
    public uint gpuSceneBuffer;
    public uint addBuffer;
    public uint addCount;
    public uint removeBuffer;
    public uint removeCount;
}

internal partial class GhostRenderPipeline
{
    public void UpdateGPUScene(RenderContext ctx, Handle<GPUBuffer> addBuffer, int addCount, Handle<GPUBuffer> removeBuffer, int removeCount)
    {
        if (addCount <= 0 && removeCount <= 0)
        {
            Logger.DebugAssert(addBuffer.IsInvalid && removeBuffer.IsInvalid, "Buffers should be invalid when there are no updates.");
            return; // No updates needed
        }

        // NOTE: We dispatch it here instead of in render graph is because the update does not perform every frame.
        // The topology change of the graph will trigger the recompilation of the render graph, which is expensive.
        // Currently the render graph does not support import invalid resources, which means we can not handle the early return in the render func.
        // Furthermore, updating the GPU scene does not rely on other resources and passes, it's isolated and always run before the actual rendering.
        // So it's fine to dispatch it here directly.

        var property = new UpdateGPUSceneShaderProperty
        {
            gpuSceneBuffer = ctx.ResourceDatabase.GetBindlessIndex(_gpuScene.SceneBuffer.AsResource(), BindlessAccess.UnorderedAccess),
            addBuffer = ctx.ResourceDatabase.GetBindlessIndex(addBuffer.AsResource()),
            addCount = (uint)addCount,
            removeBuffer = ctx.ResourceDatabase.GetBindlessIndex(removeBuffer.AsResource()),
            removeCount = (uint)removeCount
        };

        // TODO: Write and load the shader. This is just a placeholder for now.
        var shader = default(Handle<ComputeShader>);
        var keywords = new LocalKeywordSet();

        ctx.DispatchCompute(shader, 0, in keywords, in property, new uint3());
    }
}
