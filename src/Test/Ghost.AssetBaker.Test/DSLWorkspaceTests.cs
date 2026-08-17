using Ghost.DSL.ShaderCompiler;
using Ghost.DSL.Symbols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.AssetForge.Test;

[TestClass]
public class DSLWorkspaceTests
{
    private string _tempDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GhostDSLWorkspaceTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Teardown()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch { }
        }
    }

    [TestMethod]
    public void Resolve_MultiModuleEngineAndGame_Succeeds()
    {
        var interfacesDsl = @"
module ""Ghost.Rendering.Interfaces""
{
    export interface shader IBSDF
    {
        static float3 Evaluate(in BSDFContext ctx, in SurfaceData s, inout Payload p);
    }

    export interface pipeline IShadow
    {
        static float EvaluateShadow(in ShadowContext ctx, inout Payload p);
    }

    export interface pipeline IFog
    {
        static float3 ApplyFog(in FogContext ctx, in float3 radiance, inout Payload p);
    }
}
";

        var standardFeaturesDsl = @"
module ""Ghost.Rendering.StandardFeatures""
{
    import ""Ghost.Rendering.Interfaces"";

    export implementation NoShadow : IShadow
    {
        static float EvaluateShadow(in ShadowContext ctx, inout Payload p) { return 1.0f; }
    }

    export implementation CSMPCFShadow : IShadow
    {
        static float EvaluateShadow(in ShadowContext ctx, inout Payload p) { return 0.5f; }
    }

    export implementation NoFog : IFog
    {
        static float3 ApplyFog(in FogContext ctx, in float3 radiance, inout Payload p) { return radiance; }
    }
}
";

        var litTemplateDsl = @"
module ""Ghost.Rendering.Lit""
{
    import ""Ghost.Rendering.Interfaces"";
    import ""Ghost.Rendering.StandardFeatures"";

    export template ""LitTemplate""
    {
        slot
        {
            IBSDF;
            IShadow = CSMPCFShadow;
            IFog = NoFog;
        }

        pass ""GBuffer""
        {
            compose
            {
                IBSDF;
            }
            mesh ""GBuffer.hlsl"" : ""MainMS"";
            pixel ""GBuffer.hlsl"" : ""MainPS"";
        }

        pass ""DeferredLighting""
        {
            compose
            {
                IBSDF;
                IShadow;
                IFog;
            }
            pixel ""Deferred.hlsl"" : ""DeferredLightingMain"";
        }

        pass ""DepthOnly""
        {
            mesh ""Depth.hlsl"" : ""DepthMain"";
        }
    }
}
";

        var gameMaterialsDsl = @"
module ""Game.Materials""
{
    import ""Ghost.Rendering.Interfaces"";
    import ""Ghost.Rendering.Lit"";

    export shader ""Game/Lit"" : ""Ghost.Rendering.Lit.LitTemplate""
    {
        payload
        {
            float directSpecularStrength;
        }

        implementation CustomGGX : IBSDF
        {
            static float3 Evaluate(in BSDFContext ctx, in SurfaceData s, inout Payload p)
            {
                return s.albedo * p.directSpecularStrength;
            }
        }

        bind
        {
            IBSDF = CustomGGX;
        }
    }
}
";

        var workspace = new ShaderWorkspace();
        workspace.IndexDocument("Interfaces.gshdr", DSLShaderCompiler.ParseDSLDocument(interfacesDsl).Value);
        workspace.IndexDocument("StandardFeatures.gshdr", DSLShaderCompiler.ParseDSLDocument(standardFeaturesDsl).Value);
        workspace.IndexDocument("LitTemplate.gshdr", DSLShaderCompiler.ParseDSLDocument(litTemplateDsl).Value);
        workspace.IndexDocument("GameMaterials.gshdr", DSLShaderCompiler.ParseDSLDocument(gameMaterialsDsl).Value);

        var resolveResult = workspace.ResolveAndValidate();
        Assert.IsTrue(resolveResult.IsSuccess, resolveResult.Message);

        // Verify Modules
        Assert.AreEqual(4, workspace.Modules.Count);

        // Verify Template
        var tmpl = workspace.Templates.Values.FirstOrDefault(t => t.QualifiedName == "Ghost.Rendering.Lit.LitTemplate");
        Assert.IsNotNull(tmpl);
        Assert.AreEqual(3, tmpl.Slots.Count);
        Assert.AreEqual(3, tmpl.Passes.Count);

        // Verify Pass Compose IDs
        var deferredPass = tmpl.Passes.First(p => p.Name == "DeferredLighting");
        Assert.AreEqual(3, deferredPass.ComposedInterfaceIds.Count);

        // Verify Shader
        var shdr = workspace.Shaders.Values.FirstOrDefault(s => s.QualifiedName == "Game.Materials.Game/Lit");
        Assert.IsNotNull(shdr);
        Assert.AreEqual(tmpl.Id, shdr.BaseTemplateId);
        Assert.AreEqual(1, shdr.Bindings.Count);
        Assert.IsNotNull(shdr.PayloadBody);

        // Verify Packaged Pipeline Implementations for IShadow
        var ishadow = workspace.Interfaces.Values.First(i => i.QualifiedName == "Ghost.Rendering.Interfaces.IShadow");
        var shadowImpls = workspace.GetPackagedPipelineImplementations(ishadow.Id);
        Assert.AreEqual(2, shadowImpls.Count);
        CollectionAssert.AreEquivalent(new[] { "Ghost.Rendering.StandardFeatures.NoShadow", "Ghost.Rendering.StandardFeatures.CSMPCFShadow" },
            shadowImpls.Select(s => s.QualifiedName).ToArray());
    }

    [TestMethod]
    public void Resolve_PluginExtendsOpenPipelineInterface_ParticipatesInDomain()
    {
        var ifaceDsl = @"
module ""Ghost.Rendering.Interfaces""
{
    export interface pipeline IShadow
    {
        static float EvaluateShadow(in ShadowContext ctx, inout Payload p);
    }
}
";

        var engineShadowsDsl = @"
module ""Ghost.Rendering.StandardFeatures""
{
    import ""Ghost.Rendering.Interfaces"";
    export implementation CSMPCF : IShadow { static float EvaluateShadow(in ShadowContext ctx, inout Payload p) { return 1; } }
}
";

        var pluginShadowsDsl = @"
module ""Game.ExperimentalShadows""
{
    import ""Ghost.Rendering.Interfaces"";
    export implementation StochasticShadowMap : IShadow
    {
        provider = ""Game.Rendering.StochasticShadowFeature"";
        static float EvaluateShadow(in ShadowContext ctx, inout Payload p) { return 0.5f; }
    }
}
";

        var workspace = new ShaderWorkspace();
        workspace.IndexDocument("Interfaces.gshdr", DSLShaderCompiler.ParseDSLDocument(ifaceDsl).Value);
        workspace.IndexDocument("Standard.gshdr", DSLShaderCompiler.ParseDSLDocument(engineShadowsDsl).Value);
        workspace.IndexDocument("Plugin.gshdr", DSLShaderCompiler.ParseDSLDocument(pluginShadowsDsl).Value);

        var result = workspace.ResolveAndValidate();
        Assert.IsTrue(result.IsSuccess, result.Message);

        var ishadow = workspace.Interfaces.Values.First(i => i.QualifiedName == "Ghost.Rendering.Interfaces.IShadow");
        var impls = workspace.GetPackagedPipelineImplementations(ishadow.Id);

        Assert.AreEqual(2, impls.Count);
        Assert.IsTrue(impls.Any(i => i.QualifiedName == "Game.ExperimentalShadows.StochasticShadowMap"));
        Assert.IsTrue(impls.Any(i => i.QualifiedName == "Ghost.Rendering.StandardFeatures.CSMPCF"));
    }

    [TestMethod]
    public void Resolve_ModuleCycle_ReportsError()
    {
        var modADsl = @"
module ""ModuleA""
{
    import ""ModuleB"";
}
";
        var modBDsl = @"
module ""ModuleB""
{
    import ""ModuleC"";
}
";
        var modCDsl = @"
module ""ModuleC""
{
    import ""ModuleA"";
}
";

        var workspace = new ShaderWorkspace();
        workspace.IndexDocument("A.gshdr", DSLShaderCompiler.ParseDSLDocument(modADsl).Value);
        workspace.IndexDocument("B.gshdr", DSLShaderCompiler.ParseDSLDocument(modBDsl).Value);
        workspace.IndexDocument("C.gshdr", DSLShaderCompiler.ParseDSLDocument(modCDsl).Value);

        var result = workspace.ResolveAndValidate();
        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.Message, "Circular module dependency");
    }

    [TestMethod]
    public void Resolve_ShaderBindsPipelineInterface_FailsValidation()
    {
        var ifaceDsl = @"
module ""Rendering.Interfaces""
{
    export interface pipeline IShadow { static float Evaluate(); }
}
";
        var templateDsl = @"
module ""Rendering.Lit""
{
    import ""Rendering.Interfaces"";
    export template ""LitTemplate""
    {
        slot { IShadow; }
    }
}
";
        var shaderDsl = @"
module ""Game.Materials""
{
    import ""Rendering.Interfaces"";
    import ""Rendering.Lit"";

    export implementation MyShadow : IShadow { static float Evaluate() { return 1; } }

    export shader ""Game/Lit"" : ""Rendering.Lit.LitTemplate""
    {
        bind
        {
            IShadow = MyShadow;
        }
    }
}
";

        var workspace = new ShaderWorkspace();
        workspace.IndexDocument("Iface.gshdr", DSLShaderCompiler.ParseDSLDocument(ifaceDsl).Value);
        workspace.IndexDocument("Template.gshdr", DSLShaderCompiler.ParseDSLDocument(templateDsl).Value);
        workspace.IndexDocument("Shader.gshdr", DSLShaderCompiler.ParseDSLDocument(shaderDsl).Value);

        var result = workspace.ResolveAndValidate();
        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.Message, "cannot bind pipeline interface");
    }

    [TestMethod]
    public void Resolve_ShaderBindsClosedInterface_FailsValidation()
    {
        var ifaceDsl = @"
module ""Rendering.Interfaces""
{
    export closed interface shader IClosedSurface
    {
        static float4 Encode();
    }

    export implementation StandardEncoding : IClosedSurface
    {
        static float4 Encode() { return float4(0,0,0,0); }
    }
}
";
        var templateDsl = @"
module ""Rendering.Lit""
{
    import ""Rendering.Interfaces"";
    export template ""LitTemplate""
    {
        slot { IClosedSurface = StandardEncoding; }
    }
}
";
        var shaderDsl = @"
module ""Game.Materials""
{
    import ""Rendering.Interfaces"";
    import ""Rendering.Lit"";

    implementation HackEncoding : IClosedSurface { static float4 Encode() { return float4(1,1,1,1); } }

    export shader ""Game/Lit"" : ""Rendering.Lit.LitTemplate""
    {
        bind
        {
            IClosedSurface = HackEncoding;
        }
    }
}
";

        var workspace = new ShaderWorkspace();
        workspace.IndexDocument("Iface.gshdr", DSLShaderCompiler.ParseDSLDocument(ifaceDsl).Value);
        workspace.IndexDocument("Template.gshdr", DSLShaderCompiler.ParseDSLDocument(templateDsl).Value);
        workspace.IndexDocument("Shader.gshdr", DSLShaderCompiler.ParseDSLDocument(shaderDsl).Value);

        var result = workspace.ResolveAndValidate();
        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.Message, "cannot bind closed interface");
    }

    [TestMethod]
    public void CreateFromAssetDirectories_AutoDiscoversAndResolves()
    {
        var engineAssetDir = Path.Combine(_tempDir, "EngineAssets");
        var gameAssetDir = Path.Combine(_tempDir, "GameAssets");
        Directory.CreateDirectory(engineAssetDir);
        Directory.CreateDirectory(gameAssetDir);

        File.WriteAllText(Path.Combine(engineAssetDir, "Interfaces.gshdr"), @"
module ""Ghost.Rendering.Interfaces""
{
    export interface shader IBSDF { static float3 Evaluate(); }
    export interface pipeline IShadow { static float Evaluate(); }
}
");

        File.WriteAllText(Path.Combine(engineAssetDir, "LitTemplate.gshdr"), @"
module ""Ghost.Rendering.Lit""
{
    import ""Ghost.Rendering.Interfaces"";

    export template ""LitTemplate""
    {
        slot { IBSDF; IShadow; }
        pass ""Main"" { compose { IBSDF; IShadow; } }
    }
}
");

        File.WriteAllText(Path.Combine(gameAssetDir, "PlayerMaterial.gshdr"), @"
module ""Game.Materials""
{
    import ""Ghost.Rendering.Interfaces"";
    import ""Ghost.Rendering.Lit"";

    export shader ""Game/Player"" : ""Ghost.Rendering.Lit.LitTemplate""
    {
        implementation PlayerBSDF : IBSDF
        {
            static float3 Evaluate() { return float3(1, 0, 0); }
        }

        bind
        {
            IBSDF = PlayerBSDF;
        }
    }
}
");

        var result = ShaderWorkspace.CreateFromAssetDirectories(new[] { engineAssetDir, gameAssetDir });
        Assert.IsTrue(result.IsSuccess, result.Message);

        var workspace = result.Value;
        Assert.AreEqual(3, workspace.Modules.Count);
        Assert.AreEqual(2, workspace.Interfaces.Count);
        Assert.AreEqual(1, workspace.Templates.Count);
        Assert.AreEqual(1, workspace.Shaders.Count);

        var playerShader = workspace.Shaders.Values.First();
        Assert.AreEqual("Game.Materials.Game/Player", playerShader.QualifiedName);
        Assert.AreEqual("Ghost.Rendering.Lit.LitTemplate", playerShader.BaseTemplateQualifiedName);
    }
    [TestMethod]
    public void Resolve_UnimportedModule_FailsResolution()
    {
        var modADsl = @"
module ""ModuleA""
{
    export interface shader ISecretInterface { static float Evaluate(); }
}
";
        var modBDsl = @"
module ""ModuleB""
{
    // Intentionally omitted: import ""ModuleA"";

    export template ""TestTemplate""
    {
        slot { ISecretInterface; }
    }
}
";

        var workspace = new ShaderWorkspace();
        workspace.IndexDocument("A.gshdr", DSLShaderCompiler.ParseDSLDocument(modADsl).Value);
        workspace.IndexDocument("B.gshdr", DSLShaderCompiler.ParseDSLDocument(modBDsl).Value);

        var result = workspace.ResolveAndValidate();
        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.Message, "references unknown interface 'ISecretInterface'");
    }

    [TestMethod]
    public void Resolve_UnexportedPrivateSymbol_IsHiddenFromImporter()
    {
        var modADsl = @"
module ""ModuleA""
{
    // Notice: NOT exported (private to ModuleA)
    interface shader IPrivateInterface { static float Evaluate(); }
}
";
        var modBDsl = @"
module ""ModuleB""
{
    import ""ModuleA"";

    export template ""TestTemplate""
    {
        slot { IPrivateInterface; }
    }
}
";

        var workspace = new ShaderWorkspace();
        workspace.IndexDocument("A.gshdr", DSLShaderCompiler.ParseDSLDocument(modADsl).Value);
        workspace.IndexDocument("B.gshdr", DSLShaderCompiler.ParseDSLDocument(modBDsl).Value);

        var result = workspace.ResolveAndValidate();
        Assert.IsTrue(result.IsFailure);
        StringAssert.Contains(result.Message, "references unknown interface 'IPrivateInterface'");
    }
}
