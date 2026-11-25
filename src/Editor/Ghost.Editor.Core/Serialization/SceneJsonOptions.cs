using Ghost.Graphics.Core;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Editor.Core.Serialization;

/// <summary>
/// Provides <see cref="JsonSerializerOptions"/> and custom converters for converting editor node property values
/// (math vectors, quaternions, matrices, enums, and bitmasks) to and from compact <see cref="JsonElement"/> tokens.
/// </summary>
public static class SceneJsonOptions
{
    public static JsonSerializerOptions Default { get; } = CreateDefault();

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            Converters =
            {
                new JsonStringEnumConverter(),
                new Float2JsonConverter(),
                new Float3JsonConverter(),
                new Float4JsonConverter(),
                new QuaternionJsonConverter(),
                new Float4x4JsonConverter(),
                new Int2JsonConverter(),
                new Int3JsonConverter(),
                new Int4JsonConverter(),
                new UInt2JsonConverter(),
                new UInt3JsonConverter(),
                new UInt4JsonConverter(),
                new RenderingLayerMaskJsonConverter(),
            }
        };
        return options;
    }

    /// <summary>
    /// Compares two <see cref="JsonElement"/> values semantically (ignoring formatting and float representation quirks).
    /// </summary>
    public static bool JsonElementsEqual(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind)
        {
            return false;
        }

        switch (a.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
            case JsonValueKind.True:
            case JsonValueKind.False:
                return true;

            case JsonValueKind.Number:
                if (a.TryGetInt64(out var la) && b.TryGetInt64(out var lb))
                {
                    return la == lb;
                }

                if (a.TryGetUInt64(out var ula) && b.TryGetUInt64(out var ulb))
                {
                    return ula == ulb;
                }

                return Math.Abs(a.GetDouble() - b.GetDouble()) <= 1e-6;

            case JsonValueKind.String:
                return string.Equals(a.GetString(), b.GetString(), StringComparison.Ordinal);

            case JsonValueKind.Array:
            {
                var len = a.GetArrayLength();
                if (len != b.GetArrayLength())
                {
                    return false;
                }

                for (var i = 0; i < len; i++)
                {
                    if (!JsonElementsEqual(a[i], b[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            case JsonValueKind.Object:
            {
                var countA = 0;
                foreach (var propA in a.EnumerateObject())
                {
                    countA++;
                    if (!b.TryGetProperty(propA.Name, out var propB) || !JsonElementsEqual(propA.Value, propB))
                    {
                        return false;
                    }
                }

                var countB = 0;
                foreach (var _ in b.EnumerateObject())
                {
                    countB++;
                }

                return countA == countB;
            }

            default:
                return false;
        }
    }

    private static void ReadFloatArray(ref Utf8JsonReader reader, scoped Span<float> dst)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected StartArray for {dst.Length}-component float vector.");
        }

        for (var i = 0; i < dst.Length; i++)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException("Unexpected token inside float vector array.");
            }

            dst[i] = reader.GetSingle();
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Expected EndArray after float vector.");
        }
    }

    private static void WriteFloatArray(Utf8JsonWriter writer, ReadOnlySpan<float> src)
    {
        writer.WriteStartArray();
        for (var i = 0; i < src.Length; i++)
        {
            writer.WriteNumberValue(src[i]);
        }
        writer.WriteEndArray();
    }

    private static void ReadIntArray(ref Utf8JsonReader reader, scoped Span<int> dst)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected StartArray for {dst.Length}-component int vector.");
        }

        for (var i = 0; i < dst.Length; i++)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException("Unexpected token inside int vector array.");
            }

            dst[i] = reader.GetInt32();
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Expected EndArray after int vector.");
        }
    }

    private static void WriteIntArray(Utf8JsonWriter writer, ReadOnlySpan<int> src)
    {
        writer.WriteStartArray();
        for (var i = 0; i < src.Length; i++)
        {
            writer.WriteNumberValue(src[i]);
        }
        writer.WriteEndArray();
    }

    private static void ReadUIntArray(ref Utf8JsonReader reader, scoped Span<uint> dst)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected StartArray for {dst.Length}-component uint vector.");
        }

        for (var i = 0; i < dst.Length; i++)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException("Unexpected token inside uint vector array.");
            }

            dst[i] = reader.GetUInt32();
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Expected EndArray after uint vector.");
        }
    }

    private static void WriteUIntArray(Utf8JsonWriter writer, ReadOnlySpan<uint> src)
    {
        writer.WriteStartArray();
        for (var i = 0; i < src.Length; i++)
        {
            writer.WriteNumberValue(src[i]);
        }
        writer.WriteEndArray();
    }

    private sealed class Float2JsonConverter : JsonConverter<float2>
    {
        public override float2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<float> v = stackalloc float[2];
            ReadFloatArray(ref reader, v);
            return new float2(v[0], v[1]);
        }

        public override void Write(Utf8JsonWriter writer, float2 value, JsonSerializerOptions options)
        {
            WriteFloatArray(writer, new[] { value.x, value.y });
        }
    }

    private sealed class Float3JsonConverter : JsonConverter<float3>
    {
        public override float3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<float> v = stackalloc float[3];
            ReadFloatArray(ref reader, v);
            return new float3(v[0], v[1], v[2]);
        }

        public override void Write(Utf8JsonWriter writer, float3 value, JsonSerializerOptions options)
        {
            WriteFloatArray(writer, new[] { value.x, value.y, value.z });
        }
    }

    private sealed class Float4JsonConverter : JsonConverter<float4>
    {
        public override float4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<float> v = stackalloc float[4];
            ReadFloatArray(ref reader, v);
            return new float4(v[0], v[1], v[2], v[3]);
        }

        public override void Write(Utf8JsonWriter writer, float4 value, JsonSerializerOptions options)
        {
            WriteFloatArray(writer, new[] { value.x, value.y, value.z, value.w });
        }
    }

    private sealed class QuaternionJsonConverter : JsonConverter<quaternion>
    {
        public override quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<float> v = stackalloc float[4];
            ReadFloatArray(ref reader, v);
            return new quaternion(v[0], v[1], v[2], v[3]);
        }

        public override void Write(Utf8JsonWriter writer, quaternion value, JsonSerializerOptions options)
        {
            var v = Unsafe.BitCast<quaternion, float4>(value);
            WriteFloatArray(writer, new[] { v.x, v.y, v.z, v.w });
        }
    }

    private sealed class Float4x4JsonConverter : JsonConverter<float4x4>
    {
        public override float4x4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<float> m = stackalloc float[16];
            ReadFloatArray(ref reader, m);
            return new float4x4(
                new float4(m[0], m[1], m[2], m[3]),
                new float4(m[4], m[5], m[6], m[7]),
                new float4(m[8], m[9], m[10], m[11]),
                new float4(m[12], m[13], m[14], m[15]));
        }

        public override void Write(Utf8JsonWriter writer, float4x4 value, JsonSerializerOptions options)
        {
            WriteFloatArray(writer, new[]
            {
                value.c0.x, value.c0.y, value.c0.z, value.c0.w,
                value.c1.x, value.c1.y, value.c1.z, value.c1.w,
                value.c2.x, value.c2.y, value.c2.z, value.c2.w,
                value.c3.x, value.c3.y, value.c3.z, value.c3.w,
            });
        }
    }

    private sealed class Int2JsonConverter : JsonConverter<int2>
    {
        public override int2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<int> v = stackalloc int[2];
            ReadIntArray(ref reader, v);
            return new int2(v[0], v[1]);
        }

        public override void Write(Utf8JsonWriter writer, int2 value, JsonSerializerOptions options)
        {
            WriteIntArray(writer, new[] { value.x, value.y });
        }
    }

    private sealed class Int3JsonConverter : JsonConverter<int3>
    {
        public override int3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<int> v = stackalloc int[3];
            ReadIntArray(ref reader, v);
            return new int3(v[0], v[1], v[2]);
        }

        public override void Write(Utf8JsonWriter writer, int3 value, JsonSerializerOptions options)
        {
            WriteIntArray(writer, new[] { value.x, value.y, value.z });
        }
    }

    private sealed class Int4JsonConverter : JsonConverter<int4>
    {
        public override int4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<int> v = stackalloc int[4];
            ReadIntArray(ref reader, v);
            return new int4(v[0], v[1], v[2], v[3]);
        }

        public override void Write(Utf8JsonWriter writer, int4 value, JsonSerializerOptions options)
        {
            WriteIntArray(writer, new[] { value.x, value.y, value.z, value.w });
        }
    }

    private sealed class UInt2JsonConverter : JsonConverter<uint2>
    {
        public override uint2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<uint> v = stackalloc uint[2];
            ReadUIntArray(ref reader, v);
            return new uint2(v[0], v[1]);
        }

        public override void Write(Utf8JsonWriter writer, uint2 value, JsonSerializerOptions options)
        {
            WriteUIntArray(writer, new[] { value.x, value.y });
        }
    }

    private sealed class UInt3JsonConverter : JsonConverter<uint3>
    {
        public override uint3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<uint> v = stackalloc uint[3];
            ReadUIntArray(ref reader, v);
            return new uint3(v[0], v[1], v[2]);
        }

        public override void Write(Utf8JsonWriter writer, uint3 value, JsonSerializerOptions options)
        {
            WriteUIntArray(writer, new[] { value.x, value.y, value.z });
        }
    }

    private sealed class UInt4JsonConverter : JsonConverter<uint4>
    {
        public override uint4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<uint> v = stackalloc uint[4];
            ReadUIntArray(ref reader, v);
            return new uint4(v[0], v[1], v[2], v[3]);
        }

        public override void Write(Utf8JsonWriter writer, uint4 value, JsonSerializerOptions options)
        {
            WriteUIntArray(writer, new[] { value.x, value.y, value.z, value.w });
        }
    }

    private sealed class RenderingLayerMaskJsonConverter : JsonConverter<RenderingLayerMask>
    {
        public override RenderingLayerMask Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new RenderingLayerMask(reader.GetUInt32());
        }

        public override void Write(Utf8JsonWriter writer, RenderingLayerMask value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((uint)value);
        }
    }
}
