using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using System.Text;

namespace Ghost.AssetForge.Test;

[TestClass]
[DoNotParallelize]
public sealed class ShaderCatalogPackingTests
{
    [TestMethod]
    public async Task PackProjectEmitsShaderCatalogFromBakedPayload()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"GhostShaderCatalogTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var registry = new BakerRegistry();
            var projectService = new ProjectService(registry);
            var projectDir = Path.Combine(tempDir, "Project");
            projectService.CreateProject(projectDir, "Project");

            var assetDirectory = projectService.AssetDirectories[0];
            foreach (var directory in Directory.GetDirectories(assetDirectory))
            {
                Directory.Delete(directory, true);
            }

            var sourceDirectory = Path.Combine(assetDirectory, "Shaders");
            Directory.CreateDirectory(sourceDirectory);
            var sourcePath = Path.Combine(sourceDirectory, "standard-lit.gshdr");
            await File.WriteAllTextAsync(sourcePath, "shader StandardLit : Lit {}");

            var assetId = Guid.NewGuid();
            projectService.SaveMetadata(sourcePath + ".meta", new AssetMetadata
            {
                Id = assetId,
                Type = AssetType.Shader,
                Settings = new ShaderBakeSettings(),
            });

            var cachePath = Path.Combine(projectService.CacheDirectory, "Shaders", "standard-lit");
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            WriteShaderCache(cachePath);

            var packService = new PackService(projectService.GetContext(), registry);
            await packService.PackProjectAsync();

            var manifest = await Manifest.LoadFromDiskAsync(Path.Combine(projectService.BuildDirectory, "manifest.json"));
            Assert.HasCount(1, manifest.Shaders);
            var shader = manifest.Shaders[0];
            Assert.AreEqual(assetId, shader.AssetId);
            Assert.AreEqual("StandardLit", shader.Name);
            Assert.AreEqual(ShaderIdentity.GetShaderId("Lit"), shader.FamilyId);
            Assert.AreEqual(64u, shader.PropertyBufferSize);
            Assert.HasCount(1, shader.Passes);
            Assert.AreEqual(PassSemantic.DeferredTexturing, shader.Passes[0].Semantic);
            Assert.AreEqual(ShaderStageMask.Compute, shader.Passes[0].StageMask);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static void WriteShaderCache(string path)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        new CacheFileHeader().WriteTo(stream);
        var assetStart = stream.Position;
        var shaderId = ShaderIdentity.GetShaderId("StandardLit");
        var header = new ShaderContentHeader
        {
            shaderType = ShaderType.Graphics,
            passCount = 1,
            propertyBufferSize = 64,
            shaderModel = ShaderModel.SM_6_8,
            shaderId = shaderId,
            familyId = ShaderIdentity.GetShaderId("Lit"),
            layoutHash = 0x1122334455667788ul,
        };

        stream.Write(header);
        WriteName(stream, assetStart, "StandardLit", ref header.nameOffset, ref header.nameSize);

        var passHeaderOffset = stream.Position;
        var pass = new ShaderContentHeader.PassHeader
        {
            entryPointCount = 1,
            semantic = PassSemantic.DeferredTexturing,
            stageMask = ShaderStageMask.Compute,
            passId = ShaderIdentity.GetPassId(shaderId, 0),
            localPipeline = PipelineState.Default,
        };
        stream.Write(pass);
        WriteName(stream, assetStart, "DeferredTexturing", ref pass.nameOffset, ref pass.nameSize);
        pass.dataOffset = stream.Position - assetStart;
        pass.dataSize = 0;

        var end = stream.Position;
        stream.Position = passHeaderOffset;
        stream.Write(pass);
        stream.Position = assetStart;
        stream.Write(header);
        stream.Position = end;
    }

    private static void WriteName(Stream stream, long assetStart, string value, ref long offset, ref uint size)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        offset = stream.Position - assetStart;
        size = (uint)bytes.Length;
        stream.Write(bytes);
    }
}
