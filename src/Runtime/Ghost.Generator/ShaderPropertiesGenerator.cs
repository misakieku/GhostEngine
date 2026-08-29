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
            public string Type;
            public string Name;
            public string DefaultValue;
        }

        private class ParsedShader
        {
            public string ShaderName = string.Empty;
            public string TemplateName = string.Empty;
            public List<ParsedProperty> Properties = new List<ParsedProperty>();
        }

        private static readonly Dictionary<string, ParsedProperty[]> s_templateProperties =
            new Dictionary<string, ParsedProperty[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Lit"] = new[]
                {
                    new ParsedProperty { Type = "float4", Name = "baseColor" },
                    new ParsedProperty { Type = "uint", Name = "baseMap" },
                    new ParsedProperty { Type = "uint", Name = "sampler_baseMap" },
                    new ParsedProperty { Type = "uint", Name = "normalMap" },
                    new ParsedProperty { Type = "uint", Name = "sampler_normalMap" },
                    new ParsedProperty { Type = "float", Name = "metallic" },
                    new ParsedProperty { Type = "float", Name = "roughness" },
                    new ParsedProperty { Type = "float", Name = "occlusion" },
                },
                ["Unlit"] = new[]
                {
                    new ParsedProperty { Type = "float4", Name = "baseColor" },
                    new ParsedProperty { Type = "uint", Name = "baseMap" },
                    new ParsedProperty { Type = "uint", Name = "sampler_baseMap" },
                },
                ["Sky"] = new[]
                {
                    new ParsedProperty { Type = "float4", Name = "skyTint" },
                    new ParsedProperty { Type = "float", Name = "exposure" },
                },
                ["UI"] = new[]
                {
                    new ParsedProperty { Type = "float4", Name = "color" },
                    new ParsedProperty { Type = "uint", Name = "mainTex" },
                }
            };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Watch all .gshdr AdditionalFiles
            var shaderFiles = context.AdditionalTextsProvider
                .Where(file => file.Path.EndsWith(".gshdr", StringComparison.OrdinalIgnoreCase))
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

            // Match shader declaration: shader "Name" (: "Template")? {
            var shaderDeclMatch = Regex.Match(cleanText, @"shader\s+""([^""]+)""(?:\s*:\s*""([^""]+)"")?");
            if (!shaderDeclMatch.Success)
            {
                return null;
            }

            var shader = new ParsedShader
            {
                ShaderName = shaderDeclMatch.Groups[1].Value,
                TemplateName = shaderDeclMatch.Groups[2].Success ? shaderDeclMatch.Groups[2].Value : string.Empty
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
                    shader.Properties.Add(new ParsedProperty
                    {
                        Type = stmt.Groups[1].Value,
                        Name = stmt.Groups[2].Value,
                        DefaultValue = stmt.Groups[3].Success ? stmt.Groups[3].Value.Trim() : string.Empty
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
                if (shader == null || string.IsNullOrEmpty(shader.ShaderName))
                {
                    continue;
                }

                var structName = SanitizeToIdentifier(shader.ShaderName);
                var allProperties = new List<ParsedProperty>();

                // 1. Injected base properties from template
                if (!string.IsNullOrEmpty(shader.TemplateName) && s_templateProperties.TryGetValue(shader.TemplateName, out var baseProps))
                {
                    allProperties.AddRange(baseProps);
                }

                // 2. Custom properties declared in .gshdr (excluding any duplicate base property names)
                foreach (var prop in shader.Properties)
                {
                    var isDuplicate = allProperties.Any(p => string.Equals(p.Name, prop.Name, StringComparison.OrdinalIgnoreCase));
                    if (!isDuplicate)
                    {
                        allProperties.Add(prop);
                    }
                }

                // If no properties at all, omit generation
                if (allProperties.Count == 0 && string.IsNullOrEmpty(shader.TemplateName))
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

                var hasTemplate = !string.IsNullOrEmpty(shader.TemplateName) && s_templateProperties.ContainsKey(shader.TemplateName);
                var basePropCount = hasTemplate ? s_templateProperties[shader.TemplateName].Length : 0;

                for (var i = 0; i < allProperties.Count; i++)
                {
                    var prop = allProperties[i];
                    var csType = MapHlslTypeToCSharp(prop.Type);

                    if (i == 0 && basePropCount > 0)
                    {
                        sb.AppendLine($"        // --- Base properties from template: {shader.TemplateName} ---");
                    }
                    else if (i == basePropCount && allProperties.Count > basePropCount)
                    {
                        sb.AppendLine($"        // --- Custom properties from shader: {shader.ShaderName} ---");
                    }

                    sb.AppendLine($"        public {csType} {prop.Name};");
                }

                sb.AppendLine();
                sb.AppendLine($"        public const string SHADER_NAME = \"{shader.ShaderName}\";");
                if (!string.IsNullOrEmpty(shader.TemplateName))
                {
                    sb.AppendLine($"        public const string TEMPLATE_NAME = \"{shader.TemplateName}\";");
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");

                context.AddSource($"{structName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        }

        private static string MapHlslTypeToCSharp(string hlslType)
        {
            switch (hlslType.Trim().ToLowerInvariant())
            {
                case "float": return "float";
                case "float2": return "float2";
                case "float3": return "float3";
                case "float4": return "float4";
                case "float2x2": return "float2x2";
                case "float3x3": return "float3x3";
                case "float4x4": return "float4x4";
                case "float4x3": return "float4x3";
                case "float3x4": return "float3x4";
                case "int": return "int";
                case "int2": return "int2";
                case "int3": return "int3";
                case "int4": return "int4";
                case "int2x4": return "int2x4";
                case "uint": return "uint";
                case "uint2": return "uint2";
                case "uint3": return "uint3";
                case "uint4": return "uint4";
                case "bool": return "bool";
                case "bool2": return "bool2";
                case "bool3": return "bool3";
                case "bool4": return "bool4";
                case "texture2d":
                case "texture3d":
                case "texturecube":
                case "texture2darray":
                case "texturecubearray":
                case "samplerstate":
                case "sampler":
                case "byte_address_buffer":
                case "struct_buffer":
                    return "uint";
                default:
                    return hlslType;
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
