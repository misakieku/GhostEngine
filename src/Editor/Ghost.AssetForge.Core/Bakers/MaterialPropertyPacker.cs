using Ghost.Core;
using Ghost.DSL.ShaderCompiler;
using Ghost.DSL.ShaderCompiler.Templates;
using Misaki.HighPerformance.Mathematics;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace Ghost.AssetForge.Core.Bakers;

public static partial class MaterialPropertyPacker
{
    public static readonly Guid STANDARD_LIT_SHADER_GUID = Guid.Parse("dea05727-fa9e-41ac-b039-f2c0411a0e04");

    private const string STANDARD_LIT_SOURCE = """
shader "Ghost/StandardLit" : "Lit"
{
    properties
    {
        float3 baseColor = float3(0.73, 0.73, 0.73);
        Texture2D baseColorMap;
        Texture2D normalMap;
        Texture2D ormMap;
        Sampler sampler;
        float roughness = 0.5;
        float metallic;
        float3 emissionColor;
        Texture2D emissionMap;
    }
}
""";

    private static readonly ConcurrentDictionary<Guid, GraphicsShaderSemantics> s_semanticsCache = new();

    public static GraphicsShaderSemantics? GetShaderSemantics(Guid shaderGuid, IReadOnlyList<string>? assetDirectories = null)
    {
        if (shaderGuid == Guid.Empty)
        {
            shaderGuid = STANDARD_LIT_SHADER_GUID;
        }

        if (s_semanticsCache.TryGetValue(shaderGuid, out var cached))
        {
            return cached;
        }

        var sourceCode = FindShaderSourceCode(shaderGuid, assetDirectories);
        if (string.IsNullOrEmpty(sourceCode))
        {
            if (shaderGuid == STANDARD_LIT_SHADER_GUID)
            {
                sourceCode = STANDARD_LIT_SOURCE;
            }
            else
            {
                return null;
            }
        }

        var syntaxRes = DSLShaderCompiler.ParseGraphicsShaderSyntax(sourceCode);
        if (syntaxRes.IsFailure)
        {
            return null;
        }

        var semanticsRes = DSLShaderCompiler.GetShaderSemantics(syntaxRes.Value);
        if (semanticsRes.IsFailure || semanticsRes.Value == null)
        {
            return null;
        }

        s_semanticsCache.TryAdd(shaderGuid, semanticsRes.Value);
        return semanticsRes.Value;
    }

    private static string? FindShaderSourceCode(Guid shaderGuid, IReadOnlyList<string>? assetDirectories)
    {
        if (assetDirectories == null || assetDirectories.Count == 0)
        {
            return null;
        }

        foreach (var dir in assetDirectories)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            var metaFiles = Directory.GetFiles(dir, "*.gshdr.meta", SearchOption.AllDirectories);
            foreach (var metaFile in metaFiles)
            {
                try
                {
                    var text = File.ReadAllText(metaFile);
                    using var doc = JsonDocument.Parse(text);
                    if (doc.RootElement.TryGetProperty("Id", out var idProp) && idProp.GetGuid() == shaderGuid)
                    {
                        var shaderPath = metaFile[..^5];
                        if (File.Exists(shaderPath))
                        {
                            return File.ReadAllText(shaderPath);
                        }
                    }
                }
                catch
                {
                }
            }
        }

