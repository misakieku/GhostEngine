using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Ghost.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.AssetForge.Test;

[TestClass]
public class DSLSourceGeneratorTests
{
    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content, Encoding.UTF8);
        }

        public override string Path { get; }
        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }

    [TestMethod]
    public void TestShaderCatalogGenerator_GeneratesInterfaceImplementationAndShaderTags()
    {
        var dslContent = @"
module ""Ghost.Rendering.Interfaces""
{
    export interface pipeline IShadow;
    export interface pipeline IFog;
}

module ""Ghost.Rendering.StandardFeatures""
{
    import ""Ghost.Rendering.Interfaces"";

    export implementation VirtualShadowMap : IShadow
    {
        provider = ""Ghost.Rendering.VirtualShadowMapFeature"";
        static float Evaluate() { return 1.0; }
    }

    export implementation VolumetricFog : IFog
    {
        static float Evaluate() { return 0.5; }
    }
}

module ""Game.Materials""
{
    import ""Ghost.Rendering.Interfaces"";

    export template LitTemplate
    {
        properties
        {
            Float4 BaseColor;
            Float Roughness;
            Float Metallic;
        }

        slot
        {
            IShadow;
            IFog;
        }
    }

    export shader CustomLit : LitTemplate
    {
        properties
        {
            Float DirectSpecularStrength;
            Float3 CustomTint;
        }
    }
}
";
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Ghost.Core.Graphics.ShaderInterfaceId).Assembly.Location),
            });

        var additionalText = new InMemoryAdditionalText("Assets/Shaders/Shaders.gshdr", dslContent);
        var generator = new GhostShaderCatalogGenerator();

        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalText));

        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        Assert.AreEqual(2, runResult.GeneratedTrees.Length);

        var catalogTree = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("GhostShaderCatalog.g.cs"));
        Assert.IsNotNull(catalogTree);
        var catalogText = catalogTree.GetText().ToString();

        // Check interfaces generated
        Assert.IsTrue(catalogText.Contains("IShadow : IShaderInterfaceTag"), "Should contain IShadow interface tag.");
        Assert.IsTrue(catalogText.Contains("IFog : IShaderInterfaceTag"), "Should contain IFog interface tag.");

        // Check implementations generated
        Assert.IsTrue(catalogText.Contains("VirtualShadowMap : IShaderImplementationTag<Interfaces.Ghost_Rendering_Interfaces_IShadow>"), "Should contain VirtualShadowMap implementation tag.");
        Assert.IsTrue(catalogText.Contains("VolumetricFog : IShaderImplementationTag<Interfaces.Ghost_Rendering_Interfaces_IFog>"), "Should contain VolumetricFog implementation tag.");

        // Check shader tag generated
        Assert.IsTrue(catalogText.Contains("CustomLit : IShaderTag"), "Should contain CustomLit shader tag.");

        var propertiesTree = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("GhostShaderProperties.g.cs"));
        Assert.IsNotNull(propertiesTree);
        var propertiesText = propertiesTree.GetText().ToString();

        // Check properties structs generated
        Assert.IsTrue(propertiesText.Contains("Game_Materials_CustomLitProperties : IShaderProperties"), "Should contain CustomLit property struct.");
        Assert.IsTrue(propertiesText.Contains("[FieldOffset(0)] public float4 BaseColor;"));
        Assert.IsTrue(propertiesText.Contains("[FieldOffset(16)] public float Roughness;"));
        Assert.IsTrue(propertiesText.Contains("[FieldOffset(20)] public float Metallic;"));
        Assert.IsTrue(propertiesText.Contains("[FieldOffset(24)] public float DirectSpecularStrength;"));
        Assert.IsTrue(propertiesText.Contains("[FieldOffset(32)] public float3 CustomTint;"));
        Assert.IsTrue(propertiesText.Contains("public static uint PropertySize => 48;"));
    }
}
