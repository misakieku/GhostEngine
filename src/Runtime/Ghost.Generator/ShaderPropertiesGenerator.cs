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
        private struct ParsedProperty
        {
            public string type;
            public string name;
            public string defaultValue;
        }

        private class ParsedShader
        {
            public string shaderName = string.Empty;
            public string templateName = string.Empty;
            public List<ParsedProperty> properties = new List<ParsedProperty>();
        }

        private static readonly Dictionary<string, ParsedProperty[]> s_templateProperties =
            new Dictionary<string, ParsedProperty[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Lit"] = new[]
                {
                    new ParsedProperty { type = "bool", name = "alphaClip" },
                    new ParsedProperty { type = "float", name = "alphaClipThreshold" },
                },
                ["Unlit"] = new[]
                {
                    new ParsedProperty { type = "bool", name = "alphaClip" },
                    new ParsedProperty { type = "float", name = "alphaClipThreshold" },
                },
                ["Sky"] = new[]
                {
                    new ParsedProperty { type = "float4", name = "skyTint" },
                    new ParsedProperty { type = "float", name = "exposure" },
                },
                ["UI"] = new[]
                {
                    new ParsedProperty { type = "float4", name = "color" },
                    new ParsedProperty { type = "uint", name = "mainTex" },
                }
            };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Watch all .gshdr, .gcomp, and .ggraph AdditionalFiles
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
            // Strip comments
            var cleanText = Regex.Replace(content, @"//.*?$|/\*.*?\*/", "", RegexOptions.Multiline | RegexOptions.Singleline);

            // Match shader or compute declaration: (shader|compute) "Name" (: "Template")? {
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

            // Match properties { ... }
            var propertiesMatch = Regex.Match(cleanText, @"properties\s*\{([^}]*)\}", RegexOptions.Singleline);
            if (propertiesMatch.Success)
            {
                var propsBlock = propertiesMatch.Groups[1].Value;
                // Match each property statement: type name (= default)?;
                var propStatements = Regex.Matches(propsBlock, @"([a-zA-Z0-9_]+)\s+([a-zA-Z0-9_]+)(?:\s*=\s*([^;]+))?\s*;");
                foreach (Match stmt in propStatements)
                {
                    shader.properties.Add(new ParsedProperty
                    {
                        type = stmt.Groups[1].Value,
                        name = stmt.Groups[2].Value,
                        defaultValue = stmt.Groups[3].Success ? stmt.Groups[3].Value.Trim() : string.Empty
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
                var allProperties = new List<ParsedProperty>();

                // 1. Injected base properties from template
                if (!string.IsNullOrEmpty(shader.templateName) && s_templateProperties.TryGetValue(shader.templateName, out var baseProps))
                {
                    allProperties.AddRange(baseProps);
                }

                // 2. Custom properties declared in .gshdr (excluding any duplicate base property names)
                foreach (var prop in shader.properties)
                {
                    var isDuplicate = allProperties.Any(p => string.Equals(p.name, prop.name, StringComparison.OrdinalIgnoreCase));
                    if (!isDuplicate)
                    {
                        allProperties.Add(prop);
                    }
                }

                // If no properties at all, omit generation
                if (allProperties.Count == 0 && string.IsNullOrEmpty(shader.templateName))
                {
                    continue;
                }

                var sb = new StringBuilder();
                sb.AppendLine("// <auto-generated/>");
                sb.AppendLine("#nullable enable");
                sb.AppendLine("using System.Runtime.InteropServices;");
                sb.AppendLine("using Misaki.HighPerformance.Mathematics;");
                sb.AppendLine();
                sb.AppendLine("namespace Ghost.Engine.ShaderProperties");
                sb.AppendLine("{");
                sb.AppendLine("    [StructLayout(LayoutKind.Sequential, Pack = 4)]");
                sb.AppendLine($"    public partial struct {structName}");
                sb.AppendLine("    {");

                var hasTemplate = !string.IsNullOrEmpty(shader.templateName) && s_templateProperties.ContainsKey(shader.templateName);
                var basePropCount = hasTemplate ? s_templateProperties[shader.templateName].Length : 0;

                for (var i = 0; i < allProperties.Count; i++)
                {
                    var prop = allProperties[i];
                    var csType = MapHlslTypeToCSharp(prop.type);

                    if (i == 0 && basePropCount > 0)
                    {
                        sb.AppendLine($"        // --- Base properties from template: {shader.templateName} ---");
                    }
                    else if (i == basePropCount && allProperties.Count > basePropCount)
                    {
                        sb.AppendLine($"        // --- Custom properties from shader: {shader.shaderName} ---");
                    }

                    sb.AppendLine($"        public {csType} {prop.name};");
                }

                sb.AppendLine();
                sb.AppendLine($"        public const string SHADER_NAME = \"{shader.shaderName}\";");
                if (!string.IsNullOrEmpty(shader.templateName))
                {
                    sb.AppendLine($"        public const string TEMPLATE_NAME = \"{shader.templateName}\";");
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");

                context.AddSource($"{structName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
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
                "float2x2" => "float2x2",
                "float3x3" => "float3x3",
                "float4x4" => "float4x4",
                "float4x3" => "float4x3",
                "float3x4" => "float3x4",
                "int" => "int",
                "int2" => "int2",
                "int3" => "int3",
                "int4" => "int4",
                "int2x4" => "int2x4",
                "uint" => "uint",
                "uint2" => "uint2",
                "uint3" => "uint3",
                "uint4" => "uint4",
                "bool" => "uint",
                "bool2" => "uint2",
                "bool3" => "uint3",
                "bool4" => "uint4",
                "texture2d" or "texture3d" or "texturecube" or "texture2darray" or "texturecubearray" or "samplerstate" or "sampler" or "byte_address_buffer" or "struct_buffer" => "uint",
                _ => hlslType,
            };
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
