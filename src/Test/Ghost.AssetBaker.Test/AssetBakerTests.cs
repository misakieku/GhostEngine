using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using System.Text.Json;

namespace Ghost.AssetForge.Test;

[TestClass]
[DoNotParallelize]
public class AssetBakerTests
{
    private string _tempDir = "";
    private BakerRegistry _bakerRegistry = null!;
    private ProjectService _projectService = null!;
    private BakeService _bakeService = null!;

    [TestInitialize]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GhostAssetBakerTest_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _bakerRegistry = new BakerRegistry();

        _projectService = new ProjectService(_bakerRegistry);
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
    public void TestMetadataPolymorphicSerialization()
    {
        var originalMetadata = new AssetMetadata
        {
            Id = Guid.NewGuid(),
            Type = AssetType.Texture,
            Settings = new TextureBakeSettings
            {
                Basic = new TextureBakeSettings.BasicSettings
                {
                    TextureType = TextureType.Default,
                    TextureShape = TextureShape.Texture2D,
                    IsSRGB = true
                },
                Advanced = new TextureBakeSettings.AdvancedSettings
                {
                    GenerateMipmaps = true,
                    MipmapFilter = MipmapFilter.Kaiser,
                    CompressionLevel = TextureCompressionLevel.High
                }
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        // Serialize
        var json = JsonSerializer.Serialize(originalMetadata, options);
        StringAssert.Contains(json, "\"Type\": \"Texture\"");
        StringAssert.Contains(json, "\"GenerateMipmaps\": true");

        // Deserialize
        var deserialized = JsonSerializer.Deserialize<AssetMetadata>(json, options);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(originalMetadata.Id, deserialized.Id);
        Assert.AreEqual(originalMetadata.Type, deserialized.Type);
        Assert.IsNotNull(deserialized.Settings);
        Assert.IsInstanceOfType(deserialized.Settings, typeof(TextureBakeSettings));

        var texSettings = (TextureBakeSettings)deserialized.Settings;
        Assert.AreEqual(TextureType.Default, texSettings.Basic.TextureType);
        Assert.AreEqual(TextureShape.Texture2D, texSettings.Basic.TextureShape);
        Assert.IsTrue(texSettings.Basic.IsSRGB);
        Assert.IsTrue(texSettings.Advanced.GenerateMipmaps);
        Assert.AreEqual(MipmapFilter.Kaiser, texSettings.Advanced.MipmapFilter);
        Assert.AreEqual(TextureCompressionLevel.High, texSettings.Advanced.CompressionLevel);
    }

    [TestMethod]
    public async Task TestDuplicateAssetNameReturnsFailedResult()
    {
        // Set up project
        var projectDir = Path.Combine(_tempDir, "MyProject");
        _projectService.CreateProject(projectDir, "MyProject");
        _bakeService = new BakeService(_projectService.GetContext(), _bakerRegistry);

        var assetDir = Path.Combine(projectDir, "Asset");
        var texDir = Path.Combine(assetDir, "Textures");
        Directory.CreateDirectory(texDir);

        // Create duplicate names (same key: Textures/skybox) with different extensions
        File.WriteAllText(Path.Combine(texDir, "skybox.png"), "dummy");
        File.WriteAllText(Path.Combine(texDir, "skybox.tga"), "dummy");

        // Write default metadata files
        var meta1 = new AssetMetadata { Type = AssetType.Texture, Settings = new TextureBakeSettings() };
        _projectService.SaveMetadata(Path.Combine(texDir, "skybox.png.meta"), meta1);
        _projectService.SaveMetadata(Path.Combine(texDir, "skybox.tga.meta"), meta1);

        // Run bake pipeline: duplicates are detected in a pre-scan, nothing is baked,
        // and the result reports every duplicated asset as failed (no exception thrown).
        var result = await _bakeService.BakeProjectAsync();

        Assert.AreEqual(2, result.Total);
        Assert.AreEqual(2, result.Failed);
        Assert.AreEqual(0, result.Succeeded);
        Assert.AreEqual(0, result.Skipped);
        Assert.HasCount(2, result.FailedAssets);
        Assert.Contains("Textures/skybox", result.FailedAssets[0]);
    }

    [TestMethod]
    public async Task TestBakeIncrementalLogic()
    {
        var projectDir = Path.Combine(_tempDir, "IncrementalProject");
        _projectService.CreateProject(projectDir, "IncrementalProject");
        _bakeService = new BakeService(_projectService.GetContext(), _bakerRegistry);

        var assetDir = _projectService.AssetDirectories[0];
        var cacheDir = _projectService.CacheDirectory;

        // Clear pre-created dummy assets to only test our file
        foreach (var dir in Directory.GetDirectories(assetDir))
        {
            Directory.Delete(dir, true);
        }

        var texDir = Path.Combine(assetDir, "Textures");
        Directory.CreateDirectory(texDir);

        var pngPath = Path.Combine(texDir, "test.png");
        // TextureBaker reads image info. For StbI/GetImageInfo not to fail, we write a valid 1x1 pixel PNG file:
        byte[] minimalPng = {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
            0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
            0x00, 0x03, 0x01, 0x01, 0x00, 0x18, 0xDD, 0x8D, 0xB0, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
            0x44, 0xAE, 0x42, 0x60, 0x82
        };
        await File.WriteAllBytesAsync(pngPath, minimalPng);

        var meta = new AssetMetadata
        {
            Type = AssetType.Texture,
            Settings = new TextureBakeSettings
            {
                Basic = new TextureBakeSettings.BasicSettings
                {
                    TextureType = TextureType.Default,
                    TextureShape = TextureShape.Texture2D,
                    IsSRGB = true
                },
                Advanced = new TextureBakeSettings.AdvancedSettings
                {
                    GenerateMipmaps = false,
                    CompressionLevel = TextureCompressionLevel.Low
                }
            }
        };
        _projectService.SaveMetadata(pngPath + ".meta", meta);

        // Run first bake
        await _bakeService.BakeProjectAsync();

        var cachedFile = Path.Combine(cacheDir, "Textures", "test");
        Assert.IsTrue(File.Exists(cachedFile), "Cache file should exist after bake.");
        var firstWriteTime = File.GetLastWriteTimeUtc(cachedFile);

        // Run second bake immediately without modification
        await _bakeService.BakeProjectAsync();
        var secondWriteTime = File.GetLastWriteTimeUtc(cachedFile);
        Assert.AreEqual(firstWriteTime, secondWriteTime, "Bake should be skipped and cache time unchanged.");

        // Modify source file slightly and bake again
        await Task.Delay(100); // Ensure timestamp difference
        await File.WriteAllBytesAsync(pngPath, minimalPng); // rewrite source
        await _bakeService.BakeProjectAsync();
        var thirdWriteTime = File.GetLastWriteTimeUtc(cachedFile);
        Assert.AreNotEqual(firstWriteTime, thirdWriteTime, "Bake should execute again when source file is modified.");
    }
}
