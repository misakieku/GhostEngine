using Ghost.AssetForge.Core.Bakers;
using Ghost.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.AssetForge.Core.Models;

[JsonConverter(typeof(AssetMetadataConverter))]
public record AssetMetadata
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public AssetType Type { get; init; } = AssetType.Unknown;

    // Custom converter handles polymorphic deserialization of IBakeSettings based on Type
    public IBakeSettings? Settings { get; init; }
}

public class AssetMetadataConverter : JsonConverter<AssetMetadata>
{
    private static Type? GetSettingsTypeByName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        var type = Type.GetType(typeName);
        if (type != null) return type;

        // Try searching loaded assemblies by full name or simple class name
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null) return type;

            try
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.Name == typeName || t.FullName == typeName)
                    {
                        return t;
                    }
                }
            }
            catch
            {
                // Ignore assembly loading/resolution issues for specific dynamic assemblies
            }
        }
        return null;
    }

    public override AssetMetadata? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var id = root.GetProperty("Id").GetGuid();
        var type = Enum.Parse<AssetType>(root.GetProperty("Type").GetString() ?? "Unknown");

        IBakeSettings? settings = null;
        if (root.TryGetProperty("SettingsType", out var settingsTypeEl) && root.TryGetProperty("Settings", out var settingsEl))
        {
            var typeName = settingsTypeEl.GetString();
            if (!string.IsNullOrEmpty(typeName))
            {
                var settingsType = GetSettingsTypeByName(typeName);
                if (settingsType != null)
                {
                    settings = (IBakeSettings?)JsonSerializer.Deserialize(settingsEl.GetRawText(), settingsType, options);
                }
            }
        }

        return new AssetMetadata
        {
            Id = id,
            Type = type,
            Settings = settings
        };
    }

    public override void Write(Utf8JsonWriter writer, AssetMetadata value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Id", value.Id);
        writer.WriteString("Type", value.Type.ToString());
        if (value.Settings != null)
        {
            var settingsType = value.Settings.GetType();
            writer.WriteString("SettingsType", settingsType.AssemblyQualifiedName ?? settingsType.FullName ?? settingsType.Name);
            writer.WritePropertyName("Settings");
            JsonSerializer.Serialize(writer, value.Settings, settingsType, options);
        }
        writer.WriteEndObject();
    }
}
