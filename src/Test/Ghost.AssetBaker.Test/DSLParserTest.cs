using Ghost.Core;
using Ghost.DSL.ShaderCompiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.AssetForge.Test;

[TestClass]
public class DSLParserTest
{
    [TestMethod]
    public void TestParseTemplateShader_ExtractsPropertiesPayloadAndHLSL()
    {
        var shaderSource = @"
shader ""Custom/CarPaint"" : ""Lit""
{
    properties
    {
        uint flakeNormal;
        float flakeStrength = 0.5;
        float flakeScale = 100.0;
    }

    payload
    {
        float flakeIntensity;
    }

    hlsl
    {
        void GetSurfaceData(in MaterialContext ctx, inout Payload payload, out SurfaceData surface)
        {
            surface = DefaultSurfaceData();
        }
    }
}
";

        var syntaxResult = DSLShaderCompiler.ParseGraphicsShaderSyntax(shaderSource);
        Assert.IsTrue(syntaxResult.IsSuccess, $"Failed to parse syntax: {syntaxResult.Message}");

        var syntax = syntaxResult.Value;
        Assert.AreEqual("Custom/CarPaint", syntax.Name);
        Assert.AreEqual("Lit", syntax.TemplateName);
        Assert.IsNotNull(syntax.Properties);
        Assert.HasCount(3, syntax.Properties.Properties);
        Assert.AreEqual("uint", syntax.Properties.Properties[0].Type);
        Assert.AreEqual("flakeNormal", syntax.Properties.Properties[0].Name);
        Assert.AreEqual("float", syntax.Properties.Properties[1].Type);
        Assert.AreEqual("flakeStrength", syntax.Properties.Properties[1].Name);
        Assert.AreEqual("0.5", syntax.Properties.Properties[1].DefaultValue);

        Assert.IsNotNull(syntax.Payload);
        StringAssert.Contains(syntax.Payload.Code, "flakeIntensity");

        Assert.IsNotNull(syntax.Hlsl);
        StringAssert.Contains(syntax.Hlsl.Code, "GetSurfaceData");

        var semanticsResult = DSLShaderCompiler.GetShaderSemantics(syntax);
        Assert.IsTrue(semanticsResult.IsSuccess, $"Failed to get semantics: {semanticsResult.Message}");

        var semantics = semanticsResult.Value;
        Assert.AreEqual("Custom/CarPaint", semantics.name);
        Assert.AreEqual("Lit", semantics.templateName);
        Assert.HasCount(3, semantics.properties);
        Assert.IsNotNull(semantics.payload);
        Assert.IsNotNull(semantics.hlsl);
    }

