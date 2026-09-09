using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;

namespace Ghost.AssetForge.Test;

[TestClass]
[DoNotParallelize]
public class AssetDependencyTests
{
    private string _tempDir = "";
    private BakerRegistry _bakerRegistry = null!;
    private ProjectService _projectService = null!;
    private BakeService _bakeService = null!;

    [TestInitialize]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GhostAssetDepTest_" + Guid.NewGuid().ToString("N"));
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
            catch
            {
            }
        }
    }

    [TestMethod]
    public void StripComments_RemovesLineAndBlockComments_PreservesStringsAndLines()
    {
        var input = @"// Header comment
#include ""real.hlsl"" // trailing comment
/* Multi
   line
   #include ""fake1.hlsl""
*/
#include ""second.hlsl""
string s = ""/* not comment */ // not comment""; // #include ""fake2.hlsl""
";
        var cleaned = ShaderIncludeResolver.StripComments(input);

        StringAssert.Contains(cleaned, "#include \"real.hlsl\"");
        StringAssert.Contains(cleaned, "#include \"second.hlsl\"");
        StringAssert.Contains(cleaned, "string s = \"/* not comment */ // not comment\";");
        Assert.IsFalse(cleaned.Contains("fake1.hlsl", StringComparison.Ordinal));
        Assert.IsFalse(cleaned.Contains("fake2.hlsl", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ResolveDependencies_DiscoversNestedIncludes_ToArbitraryDepth()
    {
        var assetDir = Path.Combine(_tempDir, "Assets");
        var incDir = Path.Combine(assetDir, "Includes");
        Directory.CreateDirectory(incDir);

        var aHlsl = Path.Combine(incDir, "A.hlsl");
        var bHlsl = Path.Combine(incDir, "B.hlsl");
        var cHlsl = Path.Combine(incDir, "C.hlsl");

        // C.hlsl has no dependencies
        File.WriteAllText(cHlsl, "// Leaf file\nfloat GetValue() { return 42.0f; }\n");

        // B.hlsl includes C.hlsl
        File.WriteAllText(bHlsl, "#include \"C.hlsl\"\nfloat GetB() { return GetValue(); }\n");

        // A.hlsl includes B.hlsl
        File.WriteAllText(aHlsl, "#include \"B.hlsl\"\nfloat GetA() { return GetB(); }\n");

        // Shader includes A.hlsl
        var shaderSource = Path.Combine(assetDir, "test.gshdr");
        var shaderCode = @"
shader ""Test/Depth""
{
    pass ""Forward""
    {
        includes
        {
            ""/Includes/A.hlsl"";
        }
        hlsl
        {
            float Test() { return GetA(); }
        }
    }
}";
        File.WriteAllText(shaderSource, shaderCode);

        var assetDirs = new[] { assetDir };
        var deps = ShaderIncludeResolver.ResolveDependencies(shaderSource, shaderCode, assetDirs);

        var normalizedA = Path.GetFullPath(aHlsl).Replace('\\', '/');
        var normalizedB = Path.GetFullPath(bHlsl).Replace('\\', '/');
        var normalizedC = Path.GetFullPath(cHlsl).Replace('\\', '/');

        Assert.Contains(normalizedA, deps);
        Assert.Contains(normalizedB, deps);
        Assert.Contains(normalizedC, deps);
    }

    [TestMethod]
    public void ResolveDependencies_HandlesCycles_Gracefully()
    {
        var assetDir = Path.Combine(_tempDir, "Assets");
        Directory.CreateDirectory(assetDir);

        var aHlsl = Path.Combine(assetDir, "A.hlsl");
        var bHlsl = Path.Combine(assetDir, "B.hlsl");

        // A includes B, B includes A
        File.WriteAllText(aHlsl, "#ifndef A_H\n#define A_H\n#include \"B.hlsl\"\n#endif\n");
        File.WriteAllText(bHlsl, "#ifndef B_H\n#define B_H\n#include \"A.hlsl\"\n#endif\n");

        var shaderSource = Path.Combine(assetDir, "test.gshdr");
        var shaderCode = @"
shader ""Test/Cycle""
{
    pass ""Forward""
    {
        includes
        {
            ""A.hlsl"";
        }
        hlsl
        {
            void Main() {}
        }
    }
}";
        File.WriteAllText(shaderSource, shaderCode);

        var assetDirs = new[] { assetDir };
        var deps = ShaderIncludeResolver.ResolveDependencies(shaderSource, shaderCode, assetDirs);

        var normalizedA = Path.GetFullPath(aHlsl).Replace('\\', '/');
        var normalizedB = Path.GetFullPath(bHlsl).Replace('\\', '/');

        Assert.Contains(normalizedA, deps);
        Assert.Contains(normalizedB, deps);
        Assert.HasCount(2, deps);
    }

    [TestMethod]
    public async Task BakeService_Incremental_RebakesWhenIncludedHlslIsModified()
    {
        var projectDir = Path.Combine(_tempDir, "ShaderDepProject");
        _projectService.CreateProject(projectDir, "ShaderDepProject");
        _bakeService = new BakeService(_projectService.GetContext(), _bakerRegistry);

        var assetDir = _projectService.AssetDirectories[0];
        var cacheDir = _projectService.CacheDirectory;

        // Clear default dummy files
        foreach (var dir in Directory.GetDirectories(assetDir))
        {
            Directory.Delete(dir, true);
        }

        var shadersDir = Path.Combine(assetDir, "Shaders");
        Directory.CreateDirectory(shadersDir);

        var headerHlsl = Path.Combine(shadersDir, "Common.hlsl");
        await File.WriteAllTextAsync(headerHlsl, "float GetColorMultiplier() { return 1.0; }\n");

        var testShader = Path.Combine(shadersDir, "test.gshdr");
        var shaderCode = @"shader ""Custom/TestShader""
{
    pass ""Forward""
    {
        pipeline
        {
            ztest = less_equal;
            zwrite = on;
            cull = back;
            blend = opaque;
            color_mask = all;
        }

        hlsl
        {
            #include ""Common.hlsl""

            struct PixelInput
            {
                float4 position : SV_POSITION;
            };

            [numthreads(1, 1, 1)]
            [outputtopology(""triangle"")]
            void MSMain(out vertices PixelInput outVerts[3], out indices uint3 outTris[1])
            {
                SetMeshOutputCounts(3, 1);
                outVerts[0].position = float4(0, 0, 0, 1);
                outVerts[1].position = float4(1, 0, 0, 1);
                outVerts[2].position = float4(0, 1, 0, 1);
                outTris[0] = uint3(0, 1, 2);
            }

            float4 PSMain(PixelInput input) : SV_TARGET
            {
                return float4(GetColorMultiplier(), 0, 0, 1);
            }
        }

        ms ""hlsl_block"" : ""MSMain"";
        ps ""hlsl_block"" : ""PSMain"";
    }
}";
        await File.WriteAllTextAsync(testShader, shaderCode);

        var meta = new AssetMetadata
        {
            Type = AssetType.Shader,
            Settings = new ShaderBakeSettings
            {
                OptimizeLevel = CompilerOptimizeLevel.O1
            }
        };
        _projectService.SaveMetadata(testShader + ".meta", meta);

        // 1. Initial Bake
        var firstResult = await _bakeService.BakeProjectAsync();
        Assert.AreEqual(0, firstResult.Failed, "First bake should succeed.");
        Assert.AreEqual(1, firstResult.Succeeded, "1 shader should be baked.");

        var cachedFile = Path.Combine(cacheDir, "Shaders", "test");
        var depsFile = cachedFile + ".deps";

        Assert.IsTrue(File.Exists(cachedFile), "Cache file must exist after bake.");
        Assert.IsTrue(File.Exists(depsFile), "Deps file must exist after bake.");

        var depsLines = await File.ReadAllLinesAsync(depsFile);
        Assert.IsNotEmpty(depsLines, "Deps file should record at least Common.hlsl.");
        var normalizedHeader = Path.GetFullPath(headerHlsl).Replace('\\', '/');
        Assert.IsTrue(depsLines.Any(l => string.Equals(l.Trim(), normalizedHeader, StringComparison.OrdinalIgnoreCase)),
            "Deps file must contain normalized path to Common.hlsl.");

        var firstWriteTime = File.GetLastWriteTimeUtc(cachedFile);

        // 2. Second Bake without changes -> should be skipped!
        var secondResult = await _bakeService.BakeProjectAsync();
        Assert.AreEqual(0, secondResult.Succeeded, "No assets should be baked when unchanged.");
        Assert.AreEqual(2, secondResult.Skipped, "Both header and shader should be skipped when unchanged.");
        var secondWriteTime = File.GetLastWriteTimeUtc(cachedFile);
        Assert.AreEqual(firstWriteTime, secondWriteTime, "Cache timestamp should not change on skip.");

        // 3. Modify Common.hlsl only (test.gshdr NOT touched)
        await Task.Delay(100); // Ensure timestamp resolution difference
        await File.WriteAllTextAsync(headerHlsl, "float GetColorMultiplier() { return 2.0; }\n");

        var thirdResult = await _bakeService.BakeProjectAsync();
        Assert.AreEqual(1, thirdResult.Succeeded, "Shader should be rebaked when included header changes.");
        var thirdWriteTime = File.GetLastWriteTimeUtc(cachedFile);
        Assert.AreNotEqual(firstWriteTime, thirdWriteTime, "Cache file timestamp must update when header is modified.");
    }

    [TestMethod]
    public async Task BakeService_UniversalDependencies_RebakesWhenCustomDependencyChanges()
    {
        var projectDir = Path.Combine(_tempDir, "MultiAssetProject");
        _projectService.CreateProject(projectDir, "MultiAssetProject");

        _bakeService = new BakeService(_projectService.GetContext(), _bakerRegistry);

        var assetDir = _projectService.AssetDirectories[0];
        var cacheDir = _projectService.CacheDirectory;

        foreach (var dir in Directory.GetDirectories(assetDir))
        {
            Directory.Delete(dir, true);
        }

        var customDir = Path.Combine(assetDir, "Custom");
        Directory.CreateDirectory(customDir);

        var externalDir = Path.Combine(_tempDir, "ExternalData");
        Directory.CreateDirectory(externalDir);

        var companionFile = Path.Combine(externalDir, "data.txt");
        await File.WriteAllTextAsync(companionFile, "version: 1\n");

        var sourceAsset = Path.Combine(customDir, "asset.custom");
        await File.WriteAllTextAsync(sourceAsset, $"companion: {companionFile.Replace('\\', '/')}\n");

        var meta = new AssetMetadata
        {
            Type = AssetType.Unknown,
            Settings = new MockCustomSettings()
        };
        _projectService.SaveMetadata(sourceAsset + ".meta", meta);

        // 1. Initial bake
        var res1 = await _bakeService.BakeProjectAsync();
        Assert.AreEqual(1, res1.Succeeded);

        var cachedFile = Path.Combine(cacheDir, "Custom", "asset");
        var depsFile = cachedFile + ".deps";
        Assert.IsTrue(File.Exists(cachedFile));
        Assert.IsTrue(File.Exists(depsFile));

        var time1 = File.GetLastWriteTimeUtc(cachedFile);

        // 2. Second bake -> skipped
        var res2 = await _bakeService.BakeProjectAsync();
        Assert.AreEqual(1, res2.Skipped);
        Assert.AreEqual(time1, File.GetLastWriteTimeUtc(cachedFile));

        // 3. Modify companion data file only
        await Task.Delay(100);
        await File.WriteAllTextAsync(companionFile, "version: 2\n");

        var res3 = await _bakeService.BakeProjectAsync();
        Assert.AreEqual(1, res3.Succeeded, "Custom asset should rebake when its companion dependency changes.");
        Assert.AreNotEqual(time1, File.GetLastWriteTimeUtc(cachedFile));
    }
}

public class MockCustomSettings : IBakeSettings;

[AssetBaker(Extensions = new[] { ".custom" }, Type = AssetType.Unknown, SettingsType = typeof(MockCustomSettings))]
public class MockCustomBaker : IAssetBaker, IAssetDependencyScanner
{
    public IEnumerable<string> ScanDependencies(string sourceFile, IBakeSettings settings, AssetBakerContext ctx)
    {
        if (!File.Exists(sourceFile))
        {
            return Array.Empty<string>();
        }

        var lines = File.ReadAllLines(sourceFile);
        foreach (var line in lines)
        {
            if (line.StartsWith("companion: "))
            {
                var target = line.Substring("companion: ".Length).Trim();
                if (File.Exists(target))
                {
                    return new[] { target };
                }
            }
        }
        return Array.Empty<string>();
    }

    public Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        var deps = ScanDependencies(src, settings, ctx);
        foreach (var dep in deps)
        {
            ctx.AddDependency(dep);
        }

        dst.Write(new byte[] { 1, 2, 3, 4 });
        return Task.CompletedTask;
    }
}
