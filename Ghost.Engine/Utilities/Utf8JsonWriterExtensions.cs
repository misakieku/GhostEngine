using System.Text.Json;

namespace Ghost.Engine.Utilities;

public static class Utf8JsonWriterExtensions
{
    public static void WriteArray<T>(this Utf8JsonWriter writer, ReadOnlySpan<char> name, IEnumerable<T> source, Action<T> writeAction)
    {
        writer.WriteStartArray(name);
        foreach (var item in source)
        {
            writeAction(item);
        }
        writer.WriteEndArray();
    }

    public static void WriteArray<T>(this Utf8JsonWriter writer, ReadOnlySpan<char> name, ReadOnlySpan<T> source, Action<T> writeAction)
    {
        writer.WriteStartArray(name);
        foreach (var item in source)
        {
            writeAction(item);
        }
        writer.WriteEndArray();
    }

    public static void WriteObject(this Utf8JsonWriter writer, Action writeAction)
    {
        writer.WriteStartObject();
        writeAction();
        writer.WriteEndObject();
    }

    public static void WriteObject(this Utf8JsonWriter writer, ReadOnlySpan<char> name, Action writeAction)
    {
        writer.WriteStartObject(name);
        writeAction();
        writer.WriteEndObject();
    }
}