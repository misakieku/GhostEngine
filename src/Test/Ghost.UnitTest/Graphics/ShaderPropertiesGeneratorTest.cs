using Ghost.Engine.ShaderProperties;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public class ShaderPropertiesGeneratorTest
{
    [TestMethod]
    public unsafe void TestHiddenBlitShaderProperties_LayoutAndConstants()
    {
        Assert.AreEqual("Hidden/Blit", HiddenBlitShaderProperties.SHADER_NAME);
        var size = sizeof(HiddenBlitShaderProperties);
        Assert.AreEqual(8, size); // 2 uint fields = 8 bytes
    }

    [TestMethod]
    public unsafe void TestDefaultUnlitShaderProperties_IncludesTemplateBaseAndCustomFields()
    {
        Assert.AreEqual("Default/Unlit", DefaultUnlitShaderProperties.SHADER_NAME);
        Assert.AreEqual("Unlit", DefaultUnlitShaderProperties.TEMPLATE_NAME);
        var props = new DefaultUnlitShaderProperties
        {
            baseColor = new float4(1, 0, 0, 1),
            baseMap = 10,
            customTintScale = 2.0f
        };
        Assert.AreEqual(1.0f, props.baseColor.x);
    }
}
