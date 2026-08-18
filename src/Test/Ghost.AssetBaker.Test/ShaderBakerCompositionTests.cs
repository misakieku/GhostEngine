using System.Collections.Concurrent;
using System.IO;
using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.Composition;
using Ghost.DSL.Models;
using Ghost.DSL.ShaderCompiler;
using Ghost.DSL.Symbols;
using Ghost.DSL.Syntax.Symbols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.AssetForge.Test;

[TestClass]
public class ShaderBakerCompositionTests
{
    private string _tempDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GhostShaderBakerTest_" + Guid.NewGuid());
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
    public async Task HLSLCodeGenerator_GeneratesValidPreambleAndMangledStructs()
    {
        var interfacesDsl = @"
module ""Ghost.Rendering.Interfaces""
{
    export interface shader IBSDF
    {
        static float3 Evaluate(in BSDFContext ctx, in SurfaceData s, inout Payload p);
    }
}
";

        var templateDsl = @"
module ""Ghost.Rendering.Lit""
{
    import ""Ghost.Rendering.Interfaces"";

    export template ""LitTemplate""
    {
        slot { IBSDF; }
        pass ""Main""
        {
            compose { IBSDF; }
            hlsl
            {
                float4 MainPS() : SV_Target { return float4(1,1,1,1); }
            }
        }
    }
}
";

        var shaderDsl = @"
module ""Game.Materials""
{
    import ""Ghost.Rendering.Interfaces"";
    import ""Ghost.Rendering.Lit"";

    export shader ""Game/Lit"" : ""Ghost.Rendering.Lit.LitTemplate""
    {
        payload
        {
            float specularMultiplier;
        }

        implementation CustomGGX : IBSDF
        {
            static float3 Evaluate(in BSDFContext ctx, in SurfaceData s, inout Payload p)
            {
                return s.albedo * p.specularMultiplier;
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
        workspace.IndexDocument("LitTemplate.gshdr", DSLShaderCompiler.ParseDSLDocument(templateDsl).Value);
        workspace.IndexDocument("GameLit.gshdr", DSLShaderCompiler.ParseDSLDocument(shaderDsl).Value);
        workspace.ResolveAndValidate().ThrowIfFailed();

        var comp = workspace.ResolveShaderComposition("Game.Materials.Game/Lit").Value;
        var passSet = comp.Passes[0];
        var spec = passSet.Specializations[0];

        var hlslResult = HLSLCodeGenerator.GeneratePassHLSL(
            passSet.Syntax,
            spec,
            comp.Shader.PayloadBody,
            null,
            null,
            null,
            null);

        Assert.IsTrue(hlslResult.IsSuccess, hlslResult.Message);
        var hlsl = hlslResult.Value;

        // Verify Payload struct
        StringAssert.Contains(hlsl, "struct Payload");
        StringAssert.Contains(hlsl, "float specularMultiplier;");

        // Verify Mangled Implementation Struct
        StringAssert.Contains(hlsl, "struct Game__Materials__Game__Lit__CustomGGX");
        StringAssert.Contains(hlsl, "s.albedo * p.specularMultiplier");

        // Verify Pass HLSL code
        StringAssert.Contains(hlsl, "MainPS");
    }

    [TestMethod]
    public async Task ShaderBaker_BakesV3BinaryPackage_WithCompositionsAndSharedPass()
    {
        var interfacesDsl = @"
module ""Ghost.Rendering.Interfaces""
{
    export interface shader IBSDF { static float3 Evaluate(); }
    export interface pipeline IShadow { static float Evaluate(); }
}
";

        var standardFeaturesDsl = @"
module ""Ghost.Rendering.StandardFeatures""
{
    import ""Ghost.Rendering.Interfaces"";
    export implementation CSMPCFShadow : IShadow { static float Evaluate() { return 1.0f; } }
    export implementation NoShadow : IShadow { static float Evaluate() { return 1.0f; } }
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
        }

        pass ""DepthOnly""
        {
            hlsl
            {
                float4 MainPS() : SV_Target { return float4(0,0,0,1); }
            }
        }

        pass ""DeferredLighting""
        {
            compose
            {
                IBSDF;
                IShadow;
            }
            hlsl
            {
                float4 MainPS() : SV_Target { return float4(1,1,1,1); }
            }
        }
    }
}
";

        var gameMaterialDsl = @"
module ""Game.Materials""
{
    import ""Ghost.Rendering.Interfaces"";
    import ""Ghost.Rendering.Lit"";

    export shader ""Game/Metal"" : ""Ghost.Rendering.Lit.LitTemplate""
    {
        payload
        {
            float metalness;
        }

        implementation MetalBSDF : IBSDF
        {
            static float3 Evaluate() { return float3(1,1,1); }
        }

        bind
        {
            IBSDF = MetalBSDF;
        }
    }
}
";

        var ifacePath = Path.Combine(_tempDir, "Interfaces.gshdr");
        var stdPath = Path.Combine(_tempDir, "StandardFeatures.gshdr");
        var tmplPath = Path.Combine(_tempDir, "LitTemplate.gshdr");
        var matPath = Path.Combine(_tempDir, "GameMetal.gshdr");

        await File.WriteAllTextAsync(ifacePath, interfacesDsl);
        await File.WriteAllTextAsync(stdPath, standardFeaturesDsl);
        await File.WriteAllTextAsync(tmplPath, litTemplateDsl);
        await File.WriteAllTextAsync(matPath, gameMaterialDsl);

        var workspace = ShaderWorkspace.CreateFromAssetDirectories(new[] { _tempDir }).Value;
        var sharedCache = new ConcurrentDictionary<ulong, (ShaderStage stage, byte[] bytecode)[]>();

        var ctx = new AssetBakerContext
        {
            ShaderMetadata = new ShaderMetadata(),
            ShaderWorkspace = workspace,
            SharedPassBytecodeCache = sharedCache,
            AssetDirectories = new[] { _tempDir }
        };

        using var baker = new ShaderBaker();
        using var dstStream = new MemoryStream();

        await baker.BakeAssetAsync(matPath, dstStream, new ShaderBakeSettings(), ctx, CancellationToken.None);

        Assert.IsTrue(dstStream.Length > 0, "Baked output stream should not be empty.");

        // Read and validate binary header v3
        dstStream.Position = 0;
        using var reader = new BinaryReader(dstStream);

        var magic = reader.ReadUInt32();
        var version = reader.ReadUInt32();
        var shaderType = (ShaderType)reader.ReadUInt32();
        var passCount = reader.ReadUInt32();

        Assert.AreEqual(ShaderContentHeader.MAGIC, magic);
        Assert.AreEqual(3u, version);
        Assert.AreEqual(ShaderType.Graphics, shaderType);
        Assert.AreEqual(2u, passCount); // DepthOnly + DeferredLighting

        // Verify that DepthOnly was cached in shared bytecode cache
        var tmpl = workspace.Templates.Values.First(t => t.QualifiedName == "Ghost.Rendering.Lit.LitTemplate");
        var depthPassId = SymbolId.Compute($"{tmpl.QualifiedName}.DepthOnly");
        Assert.IsTrue(sharedCache.ContainsKey(depthPassId), "DepthOnly pass should be cached in SharedPassBytecodeCache.");

        // Bake a second shader deriving from the same template -> should reuse cached DepthOnly bytecode
        var clothMaterialDsl = @"
module ""Game.Materials""
{
    import ""Ghost.Rendering.Interfaces"";
    import ""Ghost.Rendering.Lit"";

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
        var clothPath = Path.Combine(_tempDir, "GameCloth.gshdr");
        await File.WriteAllTextAsync(clothPath, clothMaterialDsl);

        // Re-index cloth
        workspace.IndexDocument(clothPath, DSLShaderCompiler.ParseDSLDocument(clothMaterialDsl).Value);
        workspace.ResolveAndValidate().ThrowIfFailed();

        using var dstClothStream = new MemoryStream();
        await baker.BakeAssetAsync(clothPath, dstClothStream, new ShaderBakeSettings(), ctx, CancellationToken.None);

        Assert.IsTrue(dstClothStream.Length > 0);
    }
}
