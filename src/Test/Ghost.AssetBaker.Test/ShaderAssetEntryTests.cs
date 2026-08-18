using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.Composition;
using Ghost.DSL.Models;
using Ghost.DSL.ShaderCompiler;
using Ghost.Engine.Streaming;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.AssetForge.Test;

[TestClass]
public class ShaderAssetEntryTests
{
    private string _tempDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GhostShaderAssetEntryTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [TestMethod]
    public async Task TestShaderAssetEntry_LoadsHardenedV3Package()
    {
        var interfacesDsl = @"
module ""Ghost.Rendering.Interfaces""
{
    export interface shader IBSDF;
}
";
        var litTemplateDsl = @"
module ""Ghost.Rendering.Lit""
{
    import ""Ghost.Rendering.Interfaces"";

    export template LitTemplate
    {
        properties
        {
            Float4 BaseColor;
            Float Roughness;
            Float Metallic;
        }

        slot
        {
            IBSDF;
        }

        pass ""DeferredLighting""
        {
            compose
            {
                IBSDF;
            }

            hlsl
            {
                float4 MainPS() : SV_Target
                {
                    return float4(1.0, 1.0, 1.0, 1.0);
                }
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

    export shader ""Game/LitMat"" : ""Ghost.Rendering.Lit.LitTemplate""
    {
        implementation GGXBSDF : IBSDF
        {
            static float3 Evaluate() { return float3(1.0, 1.0, 1.0); }
        }

        properties
        {
            Float DirectSpecularStrength;
        }

        bind
        {
            IBSDF = GGXBSDF;
        }
    }
}
";
        var ifacePath = Path.Combine(_tempDir, "Interfaces.gmod");
        var templatePath = Path.Combine(_tempDir, "LitTemplate.gshdr");
        var shaderPath = Path.Combine(_tempDir, "LitMat.gshdr");

        await File.WriteAllTextAsync(ifacePath, interfacesDsl);
        await File.WriteAllTextAsync(templatePath, litTemplateDsl);
        await File.WriteAllTextAsync(shaderPath, gameMaterialDsl);

        var workspace = ShaderWorkspace.CreateFromAssetDirectories(new[] { _tempDir }).GetValueOrThrow();
        var sharedCache = new ConcurrentDictionary<ulong, (ShaderStage stage, byte[] bytecode)[]>();

        var context = new AssetBakerContext
        {
            ShaderMetadata = new ShaderMetadata(),
            AssetDirectories = new[] { _tempDir },
            ShaderWorkspace = workspace,
            SharedPassBytecodeCache = sharedCache
        };

        using var baker = new ShaderBaker();
        using var outputStream = new MemoryStream();
        await baker.BakeAssetAsync(shaderPath, outputStream, new ShaderBakeSettings(), context, CancellationToken.None);

        Assert.IsTrue(outputStream.Length > 0);

        outputStream.Position = 0;

        // Test ShaderAssetEntry load
        var assetId = Guid.NewGuid();
        var entry = new ShaderAssetEntry(null!, null!, null!, assetId, Array.Empty<Guid>());
        entry.AddRef();
        entry.State = AssetState.Ready;
        var loadResult = entry.OnLoadContent(outputStream);

        var header = default(ShaderContentHeader);
        entry.ReadAssetData(ref header);

        Assert.AreEqual(ShaderContentHeader.MAGIC, header.magic);
        Assert.AreEqual(3u, header.version);
        Assert.AreEqual(ShaderType.Graphics, header.shaderType);
        Assert.AreEqual(1u, header.passCount);
        Assert.AreNotEqual(0UL, header.shaderId);
        Assert.AreNotEqual(0UL, header.schemaId);
        Assert.AreEqual(32u, header.propertyBufferSize); // 24 bytes unpadded + 4 = 28 -> padded to 32 bytes

        entry.Release();
    }
}
