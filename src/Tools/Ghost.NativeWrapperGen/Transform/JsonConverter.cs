using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.NativeWrapperGen.Transform;

internal class DynamicJsonConverter : JsonConverter<dynamic>
{
    public override dynamic Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Parse the JSON into a strict JsonElement, then wrap it in our dynamic class
        using var document = JsonDocument.ParseValue(ref reader);
        return DynamicJsonWrapper.Wrap(document.RootElement.Clone())!;
    }

    public override void Write(Utf8JsonWriter writer, dynamic value, JsonSerializerOptions options)
    {
        // (Skipped for brevity, but you would serialize the object back here if needed)
        throw new NotImplementedException();
    }
}

internal class DynamicJsonWrapper : DynamicObject
{
    private readonly JsonElement _element;

    public DynamicJsonWrapper(JsonElement element)
    {
        _element = element;
    }

    // This method intercepts dynamic property access (e.g., .NestedValue)
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        if (_element.ValueKind == JsonValueKind.Object && _element.TryGetProperty(binder.Name, out var property))
        {
            result = Wrap(property);
            return true; // Property found!
        }

        result = null;
        return true; // Property not found — return null instead of throwing RuntimeBinderException.
    }

    // Converts JsonElements into primitives or wraps nested objects
    public static object? Wrap(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => new DynamicJsonWrapper(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.Array => element.EnumerateArray().Select(Wrap).ToArray(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }
}