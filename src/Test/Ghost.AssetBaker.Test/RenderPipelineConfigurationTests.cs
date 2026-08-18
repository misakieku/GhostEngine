using System.Collections.Generic;
using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.AssetForge.Test;

[TestClass]
public class RenderPipelineConfigurationTests
{
    private readonly struct MockShadowInterface : IShaderInterfaceTag
    {
        public static ShaderInterfaceId Id => new(0x1000UL);
    }

    private readonly struct MockFogInterface : IShaderInterfaceTag
    {
        public static ShaderInterfaceId Id => new(0x2000UL);
    }

    private readonly struct MockVirtualShadowMap : IShaderImplementationTag<MockShadowInterface>
    {
        public static ShaderImplementationId Id => new(0x1001UL);
    }

    private readonly struct MockNoShadow : IShaderImplementationTag<MockShadowInterface>
    {
        public static ShaderImplementationId Id => new(0x1002UL);
    }

    private readonly struct MockVolumetricFog : IShaderImplementationTag<MockFogInterface>
    {
        public static ShaderImplementationId Id => new(0x2001UL);
    }

    private class MockFeatureProvider : IRenderPipelineFeatureProvider
    {
        public ShaderImplementationId ImplementationId { get; }
        public bool RequiresRayTracing { get; }
        public bool Prepared { get; private set; }

        public MockFeatureProvider(ShaderImplementationId implementationId, bool requiresRayTracing = false)
        {
            ImplementationId = implementationId;
            RequiresRayTracing = requiresRayTracing;
        }

        public bool IsSupported(in GraphicsDeviceCapabilities capabilities)
        {
            if (RequiresRayTracing && !capabilities.SupportsRayTracing)
            {
                return false;
            }
            return true;
        }

        public Result Prepare(RenderPipelineFeatureContext context)
        {
            Prepared = true;
            return Result.Success();
        }
    }

    [TestMethod]
    public void TestRenderPipelineConfiguration_TypeSafeBinding()
    {
        var config = new RenderPipelineConfiguration();
        config.Bind<MockShadowInterface, MockVirtualShadowMap>();
        config.Bind<MockFogInterface, MockVolumetricFog>();

        Assert.IsTrue(config.TryGetBinding(MockShadowInterface.Id, out var shadowImpl));
        Assert.AreEqual(MockVirtualShadowMap.Id, shadowImpl);

        Assert.IsTrue(config.TryGetBinding(MockFogInterface.Id, out var fogImpl));
        Assert.AreEqual(MockVolumetricFog.Id, fogImpl);
    }

    [TestMethod]
    public void TestPreparedConfiguration_PreparesMatchingProviders()
    {
        var config = new RenderPipelineConfiguration();
        config.Bind<MockShadowInterface, MockVirtualShadowMap>();
        config.Bind<MockFogInterface, MockVolumetricFog>();

        var vsmProvider = new MockFeatureProvider(MockVirtualShadowMap.Id);
        var fogProvider = new MockFeatureProvider(MockVolumetricFog.Id);

        var capabilities = new GraphicsDeviceCapabilities(
            SupportsRayTracing: true,
            SupportsMeshShaders: true,
            SupportsVariableRateShading: false,
            SupportsSamplerFeedback: false);

        var prepareResult = PreparedRenderPipelineConfiguration.Prepare(
            config,
            new[] { vsmProvider, fogProvider },
            in capabilities,
            new RenderPipelineFeatureContext { ResourceDatabase = null! });

        Assert.IsTrue(prepareResult.IsSuccess);
        var prepared = prepareResult.Value;
        Assert.AreEqual(2, prepared.Providers.Count);
        Assert.IsTrue(vsmProvider.Prepared);
        Assert.IsTrue(fogProvider.Prepared);
    }

    [TestMethod]
    public void TestPreparedConfiguration_RejectsUnsupportedHardware()
    {
        var config = new RenderPipelineConfiguration();
        config.Bind<MockShadowInterface, MockVirtualShadowMap>();

        var rtProvider = new MockFeatureProvider(MockVirtualShadowMap.Id, requiresRayTracing: true);

        var capabilitiesWithoutRT = new GraphicsDeviceCapabilities(
            SupportsRayTracing: false,
            SupportsMeshShaders: true,
            SupportsVariableRateShading: false,
            SupportsSamplerFeedback: false);

        var prepareResult = PreparedRenderPipelineConfiguration.Prepare(
            config,
            new[] { rtProvider },
            in capabilitiesWithoutRT,
            new RenderPipelineFeatureContext { ResourceDatabase = null! });

        Assert.IsTrue(prepareResult.IsFailure);
        Assert.IsTrue(prepareResult.Message!.Contains("not supported by current graphics hardware"));
    }
}
