using System.Collections.Generic;
using Ghost.DSL.Parser;
using Ghost.DSL.Properties;
using Ghost.DSL.ShaderParser.Syntax;
using Ghost.DSL.Syntax.Symbols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.AssetForge.Test;

[TestClass]
public class DSLPropertyLayoutTests
{
    [TestMethod]
    public void TestTemplatePropertyLayout_ComputesCorrectOffsetsAndPadding()
    {
        var dsl = @"
template ""LitTemplate""
{
    properties
    {
        Float4 BaseColor;
        Float Roughness;
        Float Metallic;
    }
}
";
        var errors = new List<DSLShaderError>();
        var doc = DSLParser.ParseDocument(dsl, "LitTemplate.gshdr", errors);
        Assert.IsNotNull(doc);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(1, doc.Templates.Count);

        var template = doc.Templates[0];
        var schema = PropertyLayoutEngine.ComputeTemplateLayout(template, "LitTemplate", errors);
        Assert.IsNotNull(schema);
        Assert.AreEqual(0, errors.Count);

        Assert.AreEqual(3, schema.Fields.Count);
        // BaseColor: Float4 -> offset 0, size 16, align 16
        Assert.AreEqual("BaseColor", schema.Fields[0].Name);
        Assert.AreEqual(0u, schema.Fields[0].Offset);
        Assert.AreEqual(16u, schema.Fields[0].Size);

        // Roughness: Float -> offset 16, size 4, align 4
        Assert.AreEqual("Roughness", schema.Fields[1].Name);
        Assert.AreEqual(16u, schema.Fields[1].Offset);
        Assert.AreEqual(4u, schema.Fields[1].Size);

        // Metallic: Float -> offset 20, size 4, align 4
        Assert.AreEqual("Metallic", schema.Fields[2].Name);
        Assert.AreEqual(20u, schema.Fields[2].Offset);
        Assert.AreEqual(4u, schema.Fields[2].Size);

        // Total size padded to 16 bytes: 24 -> 32
        Assert.AreEqual(32u, schema.TotalSize);
        Assert.AreNotEqual(0UL, schema.SchemaId);
    }

    [TestMethod]
    public void TestDerivedShaderPropertyLayout_PrefixStability()
    {
        var templateDsl = @"
template ""LitTemplate""
{
    properties
    {
        Float4 BaseColor;
        Float Roughness;
        Float Metallic;
    }
}
";
        var errors = new List<DSLShaderError>();
        var templateDoc = DSLParser.ParseDocument(templateDsl, "LitTemplate.gshdr", errors);
        var templateSchema = PropertyLayoutEngine.ComputeTemplateLayout(templateDoc!.Templates[0], "LitTemplate", errors);

        var shaderDsl = @"
shader ""Game/CustomLit"" : ""LitTemplate""
{
    properties
    {
        Float DirectSpecularStrength;
        Float3 CustomTint;
    }
}
";
        var shaderDoc = DSLParser.ParseDocument(shaderDsl, "CustomLit.gshdr", errors);
        Assert.IsNotNull(shaderDoc);
        Assert.AreEqual(0, errors.Count);

        var shader = shaderDoc.Shaders[0];
        var shaderSchema = PropertyLayoutEngine.ComputeShaderLayout(shader, "Game/CustomLit", templateSchema, errors);
        Assert.IsNotNull(shaderSchema);
        Assert.AreEqual(0, errors.Count);

        Assert.AreEqual(5, shaderSchema.Fields.Count);

        // Inherited fields maintain exact offsets
        Assert.AreEqual("BaseColor", shaderSchema.Fields[0].Name);
        Assert.AreEqual(0u, shaderSchema.Fields[0].Offset);
        Assert.IsTrue(shaderSchema.Fields[0].IsInherited);

        Assert.AreEqual("Roughness", shaderSchema.Fields[1].Name);
        Assert.AreEqual(16u, shaderSchema.Fields[1].Offset);
        Assert.IsTrue(shaderSchema.Fields[1].IsInherited);

        Assert.AreEqual("Metallic", shaderSchema.Fields[2].Name);
        Assert.AreEqual(20u, shaderSchema.Fields[2].Offset);
        Assert.IsTrue(shaderSchema.Fields[2].IsInherited);

        // Derived fields start after unpadded template fields (offset 24)
        // DirectSpecularStrength: Float -> offset 24, size 4, align 4
        Assert.AreEqual("DirectSpecularStrength", shaderSchema.Fields[3].Name);
        Assert.AreEqual(24u, shaderSchema.Fields[3].Offset);
        Assert.AreEqual(4u, shaderSchema.Fields[3].Size);
        Assert.IsFalse(shaderSchema.Fields[3].IsInherited);

        // CustomTint: Float3 -> align 16 -> offset 32, size 12
        Assert.AreEqual("CustomTint", shaderSchema.Fields[4].Name);
        Assert.AreEqual(32u, shaderSchema.Fields[4].Offset);
        Assert.AreEqual(12u, shaderSchema.Fields[4].Size);
        Assert.IsFalse(shaderSchema.Fields[4].IsInherited);

        // Total size padded to 16 bytes: 32 + 12 = 44 -> 48
        Assert.AreEqual(48u, shaderSchema.TotalSize);
        Assert.AreNotEqual(0UL, shaderSchema.SchemaId);
        Assert.AreNotEqual(templateSchema!.SchemaId, shaderSchema.SchemaId);
    }

