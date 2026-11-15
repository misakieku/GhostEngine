using Ghost.AssetForge.Core.Services;

namespace Ghost.AssetForge.Test;

[TestClass]
public class ProjectServiceTests
{
    private string _tempDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GhostEngineTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Teardown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [TestMethod]
    public void InitializeFromArgs_ValidArgs_CreatesDirectoriesAndSetsProperties()
    {
        var registry = new BakerRegistry();
        var service = new ProjectService(registry);

        var assetDir = Path.Combine(_tempDir, "Asset");
        var cacheDir = Path.Combine(_tempDir, "Cache");
        var buildDir = Path.Combine(_tempDir, "Build");

        service.InitializeFromArgs(new[] { assetDir }, cacheDir, buildDir);

        Assert.AreEqual(assetDir, service.AssetDirectories[0]);
        Assert.HasCount(1, service.AssetDirectories);
        Assert.AreEqual(assetDir, service.AssetDirectories[0]);
        Assert.AreEqual(cacheDir, service.CacheDirectory);
        Assert.AreEqual(buildDir, service.BuildDirectory);

        Assert.IsTrue(Directory.Exists(assetDir));
        Assert.IsTrue(Directory.Exists(cacheDir));
        Assert.IsTrue(Directory.Exists(buildDir));
        Assert.IsNotNull(service.CurrentProject);
        Assert.AreEqual("CLI_Project", service.CurrentProject.Name);
    }

    [TestMethod]
    public void OpenProject_WithDirectory_SetsDefaultPaths()
    {
        var registry = new BakerRegistry();
        var service = new ProjectService(registry);

        // Create dummy csproj
        var csprojPath = Path.Combine(_tempDir, "MyTestProject.csproj");
        File.WriteAllText(csprojPath, "<Project></Project>");

        service.OpenProject(_tempDir);

        Assert.IsNotNull(service.CurrentProject);
        Assert.AreEqual("MyTestProject", service.CurrentProject.Name);
        Assert.AreEqual(Path.Combine(_tempDir, "Asset"), service.AssetDirectories[0]);
        Assert.AreEqual(Path.Combine(_tempDir, "obj", "AssetCache"), service.CacheDirectory);
        Assert.AreEqual(Path.Combine(_tempDir, "bin", "Assets"), service.BuildDirectory);
    }

