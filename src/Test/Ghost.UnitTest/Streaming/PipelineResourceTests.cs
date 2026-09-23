using Ghost.Core;
using Ghost.Engine.RenderPipeline;
using Ghost.Graphics.Core;

namespace Ghost.UnitTest.Streaming;

[TestClass]
public class PipelineResourceTests
{
    [TestMethod]
    public void GhostRenderPipeline_ResourcesImplementIPipelineResource()
    {
        var meshPipelineResource = new MeshPipelineResource();
        Assert.IsInstanceOfType<IPipelineResource>(meshPipelineResource);

        var materialPipelineResource = new MaterialPipelineResource();
        Assert.IsInstanceOfType<IPipelineResource>(materialPipelineResource);

        var gpuSceneResource = new GhostRenderPipeline.GPUSceneResource();
        Assert.IsInstanceOfType<IPipelineResource>(gpuSceneResource);

        // Initial field states
        Assert.AreEqual(Handle<ComputeShader>.Invalid, meshPipelineResource.buildHZBShader);
        Assert.AreEqual(Handle<ComputeShader>.Invalid, meshPipelineResource.prepareIndirectArgsShader);
        Assert.IsNull(meshPipelineResource.cullWorkGraphEntry);
        Assert.AreEqual(Handle<ComputeShader>.Invalid, gpuSceneResource.updateGPUSceneShader);

        // Resolve without AssetManager must throw InvalidOperationException
        Assert.ThrowsExactly<InvalidOperationException>(() => meshPipelineResource.Resolve());
        Assert.ThrowsExactly<InvalidOperationException>(() => materialPipelineResource.Resolve());
        Assert.ThrowsExactly<InvalidOperationException>(() => gpuSceneResource.Resolve());

        // Disposing before resolve should be completely safe and idempotent
        meshPipelineResource.Dispose();
        materialPipelineResource.Dispose();
        gpuSceneResource.Dispose();

        // Fields must remain safely defaulted after dispose
        Assert.AreEqual(Handle<ComputeShader>.Invalid, meshPipelineResource.buildHZBShader);
        Assert.AreEqual(Handle<ComputeShader>.Invalid, meshPipelineResource.prepareIndirectArgsShader);
        Assert.IsNull(meshPipelineResource.cullWorkGraphEntry);
        Assert.AreEqual(Handle<ComputeShader>.Invalid, gpuSceneResource.updateGPUSceneShader);
    }
}
