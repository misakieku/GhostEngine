using Ghost.ShaderMetadataTool;

namespace Ghost.AssetForge.Test;

[TestClass]
public class ShaderMetadataToolTests
{
    private string _tempDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "GhostShaderToolTest_" + Guid.NewGuid());
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
    public void ExtractMetadata_GeneratesHLSLOnDisk()
    {
        var csFile = Path.Combine(_tempDir, "TestShader.cs");
        File.WriteAllText(csFile, @"
using System;
using Ghost.Core.Graphics;

namespace TestNamespace
{
    [GenerateHLSL(PackingRules.Exact, ""Test/MyShader.hlsl"")]
    public struct TestShaderStruct
    {
        public float value1;
        public int value2;
        
        [GenerateAsHLSLType(""float4x4"")]
        public System.Numerics.Matrix4x4 matrix;
    }
}
");

        var inputFileList = Path.Combine(_tempDir, "input_files.txt");
        File.WriteAllLines(inputFileList, new[] { csFile });

        var assetDir = Path.Combine(_tempDir, "Assets");
        Directory.CreateDirectory(assetDir);

        // Run the tool
        Program.Main(new[] { inputFileList, assetDir });

        var generatedHlslFile = Path.Combine(assetDir, "Test", "MyShader.hlsl");
        Assert.IsTrue(File.Exists(generatedHlslFile), "Generated HLSL file should exist on disk.");

        var content = File.ReadAllText(generatedHlslFile);
        StringAssert.Contains(content, "#ifndef TEST_MYSHADER_HLSL", "HLSL should contain header guard.");
        StringAssert.Contains(content, "struct TestShaderStruct", "HLSL should contain struct declaration.");
        StringAssert.Contains(content, "float value1;", "HLSL should contain field value1.");
        StringAssert.Contains(content, "float4x4 matrix;", "HLSL should contain mapped float4x4.");

        // Verify timestamp preservation when content is unchanged
        var writeTime = File.GetLastWriteTimeUtc(generatedHlslFile);
        Thread.Sleep(20);
        Program.Main(new[] { inputFileList, assetDir });
        var writeTimeAfter = File.GetLastWriteTimeUtc(generatedHlslFile);
        Assert.AreEqual(writeTime, writeTimeAfter, "File timestamp should be preserved if content did not change.");
    }
}
