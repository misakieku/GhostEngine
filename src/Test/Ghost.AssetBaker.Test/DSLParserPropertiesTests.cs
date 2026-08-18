using System.Collections.Generic;
using Ghost.DSL.Parser;
using Ghost.DSL.ShaderParser.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.AssetForge.Test;

[TestClass]
public class DSLParserPropertiesTests
{
    [TestMethod]
    public void TestParseTemplateAndShaderProperties()
    {
        var dsl = @"
template ""BaseTemplate""
{
    properties
    {
        Float4 Albedo;
        Float Metallic;
    }
}

shader ""DerivedShader"" : ""BaseTemplate""
{
    properties
    {
        Float3 Emission;
        TextureHandle NormalMap;
    }
}
";
        var errors = new List<DSLShaderError>();
        var doc = DSLParser.ParseDocument(dsl, "Test.gshdr", errors);

        Assert.IsNotNull(doc);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(1, doc.Templates.Count);
        Assert.AreEqual(1, doc.Shaders.Count);

        var template = doc.Templates[0];
        Assert.IsNotNull(template.Properties);
        Assert.AreEqual(2, template.Properties.Declarations.Count);
        Assert.AreEqual("Albedo", template.Properties.Declarations[0].Name);
        Assert.AreEqual("Float4", template.Properties.Declarations[0].TypeName);
        Assert.AreEqual("Metallic", template.Properties.Declarations[1].Name);
        Assert.AreEqual("Float", template.Properties.Declarations[1].TypeName);

        var shader = doc.Shaders[0];
        Assert.IsNotNull(shader.Properties);
        Assert.AreEqual(2, shader.Properties.Declarations.Count);
        Assert.AreEqual("Emission", shader.Properties.Declarations[0].Name);
        Assert.AreEqual("Float3", shader.Properties.Declarations[0].TypeName);
        Assert.AreEqual("NormalMap", shader.Properties.Declarations[1].Name);
        Assert.AreEqual("TextureHandle", shader.Properties.Declarations[1].TypeName);
    }

    [TestMethod]
    public void TestParseComputeShaderProperties()
    {
        var dsl = @"
compute ""UpdateParticles""
{
    sm 6_6;

    properties
    {
        Float DeltaTime;
        Uint ParticleCount;
        BufferHandle ParticleBuffer;
    }
}
";
        var errors = new List<DSLShaderError>();
        var compute = DSLParser.ParseComputeShader(dsl, "UpdateParticles.gcomp", errors);

        Assert.IsNotNull(compute);
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual("UpdateParticles", compute.Name);
        Assert.IsNotNull(compute.Properties);
        Assert.AreEqual(3, compute.Properties.Declarations.Count);
        Assert.AreEqual("DeltaTime", compute.Properties.Declarations[0].Name);
        Assert.AreEqual("Float", compute.Properties.Declarations[0].TypeName);
        Assert.AreEqual("ParticleCount", compute.Properties.Declarations[1].Name);
        Assert.AreEqual("Uint", compute.Properties.Declarations[1].TypeName);
        Assert.AreEqual("ParticleBuffer", compute.Properties.Declarations[2].Name);
        Assert.AreEqual("BufferHandle", compute.Properties.Declarations[2].TypeName);
    }
}