        return null;
    }

    public static (byte[] propertyBuffer, List<MaterialTextureFixup> fixups) Pack(GraphicsShaderSemantics semantics, IDictionary<string, JsonElement>? jsonProperties = null,
        IDictionary<string, Guid>? jsonTextures = null, Func<string, object?>? valueFallback = null, Func<string, Guid?>? textureFallback = null)
    {
        var isTemplateShader = !string.IsNullOrEmpty(semantics.templateName);
        var allProperties = new List<PropertySemantic>();
        var seenProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (isTemplateShader && TemplateRegistry.GetTemplate(semantics.templateName!) is { IsSuccess: true, Value: var template })
        {
            foreach (var prop in template.BaseProperties)
            {
                if (seenProperties.Add(prop.name))
                {
                    allProperties.Add(new PropertySemantic
                    {
                        type = prop.type,
                        name = prop.name,
                        defaultValue = prop.defaultValue
                    });
                }
            }
        }

        foreach (var prop in semantics.properties)
        {
            if (seenProperties.Add(prop.name))
            {
                allProperties.Add(prop);
            }
        }

        var totalBufferSize = isTemplateShader
            ? (int)DSLShaderCompiler.CalculateRawPropertyBufferSize(allProperties)
            : (int)DSLShaderCompiler.CalculatePropertyBufferSize(allProperties);

        var buffer = new byte[totalBufferSize];
        var fixups = new List<MaterialTextureFixup>();

        var registerIndex = 0;
        var byteOffsetInRegister = 0;
        var rawOffset = 0;

        foreach (var prop in allProperties)
        {
            var offset = isTemplateShader
                ? DSLShaderCompiler.ComputeRawFieldOffset(prop.type, ref rawOffset)
                : DSLShaderCompiler.ComputeFieldOffset(prop.type, ref registerIndex, ref byteOffsetInRegister);

            if (IsTextureType(prop.type))
            {
                var textureGuid = Guid.Empty;
                if (jsonTextures != null && jsonTextures.TryGetValue(prop.name, out var g))
                {
                    textureGuid = g;
                }
                else if (textureFallback != null && textureFallback(prop.name) is { } fallbackGuid)
                {
                    textureGuid = fallbackGuid;
                }

                if (textureGuid != Guid.Empty)
                {
                    fixups.Add(new MaterialTextureFixup
                    {
                        byteOffset = (uint)offset,
                        textureGuid = textureGuid
                    });
                }

                continue;
            }

            object? val = null;
            if (jsonProperties != null && jsonProperties.TryGetValue(prop.name, out var jsonElem))
            {
                val = ParseJsonElement(prop.type, jsonElem);
            }
            else if (valueFallback != null)
            {
                val = valueFallback(prop.name);
            }

            if (val == null && !string.IsNullOrWhiteSpace(prop.defaultValue))
            {
                val = ParseDefaultValueString(prop.type, prop.defaultValue!);
            }

            if (val != null)
            {
                WriteValueToBuffer(buffer, offset, prop.type, val);
            }
        }

        return (buffer, fixups);
    }

    public static bool IsTextureType(string type)
    {
        var span = type.AsSpan().Trim();
        return span.Equals("texture2d", StringComparison.OrdinalIgnoreCase)
            || span.Equals("texture3d", StringComparison.OrdinalIgnoreCase)
            || span.Equals("texturecube", StringComparison.OrdinalIgnoreCase)
            || span.Equals("texture2darray", StringComparison.OrdinalIgnoreCase)
            || span.Equals("texturecubearray", StringComparison.OrdinalIgnoreCase)
            || span.Equals("texture2dhandle", StringComparison.OrdinalIgnoreCase)
            || span.Equals("texture3dhandle", StringComparison.OrdinalIgnoreCase);
    }

    private static object? ParseJsonElement(string hlslType, JsonElement elem)
    {
        var span = hlslType.AsSpan().Trim();
        if (span.Equals("float", StringComparison.OrdinalIgnoreCase)) return elem.GetSingle();
        if (span.Equals("float2", StringComparison.OrdinalIgnoreCase))
            return elem.ValueKind == JsonValueKind.Array && elem.GetArrayLength() >= 2 ? new float2(elem[0].GetSingle(), elem[1].GetSingle()) : null;
        if (span.Equals("float3", StringComparison.OrdinalIgnoreCase))
            return elem.ValueKind == JsonValueKind.Array && elem.GetArrayLength() >= 3 ? new float3(elem[0].GetSingle(), elem[1].GetSingle(), elem[2].GetSingle()) : null;
        if (span.Equals("float4", StringComparison.OrdinalIgnoreCase) || span.Equals("quaternion", StringComparison.OrdinalIgnoreCase))
            return elem.ValueKind == JsonValueKind.Array && elem.GetArrayLength() >= 4 ? new float4(elem[0].GetSingle(), elem[1].GetSingle(), elem[2].GetSingle(), elem[3].GetSingle()) : null;
        if (span.Equals("uint", StringComparison.OrdinalIgnoreCase))
            return elem.ValueKind == JsonValueKind.True ? 1u : elem.ValueKind == JsonValueKind.False ? 0u : elem.GetUInt32();
        if (span.Equals("int", StringComparison.OrdinalIgnoreCase))
            return elem.GetInt32();
        if (span.Equals("bool", StringComparison.OrdinalIgnoreCase))
            return elem.ValueKind == JsonValueKind.True ? 1u : elem.ValueKind == JsonValueKind.False ? 0u : elem.GetUInt32();

        return null;
    }

    private static object? ParseDefaultValueString(string hlslType, string defaultValue)
    {
        var clean = defaultValue.AsSpan().Trim();
        var typeSpan = hlslType.AsSpan().Trim();

        if (clean.StartsWith("float4", StringComparison.OrdinalIgnoreCase) || clean.StartsWith("quaternion", StringComparison.OrdinalIgnoreCase))
        {
            var nums = ExtractFloatArray(clean);
            return nums.Length >= 4 ? new float4(nums[0], nums[1], nums[2], nums[3]) : null;
        }

        if (clean.StartsWith("float3", StringComparison.OrdinalIgnoreCase))
        {
            var nums = ExtractFloatArray(clean);
            return nums.Length >= 3 ? new float3(nums[0], nums[1], nums[2]) : null;
        }

        if (clean.StartsWith("float2", StringComparison.OrdinalIgnoreCase))
        {
            var nums = ExtractFloatArray(clean);
            return nums.Length >= 2 ? new float2(nums[0], nums[1]) : null;
        }

        if (typeSpan.Equals("bool", StringComparison.OrdinalIgnoreCase))
        {
            if (bool.TryParse(clean, out var b)) return b ? 1u : 0u;
            if (uint.TryParse(clean, CultureInfo.InvariantCulture, out var ub)) return ub;
        }

        if (typeSpan.Equals("uint", StringComparison.OrdinalIgnoreCase) && uint.TryParse(clean, CultureInfo.InvariantCulture, out var u))
        {
            return u;
        }

        if (typeSpan.Equals("int", StringComparison.OrdinalIgnoreCase) && int.TryParse(clean, CultureInfo.InvariantCulture, out var i))
        {
            return i;
        }

        if (clean.EndsWith("f", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean[..^1];
        }

        if (float.TryParse(clean, CultureInfo.InvariantCulture, out var f))
        {
            return f;
        }

        return null;
    }

    private static float[] ExtractFloatArray(ReadOnlySpan<char> str)
    {
        var start = str.IndexOf('(');
        var end = str.LastIndexOf(')');
        if (start < 0 || end <= start)
        {
            return Array.Empty<float>();
        }

        var inner = str.Slice(start + 1, end - start - 1);
        var count = 1;
        for (var i = 0; i < inner.Length; i++)
        {
            if (inner[i] == ',') count++;
        }

        var result = new float[count];
        var idx = 0;
        while (inner.Length > 0 && idx < count)
        {
            var comma = inner.IndexOf(',');
            var part = comma >= 0 ? inner.Slice(0, comma).Trim() : inner.Trim();
            if (part.EndsWith("f", StringComparison.OrdinalIgnoreCase))
            {
                part = part[..^1];
            }
            if (float.TryParse(part, CultureInfo.InvariantCulture, out var f))
            {
                result[idx++] = f;
            }
            else
            {
                idx++;
            }

            inner = comma >= 0 ? inner.Slice(comma + 1) : default;
        }

        return result;
    }

    private static unsafe void WriteValueToBuffer(byte[] buffer, int offset, string type, object val)
    {
        fixed (byte* p = buffer)
        {
            var pDst = p + offset;
            switch (val)
            {
                case float4 f4:
                    *(float4*)pDst = f4;
                    break;
                case float3 f3:
                    *(float3*)pDst = f3;
                    break;
                case float2 f2:
                    *(float2*)pDst = f2;
                    break;
                case float f:
                    *(float*)pDst = f;
                    break;
                case uint u:
                    *(uint*)pDst = u;
                    break;
                case int i:
                    *(int*)pDst = i;
                    break;
                case bool b:
                    *(uint*)pDst = b ? 1u : 0u;
                    break;
                default:
                    if (val is IConvertible conv)
                    {
                        var normSpan = type.AsSpan().Trim();
                        if (normSpan.Equals("float", StringComparison.OrdinalIgnoreCase)) *(float*)pDst = conv.ToSingle(CultureInfo.InvariantCulture);
                        else if (normSpan.Equals("uint", StringComparison.OrdinalIgnoreCase) || normSpan.Equals("bool", StringComparison.OrdinalIgnoreCase)) *(uint*)pDst = conv.ToUInt32(CultureInfo.InvariantCulture);
                        else if (normSpan.Equals("int", StringComparison.OrdinalIgnoreCase)) *(int*)pDst = conv.ToInt32(CultureInfo.InvariantCulture);
                    }
                    break;
            }
        }
    }
}
