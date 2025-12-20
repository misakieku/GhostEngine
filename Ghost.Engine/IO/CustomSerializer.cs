using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Engine.IO;

public class CustomSerializerAttribute : JsonConverterAttribute
{
    public CustomSerializerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type serializerType)
        : base(serializerType)
    {
    }
}

public abstract class CustomSerializer<T> : JsonConverter<T>
{
    public sealed override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DeserializeJson(ref reader, options);
    }

    public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        SerializeJson(writer, value, options);
    }

    public abstract void SerializeJson(Utf8JsonWriter writer, T value, JsonSerializerOptions options);
    public abstract T? DeserializeJson(ref Utf8JsonReader reader, JsonSerializerOptions options);

    public abstract void SerializeBinary(BinaryWriter writer, T value);
    public abstract T? DeserializeBinary(BinaryReader reader);
}