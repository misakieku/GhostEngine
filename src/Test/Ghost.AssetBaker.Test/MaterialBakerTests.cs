using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.AssetForge.Test;

[TestClass]
[DoNotParallelize]
public class MaterialBakerTests
{
    private string _tempDir = "";

    [TestInitialize]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GhostMaterialBakerTest_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void TearDown()
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
    public async Task TestMaterialBaker_BasicBake()
    {
        var gmatPath = Path.Combine(_tempDir, "TestMat.gmat");
        var baseColorGuid = Guid.NewGuid();
        var normalGuid = Guid.NewGuid();

        var json = $$"""
        {
            "Version": 1,
            "Shader": "dea05727-fa9e-41ac-b039-f2c0411a0e04",
            "Properties": {
                "baseColor": [0.1, 0.2, 0.3],
                "roughness": 0.4,
                "metallic": 0.8
            },
            "Textures": {
                "baseColorMap": "{{baseColorGuid}}",
                "normalMap": "{{normalGuid}}"
            }
        }
        """;
        await File.WriteAllTextAsync(gmatPath, json);

        var baker = new MaterialBaker();
        using var outMs = new MemoryStream();
        var ctx = new AssetBakerContext
        {
            AssetDirectories = [_tempDir],
            BakeConfig = new BakeConfig()
        };

        await baker.BakeAssetAsync(gmatPath, outMs, new MaterialBakeSettings(), ctx, CancellationToken.None);

        outMs.Position = 0;
        var headerBytes = new byte[Marshal.SizeOf<MaterialContentHeader>()];
        outMs.ReadExactly(headerBytes);
        var header = MemoryMarshal.Read<MaterialContentHeader>(headerBytes);

        Assert.AreEqual(MaterialContentHeader.MAGIC, header.magic);
        Assert.AreEqual(MaterialContentHeader.VERSION, header.version);
        Assert.AreEqual(MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID, header.shaderId);
        Assert.AreEqual(80u, header.propertyBufferSize);
        Assert.AreEqual(2u, header.textureFixupCount);

        var propertyBytes = new byte[header.propertyBufferSize];
        outMs.ReadExactly(propertyBytes);

        unsafe
        {
            fixed (byte* p = propertyBytes)
            {
                var bc = *(float3*)(p + 28); // baseColor offset in Lit template
                Assert.AreEqual(0.1f, bc.x, 1e-4f);
                Assert.AreEqual(0.2f, bc.y, 1e-4f);
                Assert.AreEqual(0.3f, bc.z, 1e-4f);

                var roughness = *(float*)(p + 56); // roughness offset
                Assert.AreEqual(0.4f, roughness, 1e-4f);

                var metallic = *(float*)(p + 60); // metallic offset
                Assert.AreEqual(0.8f, metallic, 1e-4f);
            }
        }

        var fixupBytes = new byte[header.textureFixupCount * Marshal.SizeOf<MaterialTextureFixup>()];
        outMs.ReadExactly(fixupBytes);
        var fixups = MemoryMarshal.Cast<byte, MaterialTextureFixup>(fixupBytes);

        Assert.AreEqual(40u, fixups[0].byteOffset); // baseColorMap offset
        Assert.AreEqual(baseColorGuid, fixups[0].textureGuid);
        Assert.AreEqual(44u, fixups[1].byteOffset); // normalMap offset
        Assert.AreEqual(normalGuid, fixups[1].textureGuid);
    }

    [TestMethod]
    public async Task TestMaterialBaker_DefaultValues()
    {
        var gmatPath = Path.Combine(_tempDir, "DefaultMat.gmat");
        var json = """
        {
            "Version": 1,
            "Shader": "00000000-0000-0000-0000-000000000000"
        }
        """;
        await File.WriteAllTextAsync(gmatPath, json);

        var baker = new MaterialBaker();
        using var outMs = new MemoryStream();
        var ctx = new AssetBakerContext
        {
            AssetDirectories = [_tempDir],
            BakeConfig = new BakeConfig()
        };

        await baker.BakeAssetAsync(gmatPath, outMs, new MaterialBakeSettings(), ctx, CancellationToken.None);

        outMs.Position = 0;
        var headerBytes = new byte[Marshal.SizeOf<MaterialContentHeader>()];
        outMs.ReadExactly(headerBytes);
        var header = MemoryMarshal.Read<MaterialContentHeader>(headerBytes);

        Assert.AreEqual(MaterialContentHeader.MAGIC, header.magic);
        Assert.AreEqual(MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID, header.shaderId);
        Assert.AreEqual(80u, header.propertyBufferSize);
        Assert.AreEqual(0u, header.textureFixupCount);

        var propertyBytes = new byte[header.propertyBufferSize];
        outMs.ReadExactly(propertyBytes);

        unsafe
        {
            fixed (byte* p = propertyBytes)
            {
                var bc = *(float3*)(p + 28);
                Assert.AreEqual(0.73f, bc.x, 1e-4f);
                Assert.AreEqual(0.73f, bc.y, 1e-4f);
                Assert.AreEqual(0.73f, bc.z, 1e-4f);

                var roughness = *(float*)(p + 56);
                Assert.AreEqual(0.5f, roughness, 1e-4f);

                var metallic = *(float*)(p + 60);
                Assert.AreEqual(0.0f, metallic, 1e-4f);
            }
        }
    }

    [TestMethod]
    public void TestMaterialPropertyPacker_CustomNonTemplateShader()
    {
        var shaderCode = """
        shader "Custom/TestPBR"
        {
            properties
            {
                float4 tint = float4(1.0, 0.5, 0.25, 1.0);
                float gloss = 0.8;
                Texture2D albedoMap;
            }

            pass "Main"
            {
                ms "hlsl_block" : "MSMain";
                ps "hlsl_block" : "PSMain";
            }
        }
        """;

        var syntax = Ghost.DSL.ShaderCompiler.DSLShaderCompiler.ParseGraphicsShaderSyntax(shaderCode).GetValueOrThrow();
        var semantics = Ghost.DSL.ShaderCompiler.DSLShaderCompiler.GetShaderSemantics(syntax).GetValueOrThrow();

        var albedoGuid = Guid.NewGuid();
        var textures = new Dictionary<string, Guid> { ["albedoMap"] = albedoGuid };

        var (buf, fixups) = MaterialPropertyPacker.Pack(semantics, jsonProperties: null, jsonTextures: textures);

        Assert.IsNotNull(buf);
        Assert.HasCount(1, fixups);
        Assert.AreEqual(albedoGuid, fixups[0].textureGuid);

        unsafe
        {
            fixed (byte* p = buf)
            {
                var tint = *(float4*)p;
                Assert.AreEqual(1.0f, tint.x);
                Assert.AreEqual(0.5f, tint.y);
                Assert.AreEqual(0.25f, tint.z);
                Assert.AreEqual(1.0f, tint.w);

                var gloss = *(float*)(p + 16);
                Assert.AreEqual(0.8f, gloss, 1e-4f);
            }
        }
    }
}
