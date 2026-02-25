using System.Text.Json;

namespace Ghost.UnitTest;

[TestClass]
public class AssetMetaTest
{
    [TestMethod]
    public void TestMetaSerialization()
    {
        var meta = new AssetMeta
        {
            Guid = Guid.NewGuid(),
            Version = 1,
            Tags = new List<string> { "Test", "Asset" }
        };

        var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });

        Assert.IsNotNull(json);
        Assert.Contains("Guid", json);
        Assert.Contains("Version", json);
        Assert.Contains("Tags", json);
    }

    [TestMethod]
    public void TestMetaDeserialization()
    {
        var guid = Guid.NewGuid();

        var json = $@"{{
            ""Guid"": ""{guid}"",
            ""Version"": 1,
            ""Tags"": [""Test"", ""Asset""]
        }}";

        var meta = JsonSerializer.Deserialize<AssetMeta>(json);

        Assert.IsNotNull(meta);
        Assert.AreEqual(guid, meta.Guid);
        Assert.AreEqual(1, meta.Version);
        Assert.HasCount(2, meta.Tags);
        Assert.Contains("Test", meta.Tags);
    }

    [TestMethod]
    public void TestMetaWithSettings()
    {
        var meta = new AssetMeta
        {
            Guid = Guid.NewGuid(),
            Version = 1
        };

        // Add importer settings using the new API
        var settings = new TextImporterSettings
        {
            Encoding = "UTF-8",
            TrimWhitespace = true
        };

        meta.SetImporterSettings("TextImporter", settings);

        var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<AssetMeta>(json);

        Assert.IsNotNull(deserialized);
        Assert.Contains("TextImporter", deserialized.ImporterSettings.Keys);

        // Test retrieving the settings
        var retrievedSettings = deserialized.GetImporterSettings<TextImporterSettings>("TextImporter");
        Assert.IsNotNull(retrievedSettings);
        Assert.AreEqual("UTF-8", retrievedSettings.Encoding);
        Assert.IsTrue(retrievedSettings.TrimWhitespace);
    }

    [TestMethod]
    public void TestFileHashAndDependenciesNotSerialized()
    {
        var meta = new AssetMeta
        {
            Guid = Guid.NewGuid(),
            Version = 1
        };

        var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });

        // FileHash and Dependencies should NOT be in the serialized JSON
        Assert.DoesNotContain("FileHash", json);
        Assert.DoesNotContain("Dependencies", json);
    }
}
