using Ghost.Core;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Services;

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
    public override AssetMetadata? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var id = root.GetProperty("Id").GetGuid();
        var type = Enum.Parse<AssetType>(root.GetProperty("Type").GetString() ?? "Unknown");

        IBakeSettings? settings = null;
        if (root.TryGetProperty("Settings", out var settingsEl))
        {
            var settingsType = BakerRegistry.Instance.GetSettingsType(type);
            if (settingsType != null)
            {
                settings = (IBakeSettings?)JsonSerializer.Deserialize(settingsEl.GetRawText(), settingsType, options);
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
            writer.WritePropertyName("Settings");
            JsonSerializer.Serialize(writer, value.Settings, value.Settings.GetType(), options);
        }
        writer.WriteEndObject();
    }
}
