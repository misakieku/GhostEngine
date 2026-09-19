using Ghost.Engine.ShaderProperties;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public class ShaderPropertiesGeneratorTest
{
    [TestMethod]
    public unsafe void TestHiddenBlitShaderProperties_LayoutAndConstants()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Hidden/Blit", HiddenBlitShaderProperties.SHADER_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var size = sizeof(HiddenBlitShaderProperties);
        Assert.AreEqual(8, size); // 2 uint fields = 8 bytes
    }

#if false
    [TestMethod]
    public unsafe void TestDefaultUnlitShaderProperties_IncludesTemplateBaseAndCustomFields()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Default/Unlit", (string)DefaultUnlitShaderProperties.SHADER_NAME);
        Assert.AreEqual("Unlit", (string)DefaultUnlitShaderProperties.TEMPLATE_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var props = new DefaultUnlitShaderProperties
        {
            baseColor = new float4(1, 0, 0, 1),
            baseMap = 10,
            customTintScale = 2.0f
        };
        Assert.AreEqual(1.0f, props.baseColor.x);
    }
#endif

    [TestMethod]
    public unsafe void TestInternalUpdateGPUSceneShaderProperties_LayoutAndConstants()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Internal/UpdateGPUScene", InternalUpdateGPUSceneShaderProperties.SHADER_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var size = sizeof(InternalUpdateGPUSceneShaderProperties);
        Assert.AreEqual(20, size); // 5 uint fields = 20 bytes
    }
}
