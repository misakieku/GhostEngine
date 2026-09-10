using Ghost.Core;
using Ghost.Engine.RenderPipeline;
using Ghost.Graphics.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.UnitTest.Streaming;

[TestClass]
public class PipelineResourceTests
{
    [TestMethod]
    public void GhostRenderPipeline_ResourcesImplementIPipelineResource()
    {
        var cullingResource = new GhostRenderPipeline.CullingResource();
        Assert.IsInstanceOfType<IPipelineResource>(cullingResource);

        var gpuSceneResource = new GhostRenderPipeline.GPUSceneResource();
        Assert.IsInstanceOfType<IPipelineResource>(gpuSceneResource);

        // Initial field states
        Assert.AreEqual(Handle<ComputeShader>.Invalid, cullingResource.buildHZBShader);
        Assert.AreEqual(Handle<ComputeShader>.Invalid, cullingResource.prepareIndirectArgsShader);
        Assert.IsNull(cullingResource.cullWorkGraphEntry);
        Assert.AreEqual(Handle<ComputeShader>.Invalid, gpuSceneResource.updateGPUSceneShader);

        // Resolve without AssetManager must throw InvalidOperationException
        Assert.ThrowsExactly<InvalidOperationException>(() => cullingResource.Resolve());
        Assert.ThrowsExactly<InvalidOperationException>(() => gpuSceneResource.Resolve());

        // Disposing before resolve should be completely safe and idempotent
        cullingResource.Dispose();
        gpuSceneResource.Dispose();

        // Fields must remain safely defaulted after dispose
        Assert.AreEqual(Handle<ComputeShader>.Invalid, cullingResource.buildHZBShader);
        Assert.AreEqual(Handle<ComputeShader>.Invalid, cullingResource.prepareIndirectArgsShader);
        Assert.IsNull(cullingResource.cullWorkGraphEntry);
        Assert.AreEqual(Handle<ComputeShader>.Invalid, gpuSceneResource.updateGPUSceneShader);
    }
}