    [TestMethod]
    public void TestForbiddenBoolType_ReportsExplicitError()
    {
        var dsl = @"
shader ""Game/Invalid""
{
    properties
    {
        bool HasEmission;
    }
}
";
        var errors = new List<DSLShaderError>();
        var doc = DSLParser.ParseDocument(dsl, "Invalid.gshdr", errors);
        Assert.IsNotNull(doc);

        var shader = doc.Shaders[0];
        var schema = PropertyLayoutEngine.ComputeShaderLayout(shader, "Game/Invalid", null, errors);
        Assert.IsTrue(errors.Count > 0);
        Assert.IsTrue(errors[0].Message.Contains("'bool' is forbidden"));
    }

    [TestMethod]
    public void TestArrayProperties_ComputesCorrectSizeAndAlignment()
    {
        var dsl = @"
template ""LightArrayTemplate""
{
    properties
    {
        Float4 LightColors[4];
        Float LightIntensities[4];
    }
}
";
        var errors = new List<DSLShaderError>();
        var doc = DSLParser.ParseDocument(dsl, "LightArray.gshdr", errors);
        Assert.IsNotNull(doc);
        Assert.AreEqual(0, errors.Count);

        var template = doc.Templates[0];
        var schema = PropertyLayoutEngine.ComputeTemplateLayout(template, "LightArrayTemplate", errors);
        Assert.IsNotNull(schema);
        Assert.AreEqual(0, errors.Count);

        Assert.AreEqual(2, schema.Fields.Count);

        // LightColors[4]: 4 * 16 = 64 bytes, offset 0
        Assert.AreEqual("LightColors", schema.Fields[0].Name);
        Assert.AreEqual(0u, schema.Fields[0].Offset);
        Assert.AreEqual(64u, schema.Fields[0].Size);
        Assert.AreEqual(4, schema.Fields[0].ArrayLength);

        // LightIntensities[4]: 4 * 4 = 16 bytes, align 16 -> offset 64
        Assert.AreEqual("LightIntensities", schema.Fields[1].Name);
        Assert.AreEqual(64u, schema.Fields[1].Offset);
        Assert.AreEqual(16u, schema.Fields[1].Size);
        Assert.AreEqual(4, schema.Fields[1].ArrayLength);

        Assert.AreEqual(80u, schema.TotalSize);
    }

    [TestMethod]
    public void TestGenerateHlslStruct_ProducesExpectedStructDefinition()
    {
        var dsl = @"
shader ""Game/LitMat""
{
    properties
    {
        Float4 BaseColor;
        Float Roughness;
        TextureHandle MainTexture;
    }
}
";
        var errors = new List<DSLShaderError>();
        var doc = DSLParser.ParseDocument(dsl, "LitMat.gshdr", errors);
        var schema = PropertyLayoutEngine.ComputeShaderLayout(doc!.Shaders[0], "Game/LitMat", null, errors);
        Assert.IsNotNull(schema);

        var hlsl = schema.GenerateHlslStruct("MaterialProperties");
        Assert.IsTrue(hlsl.Contains("struct MaterialProperties"));
        Assert.IsTrue(hlsl.Contains("float4 BaseColor;"));
        Assert.IsTrue(hlsl.Contains("float Roughness;"));
        Assert.IsTrue(hlsl.Contains("uint MainTexture;"));
    }
}
