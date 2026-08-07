using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;
using System.Reflection;

namespace Ghost.UnitTest.Graphics;

[TestClass]
[DoNotParallelize]
public class RenderGraphValidationTest
{
    private struct PassData
    {
        public Identifier<RGTexture> texture;
        public Identifier<RGBuffer> buffer;
    }

    private MockingRenderDevice _renderDevice = null!;
    private MockingResourceDatabase _resourceDatabase = null!;
    private MockingResourceAllocator _resourceAllocator = null!;
    private MockingPipelineLibrary _pipelineLibrary = null!;
    private MockingGraphicsEngine _graphicsEngine = null!;
    private ICommandAllocator _graphicsCommandAllocator = null!;
    private ICommandAllocator _computeCommandAllocator = null!;
    private FrameScheduler _frameScheduler = null!;
    private ResourceManager _resourceManager = null!;
    private ShaderLibrary _shaderLibrary = null!;
    private RenderGraph _renderGraph = null!;
    private RenderGraphExecutionContext _executionContext;

    [TestInitialize]
    public void Setup()
    {
        _renderDevice = new MockingRenderDevice();
        _resourceDatabase = new MockingResourceDatabase();
        _resourceAllocator = new MockingResourceAllocator(_resourceDatabase);
        _pipelineLibrary = new MockingPipelineLibrary();
        _graphicsEngine = new MockingGraphicsEngine(_renderDevice, _resourceDatabase, _resourceAllocator);
        _graphicsCommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Graphics);
        _computeCommandAllocator = _graphicsEngine.CreateCommandAllocator(CommandBufferType.Compute);
        _frameScheduler = new FrameScheduler(_graphicsEngine);
        _resourceManager = new ResourceManager(_renderDevice, _resourceAllocator, _resourceDatabase);
        _shaderLibrary = new ShaderLibrary(null, _pipelineLibrary, string.Empty);
        _executionContext = new RenderGraphExecutionContext(
            _graphicsEngine,
            _frameScheduler,
            _graphicsCommandAllocator,
            _computeCommandAllocator);
        _renderGraph = new RenderGraph(_resourceDatabase, _resourceAllocator, _pipelineLibrary, _resourceManager, _shaderLibrary);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _frameScheduler.Dispose();
        _renderGraph.Dispose();
        _shaderLibrary.Dispose();
        _resourceManager.Dispose();
        _graphicsCommandAllocator.Dispose();
        _computeCommandAllocator.Dispose();
        _graphicsEngine.Dispose();
        _pipelineLibrary.Dispose();
        _resourceAllocator.Dispose();
        _resourceDatabase.Dispose();
        _renderDevice.Dispose();
    }

    private Result<RGExecution, Error> CompileAndExecute(ViewState viewState)
    {
        return _renderGraph.CompileAndExecute(_executionContext, viewState);
    }

    [TestMethod]
    public void TestColorAttachmentAndRandomAccessAreRejectedInBothOrders()
    {
        const string resourceName = "ColorUavConflict";

        var colorFirst = _renderGraph.AddRasterRenderPass<PassData>("ColorThenUav");
        var colorFirstTexture = colorFirst.CreateTexture(CreateColorTextureDesc(), resourceName);
        colorFirst.SetColorAttachment(colorFirstTexture, 0);
        var colorFirstError = Assert.ThrowsExactly<InvalidOperationException>(() => colorFirst.UseRandomAccessTexture(colorFirstTexture));
        AssertDetailedConflict(colorFirstError, "ColorThenUav", resourceName, colorFirstTexture.Value, "ColorAttachment", "UnorderedAccess");
        colorFirst.Dispose();

        _renderGraph.Reset();

        var uavFirst = _renderGraph.AddRasterRenderPass<PassData>("UavThenColor");
        var uavFirstTexture = uavFirst.CreateTexture(CreateColorTextureDesc(), resourceName);
        uavFirst.UseRandomAccessTexture(uavFirstTexture);
        var uavFirstError = Assert.ThrowsExactly<InvalidOperationException>(() => uavFirst.SetColorAttachment(uavFirstTexture, 0));
        AssertDetailedConflict(uavFirstError, "UavThenColor", resourceName, uavFirstTexture.Value, "ColorAttachment", "UnorderedAccess");
        uavFirst.Dispose();
    }

    [TestMethod]
    public void TestDepthAttachmentAndRandomAccessAreRejectedInBothOrders()
    {
        const string resourceName = "DepthUavConflict";

        var depthFirst = _renderGraph.AddRasterRenderPass<PassData>("DepthThenUav");
        var depthFirstTexture = depthFirst.CreateTexture(RGTextureDesc.RelativeDepth(1.0f), resourceName);
        depthFirst.SetDepthAttachment(depthFirstTexture);
        var depthFirstError = Assert.ThrowsExactly<InvalidOperationException>(() => depthFirst.UseRandomAccessTexture(depthFirstTexture));
        AssertDetailedConflict(depthFirstError, "DepthThenUav", resourceName, depthFirstTexture.Value, "DepthWrite", "UnorderedAccess");
        depthFirst.Dispose();

        _renderGraph.Reset();

        var uavFirst = _renderGraph.AddRasterRenderPass<PassData>("UavThenDepth");
        var uavFirstTexture = uavFirst.CreateTexture(RGTextureDesc.RelativeDepth(1.0f), resourceName);
        uavFirst.UseRandomAccessTexture(uavFirstTexture);
        var uavFirstError = Assert.ThrowsExactly<InvalidOperationException>(() => uavFirst.SetDepthAttachment(uavFirstTexture));
        AssertDetailedConflict(uavFirstError, "UavThenDepth", resourceName, uavFirstTexture.Value, "DepthWrite", "UnorderedAccess");
        uavFirst.Dispose();
    }

    [TestMethod]
    public void TestColorAndDepthAttachmentAreRejectedInBothOrders()
    {
        const string resourceName = "ColorDepthConflict";

        var colorFirst = _renderGraph.AddRasterRenderPass<PassData>("ColorThenDepth");
        var colorFirstTexture = colorFirst.CreateTexture(CreateColorTextureDesc(), resourceName);
        colorFirst.SetColorAttachment(colorFirstTexture, 0);
        var colorFirstError = Assert.ThrowsExactly<InvalidOperationException>(() => colorFirst.SetDepthAttachment(colorFirstTexture));
        AssertDetailedConflict(colorFirstError, "ColorThenDepth", resourceName, colorFirstTexture.Value, "ColorAttachment", "DepthWrite");
        colorFirst.Dispose();

        _renderGraph.Reset();

        var depthFirst = _renderGraph.AddRasterRenderPass<PassData>("DepthThenColor");
        var depthFirstTexture = depthFirst.CreateTexture(CreateColorTextureDesc(), resourceName);
        depthFirst.SetDepthAttachment(depthFirstTexture);
        var depthFirstError = Assert.ThrowsExactly<InvalidOperationException>(() => depthFirst.SetColorAttachment(depthFirstTexture, 0));
        AssertDetailedConflict(depthFirstError, "DepthThenColor", resourceName, depthFirstTexture.Value, "ColorAttachment", "DepthWrite");
        depthFirst.Dispose();
    }

    [TestMethod]
    public void TestUnsafeGenericWriteRequiresExplicitUsage()
    {
        const string resourceName = "AmbiguousUnsafeWrite";
        var builder = _renderGraph.AddUnsafeRenderPass<PassData>("AmbiguousUnsafePass");
        var texture = builder.CreateTexture(CreateColorTextureDesc(), resourceName);
        builder.UseTexture(texture, AccessFlags.WriteAll);
        builder.SetPassData(new PassData { texture = texture });
        builder.SetRenderFunc<PassData>(static (ref readonly data, ctx) => { });

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => builder.Dispose());
        Assert.Contains("AmbiguousUnsafePass", error.Message);
        Assert.Contains(resourceName, error.Message);
        Assert.Contains($"Texture #{texture.Value}", error.Message);
        Assert.Contains("generic writes are ambiguous", error.Message);
    }

    [TestMethod]
    public void TestCompilerBackstopRejectsRenderTargetStateForBuffer()
    {
        const string resourceName = "InvalidRenderTargetBuffer";
        var builder = _renderGraph.AddUnsafeRenderPass<PassData>("CompilerBackstopPass");
        builder.AllowPassCulling(false);
        var buffer = builder.CreateBuffer(new BufferDesc { Size = 1024 }, resourceName);
        builder.SetPassData(new PassData { buffer = buffer });
        builder.SetRenderFunc<PassData>(static (ref readonly data, ctx) => { });
        builder.Dispose();

        var viewState = new ViewState(1920, 1080, 1920, 1080);
        CompileAndExecute(viewState).GetValueOrThrow();

        var passes = GetPasses();
        passes[0].renderTargetWrites.Add(buffer.AsResource());

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => CompileAndExecute(viewState));
        AssertDetailedConflict(error, "CompilerBackstopPass", resourceName, buffer.Value, "ColorAttachment", "requires a texture resource");
    }

    [TestMethod]
    public void TestCompilerBackstopRejectsDepthStateForBuffer()
    {
        const string resourceName = "InvalidDepthBuffer";
        var builder = _renderGraph.AddRasterRenderPass<PassData>("DepthCompilerBackstopPass");
        var color = builder.CreateTexture(CreateColorTextureDesc(), "ValidColorTarget");
        var buffer = builder.CreateBuffer(new BufferDesc { Size = 1024 }, resourceName);
        builder.SetColorAttachment(color, 0);
        builder.SetPassData(new PassData { texture = color, buffer = buffer });
        builder.SetRenderFunc<PassData>(static (ref readonly data, ctx) => { });
        builder.Dispose();

        var passes = GetPasses();
        passes[0].depthAccess = new TextureAccess(
            buffer.AsResource().AsTexture(),
            AccessFlags.Write,
            new ResourceBarrierData(BarrierLayout.DepthStencilWrite, BarrierAccess.DepthStencilWrite, BarrierSync.DepthStencil));

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => CompileAndExecute(new ViewState(1920, 1080, 1920, 1080)));
        AssertDetailedConflict(error, "DepthCompilerBackstopPass", resourceName, buffer.Value, "DepthWrite", "requires a texture resource");
    }

    private List<RenderGraphPass> GetPasses()
    {
        var passesField = typeof(RenderGraph).GetField("_passes", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(passesField);
        var passes = passesField.GetValue(_renderGraph) as List<RenderGraphPass>;
        Assert.IsNotNull(passes);
        Assert.HasCount(1, passes);
        return passes;
    }

    private static RGTextureDesc CreateColorTextureDesc()
    {
        return RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm);
    }

    private static void AssertDetailedConflict(
        InvalidOperationException error,
        string passName,
        string resourceName,
        int resourceId,
        params string[] expectedDetails)
    {
        Assert.Contains(passName, error.Message);
        Assert.Contains(resourceName, error.Message);
        Assert.Contains($"#{resourceId}", error.Message);
        foreach (var detail in expectedDetails)
        {
            Assert.Contains(detail, error.Message);
        }
    }
}
