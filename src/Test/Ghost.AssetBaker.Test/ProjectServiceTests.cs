using Ghost.AssetForge.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

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
        var shaderMetadataPath = Path.Combine(_tempDir, "shader_properties.json");

        service.InitializeFromArgs(new[] { assetDir }, cacheDir, buildDir, new[] { shaderMetadataPath });

        Assert.AreEqual(assetDir, service.AssetDirectories[0]);
        Assert.AreEqual(1, service.AssetDirectories.Count);
        Assert.AreEqual(assetDir, service.AssetDirectories[0]);
        Assert.AreEqual(cacheDir, service.CacheDirectory);
        Assert.AreEqual(buildDir, service.BuildDirectory);
        Assert.AreEqual(1, service.ShaderMetadataPaths.Count);
        Assert.AreEqual(shaderMetadataPath, service.ShaderMetadataPaths[0]);

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
        string csprojPath = Path.Combine(_tempDir, "MyTestProject.csproj");
        File.WriteAllText(csprojPath, "<Project></Project>");

        service.OpenProject(_tempDir);

        Assert.IsNotNull(service.CurrentProject);
        Assert.AreEqual("MyTestProject", service.CurrentProject.Name);
        Assert.AreEqual(Path.Combine(_tempDir, "Asset"), service.AssetDirectories[0]);
        Assert.AreEqual(Path.Combine(_tempDir, "obj", "AssetCache"), service.CacheDirectory);
        Assert.AreEqual(Path.Combine(_tempDir, "bin", "Assets"), service.BuildDirectory);
        Assert.AreEqual(1, service.ShaderMetadataPaths.Count);
        Assert.AreEqual(Path.Combine(_tempDir, "obj", "shader_properties.json"), service.ShaderMetadataPaths[0]);
    }

    [TestMethod]
    public void OpenProject_WithCustomProperties_ParsesXml()
    {
        var registry = new BakerRegistry();
        var service = new ProjectService(registry);

        string csprojPath = Path.Combine(_tempDir, "CustomProject.csproj");
        string xml = @"<Project>
            <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
                <GhostAssetDir>CustomAssetDir</GhostAssetDir>
                <GhostAssetCacheDir>$(IntermediateOutputPath)MyCache</GhostAssetCacheDir>
                <GhostAssetBuildDir>$(TargetDir)MyAssets</GhostAssetBuildDir>
                <GhostShaderMetadataPath>$(MSBuildProjectDirectory)\CustomShader.json</GhostShaderMetadataPath>
            </PropertyGroup>
        </Project>";
        File.WriteAllText(csprojPath, xml);

        service.OpenProject(_tempDir);

        Assert.AreEqual("CustomProject", service.CurrentProject!.Name);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(_tempDir, "CustomAssetDir")), service.AssetDirectories[0]);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(_tempDir, @"obj\Debug\net9.0\MyCache")), service.CacheDirectory);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(_tempDir, @"bin\Debug\net9.0\MyAssets")), service.BuildDirectory);
        Assert.AreEqual(1, service.ShaderMetadataPaths.Count);
        Assert.AreEqual(Path.GetFullPath(Path.Combine(_tempDir, "CustomShader.json")), service.ShaderMetadataPaths[0]);
    }
}
