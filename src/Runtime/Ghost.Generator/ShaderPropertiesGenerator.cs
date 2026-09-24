using Ghost.Generator.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ghost.Generator
{
    [Generator]
    internal class ShaderPropertiesGenerator : IIncrementalGenerator
    {
        private sealed class PropertyModel
        {
            public string type = string.Empty;
            public string name = string.Empty;
            public string defaultValue = string.Empty;
            public bool isFromTemplate;
        }

        private sealed class ParsedShader
        {
            public string shaderName = string.Empty;
            public string templateName = string.Empty;
            public List<PropertyModel> properties = new List<PropertyModel>();
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var shaderFiles = context.AdditionalTextsProvider
                .Where(file => file.Path.EndsWith(".gshdr", StringComparison.OrdinalIgnoreCase) ||
                               file.Path.EndsWith(".gcomp", StringComparison.OrdinalIgnoreCase) ||
                               file.Path.EndsWith(".ggraph", StringComparison.OrdinalIgnoreCase))
                .Select((text, ct) =>
                {
                    var content = text.GetText(ct)?.ToString();
                    if (string.IsNullOrEmpty(content))
                    {
                        return null;
                    }

                    return ParseShaderProperties(content!);
                })
                .Where(x => x != null)
                .Collect();

            context.RegisterSourceOutput(shaderFiles, GenerateShaderPropertyStructs);
        }

        private static ParsedShader? ParseShaderProperties(string content)
        {
            var cleanText = Regex.Replace(content, @"//.*?$|/\*.*?\*/", "", RegexOptions.Multiline | RegexOptions.Singleline);

            var shaderDeclMatch = Regex.Match(cleanText, @"(?:shader|compute)\s+""([^""]+)""(?:\s*:\s*""([^""]+)"")?");
            if (!shaderDeclMatch.Success)
            {
                return null;
            }

            var shader = new ParsedShader
            {
                shaderName = shaderDeclMatch.Groups[1].Value,
                templateName = shaderDeclMatch.Groups[2].Success ? shaderDeclMatch.Groups[2].Value : string.Empty
            };

            var propertiesMatch = Regex.Match(cleanText, @"properties\s*\{([^}]*)\}", RegexOptions.Singleline);
            if (propertiesMatch.Success)
            {
                var propsBlock = propertiesMatch.Groups[1].Value;
                var propStatements = Regex.Matches(propsBlock, @"([a-zA-Z0-9_]+)\s+([a-zA-Z0-9_]+)(?:\s*=\s*([^;]+))?\s*;");
                foreach (Match stmt in propStatements)
                {
                    shader.properties.Add(new PropertyModel
                    {
                        type = stmt.Groups[1].Value,
                        name = stmt.Groups[2].Value,
                        defaultValue = stmt.Groups[3].Success ? stmt.Groups[3].Value.Trim() : string.Empty,
                        isFromTemplate = false
                    });
                }
            }

            return shader;
        }

        private static void GenerateShaderPropertyStructs(SourceProductionContext context, ImmutableArray<ParsedShader?> shaders)
        {
            if (shaders.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var shader in shaders)
            {
                if (shader == null || string.IsNullOrEmpty(shader.shaderName))
                {
                    continue;
                }

                var structName = SanitizeToIdentifier(shader.shaderName);
                var allProperties = new List<PropertyModel>();

                // 1. Template base properties from BuiltInTemplateProperties
                if (!string.IsNullOrEmpty(shader.templateName) &&
                    BuiltInTemplateProperties.TryGetBaseProperties(shader.templateName, out var baseProps))
                {
                    foreach (var prop in baseProps)
                    {
                        allProperties.Add(new PropertyModel
                        {
                            type = prop.type,
                            name = prop.name,
                            defaultValue = prop.defaultValue ?? string.Empty,
                            isFromTemplate = true
                        });
                    }
                }

                // 2. Custom properties declared in shader (deduplicating against template base properties)
                foreach (var prop in shader.properties)
                {
                    var isDuplicate = allProperties.Any(p => string.Equals(p.name, prop.name, StringComparison.OrdinalIgnoreCase));
                    if (!isDuplicate)
                    {
                        allProperties.Add(prop);
                    }
                }

                if (allProperties.Count == 0 && string.IsNullOrEmpty(shader.templateName))
                {
                    continue;
                }

                // 3. Generate field code with initializers
                var fieldsSb = new StringBuilder();
                var inCustomSection = false;

                for (var i = 0; i < allProperties.Count; i++)
                {
                    var prop = allProperties[i];
                    var csType = MapHlslTypeToCSharp(prop.type);

                    if (i == 0 && prop.isFromTemplate)
                    {
                        fieldsSb.AppendLine($"        // --- Base properties from template: {shader.templateName} ---");
                    }
                    else if (!prop.isFromTemplate && !inCustomSection)
                    {
                        inCustomSection = true;
                        if (i > 0)
                        {
                            fieldsSb.AppendLine();
                        }
                        fieldsSb.AppendLine($"        // --- Custom properties from shader: {shader.shaderName} ---");
                    }

                    var defVal = FormatDefaultValueToCSharp(prop.type, csType, prop.defaultValue);
                    if (!string.IsNullOrEmpty(defVal))
                    {
                        fieldsSb.AppendLine($"        public {csType} {prop.name} = {defVal};");
                    }
                    else
                    {
                        fieldsSb.AppendLine($"        public {csType} {prop.name};");
                    }
                }

                var fieldsCode = fieldsSb.ToString().TrimEnd();

                var templateConstCode = string.IsNullOrEmpty(shader.templateName)
                    ? string.Empty
                    : $"\n        public const string TEMPLATE_NAME = \"{shader.templateName}\";";

                // 4. Emit formatted C# struct using multiline raw string literal
                var code = $$"""
// <auto-generated/>
#nullable enable
using System.Runtime.InteropServices;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.ShaderProperties
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public partial struct {{structName}}()
    {
{{fieldsCode}}

        public const string SHADER_NAME = "{{shader.shaderName}}";{{templateConstCode}}
    }
}
""";

                context.AddSource($"{structName}.g.cs", SourceText.From(code, Encoding.UTF8));
            }
        }

        private static string MapHlslTypeToCSharp(string hlslType)
        {
            return hlslType.Trim().ToLowerInvariant() switch
            {
                "float" => "float",
                "float2" => "float2",
                "float3" => "float3",
                "float4" => "float4",

                "int" => "int",
                "int2" => "int2",
                "int3" => "int3",
                "int4" => "int4",

                "uint" => "uint",
                "uint2" => "uint2",
                "uint3" => "uint3",
                "uint4" => "uint4",

                "bool" => "uint",
                "bool2" => "uint2",
                "bool3" => "uint3",
                "bool4" => "uint4",

                "float2x2" => "float2x2",
                "float3x3" => "float3x3",
                "float4x4" or "matrix4x4" => "float4x4",
                "float4x3" => "float4x3",
                "float3x4" => "float3x4",
                "int2x4" => "int2x4",

                "texture2d" or "texture3d" or "texturecube" or "texture2darray" or "texturecubearray"
                    or "samplerstate" or "sampler" or "byte_address_buffer" or "struct_buffer" or "structured_buffer"
                    or "texture2dhandle" or "texture3dhandle" or "bufferhandle" => "uint",

                _ => hlslType,
            };
        }

        private static string FormatDefaultValueToCSharp(string hlslType, string csType, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(defaultValue))
            {
                return string.Empty;
            }

            defaultValue = defaultValue.Trim();
            var hlslLower = hlslType.Trim().ToLowerInvariant();

            if (hlslLower == "bool")
            {
                if (bool.TryParse(defaultValue, out var bVal))
                {
                    return bVal ? "1u" : "0u";
                }
                return defaultValue.Equals("1") ? "1u" : "0u";
            }

            switch (csType)
            {
                case "uint":
                    if (defaultValue.EndsWith("u", StringComparison.OrdinalIgnoreCase))
                    {
                        return defaultValue;
                    }
                    return defaultValue + "u";

                case "int":
                    return defaultValue;

                case "float":
                    if (!defaultValue.EndsWith("f", StringComparison.OrdinalIgnoreCase) &&
                        !defaultValue.EndsWith("F", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!defaultValue.Contains("."))
                        {
                            return defaultValue + ".0f";
                        }
                        return defaultValue + "f";
                    }
                    return defaultValue;

                case "float2" or "float3" or "float4" or "uint2" or "uint3" or "uint4" or "int2" or "int3" or "int4":
                    var parenStart = defaultValue.IndexOf('(');
                    var parenEnd = defaultValue.LastIndexOf(')');
                    if (parenStart >= 0 && parenEnd > parenStart)
                    {
                        var innerArgs = defaultValue.Substring(parenStart + 1, parenEnd - parenStart - 1);
                        var args = innerArgs.Split(',');
                        var formattedArgs = args.Select(a =>
                        {
                            a = a.Trim();
                            if (csType.StartsWith("float") &&
                                !a.EndsWith("f", StringComparison.OrdinalIgnoreCase) &&
                                !a.EndsWith("F", StringComparison.OrdinalIgnoreCase))
                            {
                                return a.Contains(".") ? a + "f" : a + ".0f";
                            }
                            if (csType.StartsWith("uint") && !a.EndsWith("u", StringComparison.OrdinalIgnoreCase))
                            {
                                return a + "u";
                            }
                            return a;
                        });
                        return $"new {csType}({string.Join(", ", formattedArgs)})";
                    }
                    return $"new {csType}()";

                default:
                    return defaultValue;
            }
        }

        private static string SanitizeToIdentifier(string shaderName)
        {
            var parts = shaderName.Split(new[] { '/', '\\', '.', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    sb.Append(char.ToUpperInvariant(part[0]));
                    if (part.Length > 1)
                    {
                        sb.Append(part.Substring(1));
                    }
                }
            }

            var result = sb.ToString();
            if (!result.EndsWith("ShaderProperties", StringComparison.OrdinalIgnoreCase) &&
                !result.EndsWith("Properties", StringComparison.OrdinalIgnoreCase))
            {
                result += "ShaderProperties";
            }

            return result;
        }
    }
}
