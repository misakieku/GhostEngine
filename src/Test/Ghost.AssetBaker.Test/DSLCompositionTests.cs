using Ghost.Core;
using Ghost.DSL.Composition;
using Ghost.DSL.ShaderCompiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Ghost.AssetForge.Test;

[TestClass]
public class DSLCompositionTests
{
    private static ShaderWorkspace BuildTestWorkspace()
    {
        var interfacesDsl = @"
module ""Ghost.Rendering.Interfaces""
{
    export interface shader IBSDF { static float3 Evaluate(); }
    export interface pipeline IShadow { static float Evaluate(); }
    export interface pipeline IFog { static float3 Apply(); }
}
";

        var standardFeaturesDsl = @"
module ""Ghost.Rendering.StandardFeatures""
{
    import ""Ghost.Rendering.Interfaces"";

    export implementation CSMPCFShadow : IShadow { static float Evaluate() { return 1.0f; } }
    export implementation VSMShadow : IShadow
    {
        provider = ""Ghost.Rendering.VirtualShadowMapFeature"";
        static float Evaluate() { return 0.8f; }
    }
    export implementation NoShadow : IShadow { static float Evaluate() { return 1.0f; } }

    export implementation NoFog : IFog { static float3 Apply() { return float3(0,0,0); } }
    export implementation VolumetricFog : IFog { static float3 Apply() { return float3(1,1,1); } }
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

        pass ""DepthOnly""
        {
            mesh ""Depth.hlsl"" : ""DepthMain"";
        }

        pass ""GBuffer""
        {
            compose
            {
                IBSDF;
            }
            mesh ""GBuffer.hlsl"" : ""GBufferMS"";
            pixel ""GBuffer.hlsl"" : ""GBufferPS"";
        }

        pass ""DeferredLighting""
        {
            compose
            {
                IBSDF;
                IShadow;
                IFog;
            }
            pixel ""Deferred.hlsl"" : ""DeferredPS"";
        }
    }
}
";

        var metalMaterialDsl = @"
module ""Game.Materials""
{
    import ""Ghost.Rendering.Interfaces"";
    import ""Ghost.Rendering.Lit"";

    export shader ""Game/Metal"" : ""Ghost.Rendering.Lit.LitTemplate""
    {
        implementation MetalGGX : IBSDF
        {
            static float3 Evaluate() { return float3(1,1,1); }
        }

        bind
        {
            IBSDF = MetalGGX;
        }
    }

    export shader ""Game/Cloth"" : ""Ghost.Rendering.Lit.LitTemplate""
    {
        implementation ClothBSDF : IBSDF
        {
            static float3 Evaluate() { return float3(0.5, 0.5, 0.5); }
        }

        bind
        {
            IBSDF = ClothBSDF;
        }
    }
}
";

        var workspace = new ShaderWorkspace();
        workspace.IndexDocument("Interfaces.gshdr", DSLShaderCompiler.ParseDSLDocument(interfacesDsl).Value);
        workspace.IndexDocument("StandardFeatures.gshdr", DSLShaderCompiler.ParseDSLDocument(standardFeaturesDsl).Value);
        workspace.IndexDocument("LitTemplate.gshdr", DSLShaderCompiler.ParseDSLDocument(litTemplateDsl).Value);
        workspace.IndexDocument("Materials.gshdr", DSLShaderCompiler.ParseDSLDocument(metalMaterialDsl).Value);

