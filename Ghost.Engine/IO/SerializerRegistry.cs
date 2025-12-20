using Ghost.Core;
using Ghost.Entities;
using System.Text.Json;

namespace Ghost.Engine.IO;

public static unsafe class ComponentSerializerRegistry
{
    public delegate void BinaryWriteDelegate(BinaryWriter writer, void* ptr);
    public delegate void JsonWriteDelegate(Utf8JsonWriter writer, void* ptr, JsonSerializerOptions options);

    private static BinaryWriteDelegate[] s_binWriters = new BinaryWriteDelegate[64];
    private static JsonWriteDelegate[] s_jsonWriters = new JsonWriteDelegate[64];

    public static void Register(int typeID, BinaryWriteDelegate binWriter, JsonWriteDelegate jsonWriter)
    {
        if (typeID < 0)
        {
            throw new Exception($"Type ID cannot be negative: {typeID}");
        }

        if (typeID >= s_binWriters.Length)
        {
            Array.Resize(ref s_binWriters, typeID + 16);
            Array.Resize(ref s_jsonWriters, typeID + 16);
        }

        s_binWriters[typeID] = binWriter;
        s_jsonWriters[typeID] = jsonWriter;
    }

    public static void SerializeBinary(Identifier<IComponent> typeID, BinaryWriter writer, void* ptr)
    {
        if (s_binWriters[typeID] == null)
        {
            throw new Exception($"No serializer for ID {typeID}");
        }

        s_binWriters[typeID](writer, ptr);
    }

    public static void SerializeJson(Identifier<IComponent> typeID, Utf8JsonWriter writer, void* ptr, JsonSerializerOptions options)
    {
        if (s_jsonWriters[typeID] == null)
        {
            // TODO: Fallback to reflection?
            return;
        }

        s_jsonWriters[typeID](writer, ptr, options);
    }
}