    [TestMethod]
    public void TestResolveUnlitTemplateShader_StitchesAllPassesAndStages()
    {
        var shaderSource = @"
shader ""Custom/MyUnlit"" : ""Unlit""
{
    properties
    {
        float customGlow = 2.0;
    }

    payload
    {
        float glowIntensity;
    }

    hlsl
    {
        float4 GetColor(uint materialIndex, float2 uv, inout Payload payload)
        {
            UnlitShaderProperties props = LoadUnlitProperties(materialIndex);
            return props.baseColor * props.customGlow;
        }
    }
}
";

        var syntax = DSLShaderCompiler.ParseGraphicsShaderSyntax(shaderSource).GetValueOrThrow();
        var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();
        var descriptor = DSLShaderCompiler.ResolveShader(semantics, new DSL.Models.ShaderReflectionData(), new Dictionary<string, string>()).GetValueOrThrow();

        Assert.AreEqual("Custom/MyUnlit", descriptor.Name);
        Assert.HasCount(3, descriptor.Passes); // Forward, Visibility, Shadow

        foreach (var pass in descriptor.Passes)
        {
            Assert.IsTrue(pass.amplificationShaderCode.IsCreated, $"Pass {pass.name} missing AS");
            Assert.IsTrue(pass.meshShaderCode.IsCreated, $"Pass {pass.name} missing MS");
            Assert.IsTrue(pass.pixelShaderCode.IsCreated, $"Pass {pass.name} missing PS");
            StringAssert.Contains(pass.pixelShaderCode.code, "CustomMyUnlitShaderProperties");
        }
    }
    [TestMethod]
    public void TestResolveLitTemplateShader_StitchesAllPassesIncludingCompute()
    {
        var shaderSource = @"
shader ""Custom/CarPaint"" : ""Lit""
{
    properties
    {
        uint flakeNormal;
        float flakeStrength = 0.5;
        float flakeScale = 100.0;
    }

    payload
    {
        float flakeIntensity;
    }

    hlsl
    {
        void GetSurfaceData(in MaterialContext ctx, inout Payload payload, out SurfaceData surface)
        {
            CustomCarPaintShaderProperties props = LoadLitProperties(ctx.materialIndex);
            surface = (SurfaceData)0;
            surface.albedo = props.baseColor.rgb;
            surface.normalWS = ctx.normalWS;
            surface.metallic = props.metallic;
            surface.roughness = props.roughness;
            surface.occlusion = props.occlusion;
        }
    }
}
";

        var syntax = DSLShaderCompiler.ParseGraphicsShaderSyntax(shaderSource).GetValueOrThrow();
        var semantics = DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();
        var descriptor = DSLShaderCompiler.ResolveShader(semantics, new DSL.Models.ShaderReflectionData(), new Dictionary<string, string>()).GetValueOrThrow();

        Assert.AreEqual("Custom/CarPaint", descriptor.Name);
        Assert.HasCount(4, descriptor.Passes); // Forward, Visibility, Shadow, DeferredTexturing

        var forwardPass = descriptor.Passes.First(p => p.name == "Forward");
        Assert.IsTrue(forwardPass.amplificationShaderCode.IsCreated);
        Assert.IsTrue(forwardPass.meshShaderCode.IsCreated);
        Assert.IsTrue(forwardPass.pixelShaderCode.IsCreated);
        StringAssert.Contains(forwardPass.pixelShaderCode.code, "CustomCarPaintShaderProperties");

        var deferredPass = descriptor.Passes.First(p => p.name == "DeferredTexturing");
        Assert.IsTrue(deferredPass.computeShaderCode.IsCreated);
        StringAssert.Contains(deferredPass.computeShaderCode.code, "CSMain");
    }


    [TestMethod]
    public void TestParseClassicPassShader_Succeeds()
    {
        var shaderSource = @"
shader ""Hidden/Blit""
{
    pass ""Blit""
    {
        pipeline
        {
            ztest = disabled;
            zwrite = off;
            cull = off;
            blend = opaque;
            color_mask = all;
        }

        hlsl
        {
            float4 PSMain() : SV_TARGET { return 0; }
        }

        ms ""hlsl_block"" : ""MSMain"";
        ps ""hlsl_block"" : ""PSMain"";
    }
}
";

        var syntaxResult = DSLShaderCompiler.ParseGraphicsShaderSyntax(shaderSource);
        Assert.IsTrue(syntaxResult.IsSuccess, $"Failed to parse syntax: {syntaxResult.Message}");

        var syntax = syntaxResult.Value;
        Assert.AreEqual("Hidden/Blit", syntax.Name);
        Assert.IsNull(syntax.TemplateName);
        Assert.HasCount(1, syntax.Passes);

        var semanticsResult = DSLShaderCompiler.GetShaderSemantics(syntax);
        Assert.IsTrue(semanticsResult.IsSuccess, $"Failed to get semantics: {semanticsResult.Message}");

        var semantics = semanticsResult.Value;
        Assert.AreEqual("Hidden/Blit", semantics.name);
        Assert.IsNull(semantics.templateName);
        Assert.HasCount(1, semantics.passes);
    }
}