        workspace.ResolveAndValidate().ThrowIfFailed();
        return workspace;
    }

    [TestMethod]
    public void CompositionKey_IsDeterministicAndCanonical()
    {
        var binding1 = (100UL, 500UL);
        var binding2 = (200UL, 600UL);
        var binding3 = (300UL, 700UL);

        var keyOrderA = CompositionKey.Compute(new[] { binding1, binding2, binding3 });
        var keyOrderB = CompositionKey.Compute(new[] { binding3, binding1, binding2 });
        var keyOrderC = CompositionKey.Compute(new[] { binding2, binding3, binding1 });

        Assert.AreNotEqual(0UL, keyOrderA);
        Assert.AreEqual(keyOrderA, keyOrderB);
        Assert.AreEqual(keyOrderA, keyOrderC);
    }

    [TestMethod]
    public void ResolveShaderComposition_ComputesPassLocalCartesianProduct()
    {
        var workspace = BuildTestWorkspace();
        var result = workspace.ResolveShaderComposition("Game.Materials.Game/Metal");
        Assert.IsTrue(result.IsSuccess, result.Message);

        var comp = result.Value;
        Assert.AreEqual(3, comp.Passes.Count);

        // 1. DepthOnly Pass: 0 composed interfaces -> 1 shared specialization
        var depthPass = comp.Passes[0];
        Assert.AreEqual("DepthOnly", depthPass.PassName);
        Assert.IsTrue(depthPass.IsTemplateShared);
        Assert.IsNotNull(depthPass.TemplatePassId);
        Assert.AreEqual(1, depthPass.Specializations.Count);
        Assert.AreEqual(0UL, depthPass.Specializations[0].CompositionKey);

        // 2. GBuffer Pass: 1 composed shader interface (IBSDF) -> 1 specialization
        var gbufferPass = comp.Passes[1];
        Assert.AreEqual("GBuffer", gbufferPass.PassName);
        Assert.IsFalse(gbufferPass.IsTemplateShared);
        Assert.AreEqual(1, gbufferPass.Specializations.Count);
        Assert.AreNotEqual(0UL, gbufferPass.Specializations[0].CompositionKey);
        StringAssert.Contains(gbufferPass.Specializations[0].CompilerDefines[0], "GHOST_IMPL_IBSDF=Game__Materials__Game__Metal__MetalGGX");

        // 3. DeferredLighting Pass: 1 IBSDF * 3 IShadow * 2 IFog -> 6 specializations
        var deferredPass = comp.Passes[2];
        Assert.AreEqual("DeferredLighting", deferredPass.PassName);
        Assert.IsFalse(deferredPass.IsTemplateShared);
        Assert.AreEqual(6, deferredPass.Specializations.Count);

        // Verify that all 6 specializations have unique composition keys
        var distinctKeys = deferredPass.Specializations.Select(s => s.CompositionKey).Distinct().ToList();
        Assert.AreEqual(6, distinctKeys.Count);

        // Total specializations = 1 + 1 + 6 = 8
        Assert.AreEqual(8, comp.TotalSpecializationCount);
    }

    [TestMethod]
    public void ResolveShaderComposition_TemplatePassSharing_IdenticalKeysAcrossShaders()
    {
        var workspace = BuildTestWorkspace();

        var metalComp = workspace.ResolveShaderComposition("Game.Materials.Game/Metal").Value;
        var clothComp = workspace.ResolveShaderComposition("Game.Materials.Game/Cloth").Value;

        // DepthOnly is template-shared: identical TemplatePassId and composition key
        var metalDepth = metalComp.Passes.First(p => p.PassName == "DepthOnly");
        var clothDepth = clothComp.Passes.First(p => p.PassName == "DepthOnly");

        Assert.IsTrue(metalDepth.IsTemplateShared);
        Assert.IsTrue(clothDepth.IsTemplateShared);
        Assert.AreEqual(metalDepth.TemplatePassId, clothDepth.TemplatePassId);
        Assert.AreEqual(metalDepth.Specializations[0].CompositionKey, clothDepth.Specializations[0].CompositionKey);

        // GBuffer differs per material: different composition keys
        var metalGBuffer = metalComp.Passes.First(p => p.PassName == "GBuffer");
        var clothGBuffer = clothComp.Passes.First(p => p.PassName == "GBuffer");

        Assert.AreNotEqual(metalGBuffer.Specializations[0].CompositionKey, clothGBuffer.Specializations[0].CompositionKey);
        StringAssert.Contains(metalGBuffer.Specializations[0].CompilerDefines[0], "MetalGGX");
        StringAssert.Contains(clothGBuffer.Specializations[0].CompilerDefines[0], "ClothBSDF");
    }

    [TestMethod]
    public void ResolveShaderComposition_CollectsRequiredFeatureProviders()
    {
        var workspace = BuildTestWorkspace();
        var comp = workspace.ResolveShaderComposition("Game.Materials.Game/Metal").Value;

        var deferredPass = comp.Passes.First(p => p.PassName == "DeferredLighting");

        // Exactly 2 specializations should require VirtualShadowMapFeature (VSM with NoFog, VSM with VolumetricFog)
        var vsmSpecs = deferredPass.Specializations.Where(s => s.RequiredFeatureProviders.Contains("Ghost.Rendering.VirtualShadowMapFeature")).ToList();
        Assert.AreEqual(2, vsmSpecs.Count);
    }
}
