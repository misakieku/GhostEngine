using Ghost.DSL.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Text.Json;

namespace Ghost.ShaderMetadataTool;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: Ghost.ShaderMetadataTool <input_files.txt> <output.json>");
            return;
        }

        var inputFileList = args[0];
        var outputFile = args[1];

        if (!File.Exists(inputFileList))
        {
            Console.WriteLine($"Input file list not found: {inputFileList}");
            return;
        }

        var files = File.ReadAllLines(inputFileList);
        var extractedData = new Dictionary<string, ShaderReflectionData>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                continue;
            }

            var text = File.ReadAllText(file);

            // Fast text filter to avoid parsing unrelated files
            if (!text.Contains("GenerateShaderProperty"))
            {
                continue;
            }

            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetRoot();

            var structDeclarations = root.DescendantNodes().OfType<StructDeclarationSyntax>();

            foreach (var structDecl in structDeclarations)
            {
                var attribute = structDecl.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .FirstOrDefault(a => a.Name.ToString() == "GenerateShaderProperty" ||
                                         a.Name.ToString().EndsWith(".GenerateShaderPropertyAttribute") ||
                                         a.Name.ToString().EndsWith(".GenerateShaderProperty"));

                if (attribute == null)
                {
                    continue;
                }

                // Extract shader name and struct name from attribute arguments
                var shaderName = "";
                var shaderStructName = structDecl.Identifier.Text;

                if (attribute.ArgumentList != null && attribute.ArgumentList.Arguments.Count >= 1)
                {
                    var argExpr = attribute.ArgumentList.Arguments[0].Expression;
                    if (argExpr is LiteralExpressionSyntax literal)
                    {
                        shaderName = literal.Token.ValueText;
                    }
                }

                if (attribute.ArgumentList != null && attribute.ArgumentList.Arguments.Count >= 2)
                {
                    var argExpr = attribute.ArgumentList.Arguments[1].Expression;
                    if (argExpr is LiteralExpressionSyntax literal)
                    {
                        shaderStructName = literal.Token.ValueText;
                    }
                }

                if (string.IsNullOrEmpty(shaderName))
                {
                    continue;
                }

                var hlslBuilder = new StringBuilder();
                var fields = new List<ShaderPropertyFieldInfo>();

                // Note: We don't have semantic model, so we calculate size / offset sequentially
                // assuming sequential layout and standard alignment.
                // In actual runtime, Size/Offset might need exact match if padding is complex.
                // Assuming Pack=4 for HLSL rules or similar. For simplicity, we just extract types for now.
                // Actually, if AssetForge just needs the HLSL code and type data, exact offset can be generated 
                // in AssetForge when laying out the buffer, or the runtime uses its own sizeof().

                // Let's mimic the Source Generator logic
                var currentOffset = 0;

                foreach (var member in structDecl.Members)
                {
                    if (member is FieldDeclarationSyntax fieldDecl)
                    {
                        // Check if static or const
                        if (fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword)))
                        {
                            continue;
                        }

                        // Try to get GenerateAsHLSLTypeAttribute
                        var hlslAttr = fieldDecl.AttributeLists
                            .SelectMany(al => al.Attributes)
                            .FirstOrDefault(a => a.Name.ToString() == "GenerateAsHLSLType" ||
                                                 a.Name.ToString().EndsWith(".GenerateAsHLSLTypeAttribute") ||
                                                 a.Name.ToString().EndsWith(".GenerateAsHLSLType"));

                        string? hlslType = null;
                        var shaderPropType = "Unknown";
                        var typeSize = 4; // default size

                        if (hlslAttr != null && hlslAttr.ArgumentList != null && hlslAttr.ArgumentList.Arguments.Count > 0)
                        {
                            var argExpr = hlslAttr.ArgumentList.Arguments[0].Expression;
                            if (argExpr is LiteralExpressionSyntax literal)
                            {
                                hlslType = literal.Token.ValueText;
                            }
                        }

                        var typeSyntax = fieldDecl.Declaration.Type.ToString();

                        if (hlslType == null)
                        {
                            switch (typeSyntax)
                            {
                                case "float":
                                case "System.Single":
                                    hlslType = "float"; shaderPropType = "Float"; typeSize = 4; break;
                                case "int":
                                case "System.Int32":
                                    hlslType = "int"; shaderPropType = "Int"; typeSize = 4; break;
                                case "uint":
                                case "System.UInt32":
                                    hlslType = "uint"; shaderPropType = "UInt"; typeSize = 4; break;
                                case "Vector2":
                                case "float2":
                                case "Misaki.HighPerformance.Mathematics.float2":
                                    hlslType = "float2"; shaderPropType = "Float2"; typeSize = 8; break;
                                case "Vector3":
                                case "float3":
                                case "Misaki.HighPerformance.Mathematics.float3":
                                    hlslType = "float3"; shaderPropType = "Float3"; typeSize = 12; break;
                                case "Vector4":
                                case "float4":
                                case "Misaki.HighPerformance.Mathematics.float4":
                                case "Quaternion":
                                case "quaternion":
                                case "Misaki.HighPerformance.Mathematics.quaternion":
                                    hlslType = "float4"; shaderPropType = "Float4"; typeSize = 16; break;
                                case "Matrix4x4":
                                case "float4x4":
                                case "Misaki.HighPerformance.Mathematics.float4x4":
                                    hlslType = "float4x4"; shaderPropType = "Float4x4"; typeSize = 64; break;
                                case "Texture2DHandle":
                                case "Ghost.Core.Graphics.Texture2DHandle":
                                    hlslType = "uint"; shaderPropType = "Texture2D"; typeSize = 4; break;
                                case "Texture3DHandle":
                                case "Ghost.Core.Graphics.Texture3DHandle":
                                    hlslType = "uint"; shaderPropType = "Texture3D"; typeSize = 4; break;
                                case "BufferHandle":
                                case "Ghost.Core.Graphics.BufferHandle":
                                    hlslType = "uint"; shaderPropType = "Buffer"; typeSize = 4; break;
                                default:
                                    if (typeSyntax.StartsWith("float") && typeSyntax.Length <= 7) // floatNxM
                                    {
                                        hlslType = typeSyntax;
                                    }

                                    break;
                            }
                        }

                        if (hlslType == null)
                        {
                            continue; // unsupported type
                        }

                        foreach (var variable in fieldDecl.Declaration.Variables)
                        {
                            var fieldName = variable.Identifier.Text;
                            hlslBuilder.AppendLine($"    {hlslType} {fieldName};");

                            fields.Add(new ShaderPropertyFieldInfo
                            {
                                Name = fieldName,
                                Type = shaderPropType,
                                Offset = currentOffset
                            });

                            currentOffset += typeSize;

                            // Align next offset to 4 bytes if needed
                            if (currentOffset % 4 != 0)
                            {
                                currentOffset += 4 - (currentOffset % 4);
                            }
                        }
                    }
                }

                var code = $"struct {shaderStructName}\n{{\n{hlslBuilder}}};";

                extractedData[shaderName] = new ShaderReflectionData
                {
                    ShaderName = shaderName,
                    Code = code,
                    Size = (uint)currentOffset,
                    Fields = fields.ToArray()
                };
            }
        }

        var json = JsonSerializer.Serialize(extractedData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(outputFile, json);
        Console.WriteLine($"Extracted {extractedData.Count} shader properties to {outputFile}");
    }
}
