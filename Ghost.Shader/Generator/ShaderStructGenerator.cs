using Misaki.HighPerformance.Mathematics;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Ghost.SDL.Generator;

public enum PackingRules
{
    Exact,
    Aligned,
}

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum)]
public class GenerateHLSLAttribute : Attribute
{
    private readonly PackingRules _packingRules;
    private readonly string? _outputSource;

    public GenerateHLSLAttribute(PackingRules packingRules, string? outputSource)
    {
        _packingRules = packingRules;
        _outputSource = outputSource;
    }
}

internal static partial class ShaderStructGenerator
{
    private struct ShaderFieldInfo
    {
        public string name;
        public Type fieldType;

        public ShaderFieldInfo(string name, Type fieldType)
        {
            this.name = name;
            this.fieldType = fieldType;
        }

        public ShaderFieldInfo(FieldInfo fieldInfo)
            : this(fieldInfo.Name, fieldInfo.FieldType)
        {
        }
    }

    private const int _HLSL_VECTOR_REGISTER_SIZE = 16; // 16 bytes (128 bits) for float4

    private static void GenerateEnumHLSL(Type type, StringBuilder sb)
    {
        if (!type.IsEnum)
        {
            throw new InvalidOperationException($"Type {type.FullName} is not an enum.");
        }

        var enumName = type.Name;
        //var underlyingType = Enum.GetUnderlyingType(space);
        //var underlyingTypeName = underlyingType switch
        //{
        //    Type t when t == typeof(byte) || t == typeof(short) || t == typeof(int) => "int",
        //    Type t when t == typeof(sbyte) || t == typeof(ushort) || t == typeof(uint) => "uint",
        //    _ => throw new InvalidOperationException($"Unsupported underlying space {underlyingType.FullName} for enum {enumName}."),
        //};

        //        sb.Append(@$"
        //enum {enumName} : {underlyingTypeName}
        //{{");
        var names = Enum.GetNames(type);
        var values = Enum.GetValuesAsUnderlyingType(type);
        for (var i = 0; i < names.Length; i++)
        {
            var name = $"{CamelCaseToUnderscoreRegex().Replace(enumName, "_$1")}_{names[i]}";
            var value = values.GetValue(i);
            //        sb.Append(@$"
            //{name} = {Value},");
            sb.Append(@$"
#define {name.ToUpperInvariant()} {value}"); // Use #define for capability. Enum is only support for newer HLSL versions.
        }
        //        sb.AppendLine(@"
        //};");

        sb.AppendLine();
    }

    public static int FindNextFieldThatFits(FieldInfo[] fields, bool[] looked, int startIndex, int size, out int foundIndex)
    {
        if (size <= 0)
        {
            foundIndex = -1;
            return size;
        }

        var bestFitIndex = -1;
        var bestFitSize = 0;

        for (var j = startIndex; j < fields.Length; j++)
        {
            if (looked[j])
            {
                continue;
            }

            var nextField = fields[j];
            var nextSize = Marshal.SizeOf(nextField.FieldType);
            if (nextSize <= size)
            {
                if (nextSize == size)
                {
                    foundIndex = j;
                    return nextSize;
                }

                if (nextSize > bestFitSize)
                {
                    bestFitSize = nextSize;
                    bestFitIndex = j;
                }
            }
        }

        if (bestFitIndex != -1)
        {
            foundIndex = bestFitIndex;
            return bestFitSize;
        }

        foundIndex = -1;
        return size;
    }

    private static void GenerateStructHLSL(Type type, PackingRules packingRules, StringBuilder sb)
    {
        if (!type.IsValueType || type.IsPrimitive)
        {
            throw new InvalidOperationException($"Type {type.FullName} is not a struct.");
        }

        var structName = type.Name;
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(static f => f.FieldType.IsValueType).ToArray();

        var shaderFields = new ShaderFieldInfo[fields.Length];
        if (packingRules == PackingRules.Aligned)
        {
            var sortedFields = new List<ShaderFieldInfo>(fields.Length);
            var looked = new bool[fields.Length];
            var paddingIndex = 0;

            // Sort the fields to align them to HLSL vector registers (16 bytes)
            for (var i = 0; i < fields.Length; i++)
            {
                if (looked[i])
                {
                    continue;
                }

                var field = fields[i];
                var size = Marshal.SizeOf(field.FieldType);

                sortedFields.Add(new ShaderFieldInfo(field));

                var registerRemaining = _HLSL_VECTOR_REGISTER_SIZE - (size % _HLSL_VECTOR_REGISTER_SIZE);
                while (true)
                {
                    var nextSize = FindNextFieldThatFits(fields, looked, i + 1, registerRemaining, out var nextIndex);
                    if (nextSize == 0 || nextIndex == -1)
                    {
                        break;
                    }

                    looked[i] = true;
                    looked[nextIndex] = true;

                    sortedFields.Add(new ShaderFieldInfo(fields[nextIndex]));

                    registerRemaining -= nextSize;
                }

                if (registerRemaining != 0)
                {
                    // Add padding if necessary
                    var count = registerRemaining / sizeof(float);
                    for (var p = 0; p < count; p++)
                    {
                        sortedFields.Add(new ShaderFieldInfo($"_padding{paddingIndex++}", typeof(float)));
                    }
                }
            }

            shaderFields = sortedFields.ToArray();
        }
        else
        {
            for (var i = 0; i < fields.Length; i++)
            {
                shaderFields[i] = new ShaderFieldInfo(fields[i]);
            }
        }

        sb.Append(@$"
struct {structName}
{{");
        foreach (var field in shaderFields)
        {
            var fieldType = field.fieldType;
            var fieldName = field.name;

            string hlslType;
            switch (fieldType)
            {
                case Type t when t == typeof(float):
                    hlslType = "float";
                    break;
                case Type t when t == typeof(double):
                    hlslType = "double";
                    break;
                case Type t when t == typeof(int):
                    hlslType = "int";
                    break;
                case Type t when t == typeof(uint):
                    hlslType = "uint";
                    break;
                case Type t when t == typeof(bool):
                    hlslType = "bool";
                    break;
                case Type t when t == typeof(Vector2):
                    hlslType = "float2";
                    break;
                case Type t when t == typeof(Vector3):
                    hlslType = "float3";
                    break;
                case Type t when t == typeof(Vector4):
                    hlslType = "float4";
                    break;
                case Type t when t == typeof(Matrix4x4):
                    hlslType = "float4x4";
                    break;
                default:
                {
                    if (fieldType.Namespace == typeof(float2).Namespace)
                    {
                        if (fieldType.Name.StartsWith("float")
                            || fieldType.Name.StartsWith("double")
                            || fieldType.Name.StartsWith("int")
                            || fieldType.Name.StartsWith("uint")
                            || fieldType.Name.StartsWith("bool"))
                        {
                            hlslType = fieldType.Name;
                            break;
                        }
                    }

                    throw new InvalidOperationException($"Unsupported field type: {fieldType.FullName} in struct {structName}.");
                }
            }

            sb.Append(@$"
    {hlslType} {fieldName};");
        }

        sb.AppendLine(@"
};");
    }

    public static void GenerateHLSL(ReadOnlySpan<Type> types, PackingRules packingRules, string outputSource)
    {
        if (!Directory.Exists(Path.GetDirectoryName(outputSource)))
        {
            throw new DirectoryNotFoundException($"The directory for the output source '{outputSource}' does not exist.");
        }

        var hlslDefine = $"{Path.GetFileNameWithoutExtension(outputSource).ToUpperInvariant().Replace('.', '_')}_HLSL";

        var sb = new StringBuilder();
        sb.AppendLine(@$"// Auto-generated HLSL code, please do not edit this file directly.

#ifndef {hlslDefine}
#define {hlslDefine}");

        foreach (var type in types)
        {
            if (type.IsEnum)
            {
                GenerateEnumHLSL(type, sb);
            }
            else if (type.IsValueType && !type.IsPrimitive)
            {
                GenerateStructHLSL(type, packingRules, sb);
            }
            else
            {
                continue;
            }
        }
        sb.Append(@"
#endif");

        var hlslCode = sb.ToString();
        File.WriteAllText(outputSource, hlslCode);
    }

    [GeneratedRegex("(?<=[a-z])([A-Z])")]
    private static partial Regex CamelCaseToUnderscoreRegex();
}