    [TestMethod]
    public void OpenProject_WithCustomProperties_ParsesXml()
    {
        var registry = new BakerRegistry();
        var service = new ProjectService(registry);

        var csprojPath = Path.Combine(_tempDir, "CustomProject.csproj");
        var xml = @"<Project>
            <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
                <GhostAssetDir>CustomAssetDir</GhostAssetDir>
                <GhostAssetCacheDir>$(IntermediateOutputPath)MyCache</GhostAssetCacheDir>
                <GhostAssetBuildDir>$(TargetDir)MyAssets</GhostAssetBuildDir>
            </PropertyGroup>
        </Project>";
        File.WriteAllText(csprojPath, xml);

        service.OpenProject(_tempDir);

        Assert.AreEqual("CustomProject", service.CurrentProject!.Name);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(_tempDir, "CustomAssetDir")), service.AssetDirectories[0]);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(_tempDir, @"obj\Debug\net9.0\MyCache")), service.CacheDirectory);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(_tempDir, @"bin\Debug\net9.0\MyAssets")), service.BuildDirectory);
    }

    [TestMethod]
    public void CreateProject_ScaffoldsGhostProjAndBuildableCsproj()
    {
        var registry = new BakerRegistry();
        var service = new ProjectService(registry);
        var projectDir = Path.Combine(_tempDir, "ScaffoldedGame");

        service.CreateProject(projectDir, "ScaffoldedGame");

        var ghostProjPath = Path.Combine(projectDir, "ScaffoldedGame.ghostproj");
        var csprojPath = Path.Combine(projectDir, "ScaffoldedGame.csproj");
        var programPath = Path.Combine(projectDir, "Program.cs");
        var launchProfilePath = Path.Combine(projectDir, "LaunchProfile.cs");
        var assetsDir = Path.Combine(projectDir, "Assets");

        Assert.IsTrue(File.Exists(ghostProjPath));
        Assert.IsTrue(File.Exists(csprojPath));
        Assert.IsTrue(File.Exists(programPath));
        Assert.IsTrue(File.Exists(launchProfilePath));
        Assert.IsTrue(Directory.Exists(assetsDir));
        Assert.AreEqual(assetsDir, service.AssetDirectories[0]);

        var reopened = new ProjectService(registry);
        reopened.OpenProject(ghostProjPath);

        Assert.IsNotNull(reopened.CurrentProject);
        Assert.AreEqual("ScaffoldedGame", reopened.CurrentProject.Name);
        Assert.AreEqual("ScaffoldedGame.csproj", reopened.CurrentProject.CsprojPath);
        Assert.AreEqual(assetsDir, reopened.AssetDirectories[0]);
    }

    [TestMethod]
    public async Task ImportAssetFile_WithAndWithoutBaker_HandlesBothAndBakeAssetsSkipsUnsupported()
    {
        using var registry = new BakerRegistry();
        var service = new ProjectService(registry);
        var projectDir = Path.Combine(_tempDir, "ImportProject");
        service.CreateProject(projectDir, "ImportProject");

        var sourceDir = Path.Combine(_tempDir, "ExternalDrop");
        Directory.CreateDirectory(sourceDir);

        var textFile = Path.Combine(sourceDir, "notes.txt");
        File.WriteAllText(textFile, "design notes");

        var importedText = service.ImportAssetFile(textFile, "Docs");
        Assert.IsNotNull(importedText);
        Assert.IsFalse(importedText.Value.HasBaker);
        Assert.IsTrue(File.Exists(importedText.Value.DestinationPath));
        Assert.IsFalse(File.Exists(importedText.Value.DestinationPath + ".meta"));

        Assert.IsTrue(registry.HasBaker(".PNG"));
        Assert.IsFalse(registry.HasBaker(".txt"));

        var bakeService = new BakeService(service.GetContext(), registry);
        var result = await bakeService.BakeAssetsAsync(new[] { (importedText.Value.RelativePath, importedText.Value.DestinationPath) }, new BakeConfig());
        Assert.AreEqual(1, result.Total);
        Assert.AreEqual(0, result.Failed);
        Assert.AreEqual(1, result.Skipped);
    }

    [TestMethod]
    public void GetOrCreateMetadata_CreatesAndRoundTripsTextureSettingsClone()
    {
        using var registry = new BakerRegistry();
        var service = new ProjectService(registry);
        var projectDir = Path.Combine(_tempDir, "MetaCloneProject");
        service.CreateProject(projectDir, "MetaCloneProject");

        var dummyPng = Path.Combine(service.AssetDirectories[0], "albedo.png");
        File.WriteAllBytes(dummyPng, new byte[] { 1, 2, 3 });

        var created = service.GetOrCreateMetadata(dummyPng);
        Assert.IsNotNull(created);
        Assert.IsInstanceOfType<Ghost.AssetForge.Core.Bakers.TextureBakeSettings>(created.Settings);

        var json = service.SerializeMetadata(created);
        var cloned = service.DeserializeMetadata(json);
        Assert.IsNotNull(cloned);
        Assert.IsInstanceOfType<Ghost.AssetForge.Core.Bakers.TextureBakeSettings>(cloned.Settings);

        var origSettings = (Ghost.AssetForge.Core.Bakers.TextureBakeSettings)created.Settings;
        var cloneSettings = (Ghost.AssetForge.Core.Bakers.TextureBakeSettings)cloned.Settings;
        cloneSettings.Basic.IsSRGB = false;

        Assert.IsTrue(origSettings.Basic.IsSRGB);
        Assert.IsFalse(cloneSettings.Basic.IsSRGB);
    }
}
