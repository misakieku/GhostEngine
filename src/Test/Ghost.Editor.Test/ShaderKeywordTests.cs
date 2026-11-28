using Ghost.AssetForge.Core.Bakers;
using Ghost.Core.Graphics;
using Ghost.DSL.ShaderCompiler.Templates;

namespace Ghost.Editor.Test;

[TestClass]
public sealed class ShaderKeywordTests
{
    [TestMethod]
    public void KeywordDef_GeneratesDisabledAndEnabledVariants()
    {
        var defaultNamed = new KeywordDef { Keyword = "COLORED_SHADOW" };
        Assert.AreEqual("COLORED_SHADOW", defaultNamed.Name);
        CollectionAssert.AreEqual(new[] { string.Empty, "COLORED_SHADOW" }, defaultNamed.Keywords);

        var customNamed = new KeywordDef { Name = "ColoredShadow", Keyword = "COLORED_SHADOW" };
        Assert.AreEqual("ColoredShadow", customNamed.Name);
        CollectionAssert.AreEqual(new[] { string.Empty, "COLORED_SHADOW" }, customNamed.Keywords);

        var empty = new KeywordDef();
        Assert.AreEqual(string.Empty, empty.Name);
        Assert.HasCount(0, empty.Keywords);
    }

    [TestMethod]
    public void GenerateKeywordPermutations_WithKeywordDef_ExcludesEmptyDefines()
    {
        var singleToggle = new IKeywordDef[] { new KeywordDef { Keyword = "COLORED_SHADOW" } };
        var togglePermutations = ShaderBaker.GenerateKeywordPermutations(singleToggle);

        Assert.HasCount(2, togglePermutations);
        CollectionAssert.AreEqual(Array.Empty<string>(), togglePermutations[0]);
        CollectionAssert.AreEqual(new[] { "COLORED_SHADOW" }, togglePermutations[1]);

        var mixed = new IKeywordDef[]
        {
            new KeywordGroupDef { Name = "ShadowQuality", Keywords = new[] { "_SHADOW_LOW", "_SHADOW_HIGH" } },
            new KeywordDef { Keyword = "COLORED_SHADOW" },
        };
        var mixedPermutations = ShaderBaker.GenerateKeywordPermutations(mixed);

        Assert.HasCount(4, mixedPermutations);
        CollectionAssert.AreEqual(new[] { "_SHADOW_LOW" }, mixedPermutations[0]);
        CollectionAssert.AreEqual(new[] { "_SHADOW_LOW", "COLORED_SHADOW" }, mixedPermutations[1]);
        CollectionAssert.AreEqual(new[] { "_SHADOW_HIGH" }, mixedPermutations[2]);
        CollectionAssert.AreEqual(new[] { "_SHADOW_HIGH", "COLORED_SHADOW" }, mixedPermutations[3]);
    }

    [TestMethod]
    public void TemplatePassDef_SupportsMixedKeywordGroupsAndKeywordDefs()
    {
        var passDef = new TemplatePassDef
        {
            name = "TestPass",
            keywordGroups = new List<IKeywordDef>
            {
                new KeywordGroupDef { Name = "Shadow", Keywords = new[] { "_SHADOW_OFF", "_SHADOW_ON" } },
                new KeywordDef { Name = "ColoredShadow", Keyword = "COLORED_SHADOW" },
            },
        };

        var permutations = ShaderBaker.GenerateKeywordPermutations(passDef.keywordGroups);
        Assert.HasCount(4, permutations);
        Assert.IsTrue(permutations.All(p => p.All(k => !string.IsNullOrEmpty(k))));
    }
}
