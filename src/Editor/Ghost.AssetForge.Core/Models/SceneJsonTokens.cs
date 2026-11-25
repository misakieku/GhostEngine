using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.AssetForge.Core.Models;

[JsonSerializable(typeof(SceneAssetRefToken))]
[JsonSerializable(typeof(SceneEntityRefToken))]
[JsonSerializable(typeof(ScenePaletteRefToken))]
internal partial class SceneTokenDocumentContext : JsonSerializerContext
{
}

internal readonly record struct SceneAssetRefToken([property: JsonPropertyName(SceneJsonTokens.ASSET_KEY)] Guid Asset);

internal readonly record struct SceneEntityRefToken([property: JsonPropertyName(SceneJsonTokens.ENTITY_KEY)] Guid Entity);

internal readonly record struct ScenePaletteRefToken([property: JsonPropertyName(SceneJsonTokens.PALETTE_KEY)] Guid[] Palette);

/// <summary>
/// Encodes and inspects tagged authoring reference objects inside <see cref="SceneAssetDocument"/> <see cref="JsonElement"/> fields
/// so <c>SceneBaker</c> can extract asset and entity references without depending on runtime engine types.
/// </summary>
public static class SceneJsonTokens
{
    public const string ASSET_KEY = "asset";
    public const string ENTITY_KEY = "entity";
    public const string PALETTE_KEY = "palette";

    public static JsonElement CreateAssetRef(Guid assetGuid)
    {
        return JsonSerializer.SerializeToElement(new SceneAssetRefToken(assetGuid), SceneTokenDocumentContext.Default.SceneAssetRefToken);
    }

    public static bool TryGetAssetRef(JsonElement element, out Guid assetGuid)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(ASSET_KEY, out var value) && TryReadGuid(value, out assetGuid))
        {
            return true;
        }

        assetGuid = Guid.Empty;
        return false;
    }

    public static JsonElement CreateEntityRef(Guid entityGuid)
    {
        return JsonSerializer.SerializeToElement(new SceneEntityRefToken(entityGuid), SceneTokenDocumentContext.Default.SceneEntityRefToken);
    }

    public static bool TryGetEntityRef(JsonElement element, out Guid entityGuid)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(ENTITY_KEY, out var value) && TryReadGuid(value, out entityGuid))
        {
            return true;
        }

        entityGuid = Guid.Empty;
        return false;
    }

    public static JsonElement CreatePaletteRef(IReadOnlyList<Guid> materialGuids)
    {
        var copy = materialGuids.Count == 0 ? Array.Empty<Guid>() : materialGuids as Guid[] ?? materialGuids.ToArray();
        return JsonSerializer.SerializeToElement(new ScenePaletteRefToken(copy), SceneTokenDocumentContext.Default.ScenePaletteRefToken);
    }

    public static bool TryGetPaletteRef(JsonElement element, out Guid[] materialGuids)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(PALETTE_KEY, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            var len = arr.GetArrayLength();
            var result = len == 0 ? Array.Empty<Guid>() : new Guid[len];
            for (var i = 0; i < len; i++)
            {
                result[i] = TryReadGuid(arr[i], out var guid) ? guid : Guid.Empty;
            }

            materialGuids = result;
            return true;
        }

        materialGuids = Array.Empty<Guid>();
        return false;
    }

    private static bool TryReadGuid(JsonElement element, out Guid guid)
    {
        if (element.ValueKind == JsonValueKind.String && element.TryGetGuid(out guid))
        {
            return true;
        }

        guid = Guid.Empty;
        return false;
    }
}
