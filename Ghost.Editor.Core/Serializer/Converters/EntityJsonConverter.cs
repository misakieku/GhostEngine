using Ghost.Entities;
using Ghost.Engine.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Editor.Core.Serializer.Converters;

/// <summary>
/// JSON converter for Entity that handles automatic ID remapping during deserialization.
/// </summary>
/// <remarks>
/// During serialization, writes the file-local entity ID.
/// During deserialization, reads the file-local ID and translates it to the runtime Entity
/// using the active SerializationContext.
/// </remarks>
public class EntityJsonConverter : JsonConverter<Entity>
{
    public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Entity.Invalid;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token for Entity.");
        }

        int fileId = -1;
        int generation = -1;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "ID":
                    case "id":
                        fileId = reader.GetInt32();
                        break;
                    case "Generation":
                    case "generation":
                        generation = reader.GetInt32();
                        break;
                }
            }
        }

        if (fileId == Entity.INVALID_ID)
        {
            return Entity.Invalid;
        }

        // If we have a serialization context, remap the file ID to runtime entity
        var context = SerializationContext.Current;
        if (context != null)
        {
            if (context.TryGetEntity(fileId, out var runtimeEntity))
            {
                return runtimeEntity;
            }

            // If entity not found in map, return invalid
            return Entity.Invalid;
        }

        // No context means we're not in a deserialization scope - should not happen
        throw new InvalidOperationException("Entity deserialization requires an active SerializationContext.");
    }

    public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
    {
        if (!value.IsValid)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        // If we have a serialization context, write the file-local ID
        var context = SerializationContext.Current;
        if (context != null)
        {
            if (context.TryGetFileId(value, out var fileId))
            {
                writer.WriteNumber("ID", fileId);
            }
            else
            {
                // Entity not in context - register it now
                var newFileId = context.RegisterEntityForSerialization(value);
                writer.WriteNumber("ID", newFileId);
            }
        }
        else
        {
            // No context - write the runtime ID (for debugging or non-scene serialization)
            writer.WriteNumber("ID", value.ID);
        }

        writer.WriteNumber("Generation", value.Generation);
        writer.WriteEndObject();
    }
}
