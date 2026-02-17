using System.Text.Json;

namespace Ghost.Engine.Utilities;

public readonly ref struct Utf8JsonObjectScope : IDisposable
{
    private readonly Utf8JsonWriter _writer;
    public Utf8JsonObjectScope(Utf8JsonWriter writer)
    {
        _writer = writer;
        _writer.WriteStartObject();
    }
    public void Dispose()
    {
        _writer.WriteEndObject();
    }
}

public readonly ref struct Utf8JsonArrayScope : IDisposable
{
    private readonly Utf8JsonWriter _writer;
    public Utf8JsonArrayScope(Utf8JsonWriter writer)
    {
        _writer = writer;
        _writer.WriteStartArray();
    }
    public void Dispose()
    {
        _writer.WriteEndArray();
    }
}

public static class Utf8JsonWriterExtension
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

    public static Utf8JsonObjectScope StartObjectScope(this Utf8JsonWriter writer)
    {
        return new Utf8JsonObjectScope(writer);
    }

    public static Utf8JsonArrayScope StartArrayScope(this Utf8JsonWriter writer)
    {
        return new Utf8JsonArrayScope(writer);
    }
}