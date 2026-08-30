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
    public void ExtractMetadata_GeneratesValidJson()
    {
        var csFile = Path.Combine(_tempDir, "TestShader.cs");
        File.WriteAllText(csFile, @"
using System;

namespace TestNamespace
{
    [Ghost.Core.Graphics.GenerateShaderProperty(""MyShader"", ""MyShaderStruct"")]
    public struct TestShaderStruct
    {
        public float value1;
        public int value2;
        
        [Ghost.Engine.Utilities.GenerateAsHLSLType(""float4x4"")]
        public System.Numerics.Matrix4x4 matrix;
    }
}
");

        var inputFileList = Path.Combine(_tempDir, "input_files.txt");
        File.WriteAllLines(inputFileList, new[] { csFile });

        var outputFile = Path.Combine(_tempDir, "output.json");

        // Run the tool's main logic
        Program.Main(new[] { inputFileList, outputFile });

        Assert.IsTrue(File.Exists(outputFile), "Output JSON should be generated.");

        var json = File.ReadAllText(outputFile);

        StringAssert.Contains(json, "MyShader", "JSON should contain the shader name.");
        StringAssert.Contains(json, "MyShaderStruct", "JSON should contain the struct name.");
        StringAssert.Contains(json, "value1", "JSON should contain the field value1.");
        StringAssert.Contains(json, "float4x4", "JSON should contain the mapped HLSL type.");
    }
}
