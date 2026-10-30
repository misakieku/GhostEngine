using Ghost.DSL.ShaderCompiler;

namespace Ghost.AssetForge.Test;

[TestClass]
public class DSLParserTest
{
    [TestMethod]
    public void TestParseTemplateShader_ExtractsPropertiesAndHLSL()
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

    hlsl
    {
        SurfaceData GetSurfaceData(in MaterialContext ctx, in MaterialProperties props)
        {
            return DefaultSurfaceData();
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

        Assert.IsNotNull(syntax.Hlsl);
        StringAssert.Contains(syntax.Hlsl.Code, "GetSurfaceData");

        var semanticsResult = DSLShaderCompiler.GetShaderSemantics(syntax);
        Assert.IsTrue(semanticsResult.IsSuccess, $"Failed to get semantics: {semanticsResult.Message}");

        var semantics = semanticsResult.Value;
        Assert.AreEqual("Custom/CarPaint", semantics.name);
        Assert.AreEqual("Lit", semantics.templateName);
        Assert.HasCount(3, semantics.properties);
        Assert.IsNotNull(semantics.hlsl);
    }

    [TestMethod]
    public void TestResolveSimpleLit_StitchesShadingModel()
    {
        var shaderSource = File.ReadAllText(@"F:\csharp\GhostEngine\src\Test\TestGame\Assets\Shaders\SimpleLit.gshdr");
        var syntaxResult = DSLShaderCompiler.ParseGraphicsShaderSyntax(shaderSource);
        Assert.IsTrue(syntaxResult.IsSuccess, syntaxResult.Message);
        var semanticsResult = DSLShaderCompiler.GetShaderSemantics(syntaxResult.Value);
        Assert.IsTrue(semanticsResult.IsSuccess, semanticsResult.Message);
        var semantics = semanticsResult.Value;
        Assert.IsTrue(semantics.strategies.ContainsKey("DEFERREDLIGHTING_STRATEGY"));
        Assert.AreEqual("EngineResources/Shaders/Material/Lit/SimpleLit.hlsl", semantics.strategies["DEFERREDLIGHTING_STRATEGY"].filePath);

        var descriptorResult = DSLShaderCompiler.ResolveShader(semantics);
        Assert.IsTrue(descriptorResult.IsSuccess, descriptorResult.Message);
        var dtPass = descriptorResult.Value.Passes.First(p => p.semantic == Ghost.Core.Graphics.PassSemantic.DeferredTexturing);
        StringAssert.Contains(dtPass.computeShaderCode.code, "#define SHADING_MODEL_ID 1u");
        Assert.DoesNotContain("SimpleLit.hlsl", dtPass.computeShaderCode.code);

        var dlPass = descriptorResult.Value.Passes.First(p => p.semantic == Ghost.Core.Graphics.PassSemantic.DeferredLighting);
        StringAssert.Contains(dlPass.computeShaderCode.code, "#define SHADING_MODEL_ID 1u");
        StringAssert.Contains(dlPass.computeShaderCode.code, "SimpleLit.hlsl");
    }

    [TestMethod]
    public void TestUnknownStrategy_FailsValidation()
    {
        var shaderSource = @"
shader ""MyShader/InvalidStrategy"" : ""Lit""
{
    strategy ""UNKNOWN_INVALID_STRATEGY"" ""SomeType""
    {
        struct SomeType { };
    }
}
";
        var syntaxResult = DSLShaderCompiler.ParseGraphicsShaderSyntax(shaderSource);
        Assert.IsTrue(syntaxResult.IsSuccess, syntaxResult.Message);
        var semanticsResult = DSLShaderCompiler.GetShaderSemantics(syntaxResult.Value);
        Assert.IsTrue(semanticsResult.IsSuccess, semanticsResult.Message);

        var descriptorResult = DSLShaderCompiler.ResolveShader(semanticsResult.Value);
        Assert.IsTrue(descriptorResult.IsFailure, "Should fail when shader specifies unknown strategy");
        StringAssert.Contains(descriptorResult.Message, "UNKNOWN_INVALID_STRATEGY");
    }

#if false
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
        SurfaceData GetSurfaceData(in MaterialContext ctx, in MaterialProperties props, inout Payload payload)
        {
            return (SurfaceData)0;
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
            if (pass.semantic != PassSemantic.Visibility)
            {
                Assert.IsTrue(pass.amplificationShaderCode.IsCreated, $"Pass {pass.name} missing AS");
            }
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
        SurfaceData GetSurfaceData(in MaterialContext ctx, in MaterialProperties props, inout Payload payload)
        {
            CustomCarPaintShaderProperties props = LoadLitProperties(ctx.materialIndex);
            SurfaceData surface = (SurfaceData)0;
            surface.albedo = props.baseColor.rgb;
            surface.normalWS = ctx.normalWS;
            surface.metallic = props.metallic;
            surface.roughness = props.roughness;
            surface.occlusion = props.occlusion;

            return surface;
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
        Assert.AreEqual(PassSemantic.Forward, forwardPass.semantic);
        Assert.IsTrue(forwardPass.amplificationShaderCode.IsCreated);
        Assert.IsTrue(forwardPass.meshShaderCode.IsCreated);
        Assert.IsTrue(forwardPass.pixelShaderCode.IsCreated);
        StringAssert.Contains(forwardPass.pixelShaderCode.code, "CustomCarPaintShaderProperties");

        var deferredPass = descriptor.Passes.First(p => p.name == "DeferredTexturing");
        Assert.AreEqual(PassSemantic.DeferredTexturing, deferredPass.semantic);
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
#endif

    [TestMethod]
    public void TestParseGraphicsShader_WithKeywords()
    {
        var shaderSource = @"
shader ""Custom/KeywordsTest""
{
    keywords
    {
        GLOBAL_FEATURE: _GLOBAL_A, _GLOBAL_B;
    }

    pass ""Forward""
    {
        keywords
        {
            SHADOW_MODE: _SHADOW_PCF, _SHADOW_VSM, _SHADOW_RIS;
            _DEBUG_OVERLAY;
        }

        hlsl
        {
            void MSMain() {}
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
        Assert.IsNotNull(syntax.Keywords);
        Assert.HasCount(1, syntax.Keywords.Groups);
        Assert.AreEqual("GLOBAL_FEATURE", syntax.Keywords.Groups[0].Name);

        Assert.HasCount(1, syntax.Passes);
        var passSyntax = syntax.Passes[0];
        Assert.IsNotNull(passSyntax.Keywords);
        Assert.HasCount(2, passSyntax.Keywords.Groups);
        Assert.AreEqual("SHADOW_MODE", passSyntax.Keywords.Groups[0].Name);
        Assert.HasCount(3, passSyntax.Keywords.Groups[0].Keywords);
        Assert.AreEqual("_DEBUG_OVERLAY", passSyntax.Keywords.Groups[1].Name);
        Assert.HasCount(1, passSyntax.Keywords.Groups[1].Keywords);

        var semanticsResult = DSLShaderCompiler.GetShaderSemantics(syntax);
        Assert.IsTrue(semanticsResult.IsSuccess, $"Failed to get semantics: {semanticsResult.Message}");

        var descriptorResult = DSLShaderCompiler.ResolveShader(semanticsResult.Value);
        Assert.IsTrue(descriptorResult.IsSuccess, $"Failed to resolve shader: {descriptorResult.Message}");

        var descriptor = descriptorResult.Value;
        Assert.HasCount(1, descriptor.Passes);
        var passDesc = descriptor.Passes[0];
        // 1 global group + 2 pass groups = 3 groups combined
        Assert.HasCount(3, passDesc.keywordGroups);
        Assert.AreEqual("GLOBAL_FEATURE", passDesc.keywordGroups[0].Name);
        Assert.AreEqual("SHADOW_MODE", passDesc.keywordGroups[1].Name);
        CollectionAssert.AreEqual(new[] { "_SHADOW_PCF", "_SHADOW_VSM", "_SHADOW_RIS" }, passDesc.keywordGroups[1].Keywords);
        Assert.AreEqual("_DEBUG_OVERLAY", passDesc.keywordGroups[2].Name);
        CollectionAssert.AreEqual(new[] { "_DEBUG_OVERLAY" }, passDesc.keywordGroups[2].Keywords);
    }

    [TestMethod]
    public void TestParseComputeShader_WithKeywords()
    {
        var computeSource = @"
compute ""Custom/ComputeKeywordsTest""
{
    keywords
    {
        CULL_MODE: _CULL_FRUSTUM, _CULL_HZB;
        _INDIRECT_DISPATCH;
    }

    hlsl
    {
        void CSMain() {}
    }

    cs ""hlsl_block"" : ""CSMain"";
}
";

        var syntaxResult = DSLShaderCompiler.ParseComputeShaderSyntax(computeSource);
        Assert.IsTrue(syntaxResult.IsSuccess, $"Failed to parse syntax: {syntaxResult.Message}");

        var syntax = syntaxResult.Value;
        Assert.IsNotNull(syntax.Keywords);
        Assert.HasCount(2, syntax.Keywords.Groups);
        Assert.AreEqual("CULL_MODE", syntax.Keywords.Groups[0].Name);
        CollectionAssert.AreEqual(new[] { "_CULL_FRUSTUM", "_CULL_HZB" }, syntax.Keywords.Groups[0].Keywords);
        Assert.AreEqual("_INDIRECT_DISPATCH", syntax.Keywords.Groups[1].Name);
        CollectionAssert.AreEqual(new[] { "_INDIRECT_DISPATCH" }, syntax.Keywords.Groups[1].Keywords);

        var semanticsResult = DSLShaderCompiler.GetShaderSemantics(syntax);
        Assert.IsTrue(semanticsResult.IsSuccess, $"Failed to get semantics: {semanticsResult.Message}");

        var descriptorResult = DSLShaderCompiler.ResolveShader(semanticsResult.Value);
        Assert.IsTrue(descriptorResult.IsSuccess, $"Failed to resolve compute shader: {descriptorResult.Message}");

        var descriptor = descriptorResult.Value;
        Assert.HasCount(2, descriptor.KeywordGroups);
        Assert.AreEqual("CULL_MODE", descriptor.KeywordGroups[0].Name);
        CollectionAssert.AreEqual(new[] { "_CULL_FRUSTUM", "_CULL_HZB" }, descriptor.KeywordGroups[0].Keywords);
        Assert.AreEqual("_INDIRECT_DISPATCH", descriptor.KeywordGroups[1].Name);
        CollectionAssert.AreEqual(new[] { "_INDIRECT_DISPATCH" }, descriptor.KeywordGroups[1].Keywords);
    }
}
