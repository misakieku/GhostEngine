using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Text.RegularExpressions;

namespace Ghost.ShaderMetadataTool;

internal static partial class Utility
{
    private struct ShaderFieldInfo
    {
        public string Name;
        public string CSharpType;
        public string HLSLType;
        public int ByteSize;

        public ShaderFieldInfo(string name, string csharpType, string hlslType, int byteSize)
        {
            Name = name;
            CSharpType = csharpType;
            HLSLType = hlslType;
            ByteSize = byteSize;
        }
    }

    private const int HLSL_VECTOR_REGISTER_SIZE = 16; // 16 bytes (128 bits) for float4

    private static bool GetHLSLTypeAndSize(string csharpType, out string hlslType, out int size)
    {
        switch (csharpType)
        {
            case "float": hlslType = "float"; size = 4; return true;
            case "double": hlslType = "double"; size = 8; return true;
            case "int": hlslType = "int"; size = 4; return true;
            case "uint": hlslType = "uint"; size = 4; return true;
            case "bool": hlslType = "bool"; size = 4; return true;
            case "Vector2":
            case "float2": hlslType = "float2"; size = 8; return true;
            case "Vector3":
            case "float3": hlslType = "float3"; size = 12; return true;
            case "Vector4":
            case "float4": hlslType = "float4"; size = 16; return true;
            case "Matrix4x4":
            case "float4x4": hlslType = "float4x4"; size = 64; return true;
            case "uint2": hlslType = "uint2"; size = 8; return true;
            case "uint3": hlslType = "uint3"; size = 12; return true;
            case "uint4": hlslType = "uint4"; size = 16; return true;
            case "int2": hlslType = "int2"; size = 8; return true;
            case "int3": hlslType = "int3"; size = 12; return true;
            case "int4": hlslType = "int4"; size = 16; return true;
            default:
                hlslType = csharpType;
                size = 0;
                return false;
        }
    }

    private static void GenerateEnumHLSL(EnumDeclarationSyntax type, StringBuilder sb)
    {
        var enumName = type.Identifier.Text;
        var currentValue = 0;

        foreach (var member in type.Members)
        {
            var memberName = member.Identifier.Text;

            if (member.EqualsValue != null)
            {
                var valueSyntax = member.EqualsValue.Value;
                if (valueSyntax is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    if (int.TryParse(literal.Token.ValueText, out var parsedVal))
                    {
                        currentValue = parsedVal;
                    }
                }
            }

            var name = $"{CamelCaseToUnderscoreRegex().Replace(enumName, "_$1")}_{memberName}";

            string valueStr;
            if (member.EqualsValue != null)
            {
                valueStr = member.EqualsValue.Value.ToString();
            }
            else
            {
                valueStr = currentValue.ToString();
                currentValue++;
            }

            sb.Append(@$"
#define {name.ToUpperInvariant()} {valueStr}"); // Use #define for capability. Enum is only support for newer HLSL versions.
        }

        sb.AppendLine();
    }

    private static int FindNextFieldThatFits(ShaderFieldInfo[] fields, bool[] looked, int startIndex, int size, out int foundIndex)
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
            var nextSize = nextField.ByteSize;

            if (nextSize == 0) continue; // Skip unknown sizes

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

    private static void GenerateStructHLSL(StructDeclarationSyntax type, PackingRules packingRules, StringBuilder sb)
    {
        var structName = type.Identifier.Text;
        var fieldDecls = type.Members.OfType<FieldDeclarationSyntax>().ToList();

        var fieldsList = new List<ShaderFieldInfo>();

        foreach (var fieldDecl in fieldDecls)
        {
            // Skip static or const fields
            if (fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword)))
                continue;

            var csharpType = fieldDecl.Declaration.Type.ToString();
            GetHLSLTypeAndSize(csharpType, out var hlslType, out var byteSize);

            if (byteSize == 0)
            {
                Logger.Warning($"Type {csharpType} in struct {structName} has an unknown size. Packing alignment may be incorrect.");
            }

            foreach (var variable in fieldDecl.Declaration.Variables)
            {
                var overrideAttr = fieldDecl.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .FirstOrDefault(a => a.Name.ToString() == "GenerateAsHLSLType" || a.Name.ToString() == "GenerateAsHLSLTypeAttribute");

                if (overrideAttr != null && overrideAttr.ArgumentList?.Arguments.Count > 0)
                {
                    var argExpr = overrideAttr.ArgumentList.Arguments[0].Expression;
                    if (argExpr is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        hlslType = lit.Token.ValueText;
                    }
                }

                fieldsList.Add(new ShaderFieldInfo(variable.Identifier.Text, csharpType, hlslType, byteSize));
            }
        }

        var fields = fieldsList.ToArray();
        var shaderFields = new ShaderFieldInfo[fields.Length];

        if (packingRules == PackingRules.Aligned)
        {
            var sortedFields = new List<ShaderFieldInfo>(fields.Length);
            var looked = new bool[fields.Length];
            var paddingIndex = 0;

            for (var i = 0; i < fields.Length; i++)
            {
                if (looked[i]) continue;

                var field = fields[i];
                var size = field.ByteSize;

                sortedFields.Add(field);
                looked[i] = true;

                if (size > 0)
                {
                    var registerRemaining = HLSL_VECTOR_REGISTER_SIZE - (size % HLSL_VECTOR_REGISTER_SIZE);
                    if (registerRemaining == HLSL_VECTOR_REGISTER_SIZE) registerRemaining = 0;

                    while (registerRemaining > 0)
                    {
                        var nextSize = FindNextFieldThatFits(fields, looked, i + 1, registerRemaining, out var nextIndex);
                        if (nextSize == 0 || nextIndex == -1)
                        {
                            break;
                        }

                        looked[nextIndex] = true;
                        sortedFields.Add(fields[nextIndex]);

                        registerRemaining -= nextSize;
                    }

                    if (registerRemaining != 0)
                    {
                        // Add padding if necessary
                        var count = registerRemaining / sizeof(float);
                        for (var p = 0; p < count; p++)
                        {
                            sortedFields.Add(new ShaderFieldInfo($"_padding{paddingIndex++}", "float", "float", sizeof(float)));
                        }
                    }
                }
            }

            shaderFields = sortedFields.ToArray();
        }
        else
        {
            for (var i = 0; i < fields.Length; i++)
            {
                shaderFields[i] = fields[i];
            }
        }

        sb.Append(@$"
struct {structName}
{{");
        foreach (var field in shaderFields)
        {
            sb.Append(@$"
    {field.HLSLType} {field.Name};");
        }

        sb.AppendLine(@"
};");
    }

    public static void GenerateHLSLTypes(ShaderMetadata manifest, string text)
    {
        if (!text.Contains("GenerateHLSL"))
        {
            return;
        }

        var tree = CSharpSyntaxTree.ParseText(text);
        var root = tree.GetRoot();

        var enumDeclarations = root.DescendantNodes().OfType<EnumDeclarationSyntax>();
        var structDeclarations = root.DescendantNodes().OfType<StructDeclarationSyntax>();

        var cadidateDeclarations = new List<(BaseTypeDeclarationSyntax syntax, PackingRules rules)>();
        var virtualPathToCadidate = new Dictionary<string, List<int>>();

        foreach (var decl in enumDeclarations)
        {
            FindCadidate(cadidateDeclarations, virtualPathToCadidate, decl);
        }

        foreach (var decl in structDeclarations)
        {
            FindCadidate(cadidateDeclarations, virtualPathToCadidate, decl);
        }

        Console.WriteLine($"Found {cadidateDeclarations.Count} candidates in this file.");

        foreach (var kvp in virtualPathToCadidate)
        {
            var virtualPath = kvp.Key;
            var list = kvp.Value;
            var sb = new StringBuilder();

            foreach (var index in list)
            {
                var item = cadidateDeclarations[index];
                if (item.syntax is EnumDeclarationSyntax enumDecl)
                {
                    GenerateEnumHLSL(enumDecl, sb);
                }
                else if (item.syntax is StructDeclarationSyntax structDecl)
                {
                    GenerateStructHLSL(structDecl, item.rules, sb);
                }
            }

            if (manifest.VirtualShader.TryGetValue(virtualPath, out var existingCode))
            {
                manifest.VirtualShader[virtualPath] = existingCode + "\n" + sb.ToString();
            }
            else
            {
                manifest.VirtualShader[virtualPath] = sb.ToString();
            }
        }
    }

    private static void FindCadidate(List<(BaseTypeDeclarationSyntax syntax, PackingRules rules)> cadidateDeclarations, Dictionary<string, List<int>> virtualPathToCadidate, BaseTypeDeclarationSyntax decl)
    {
        var attribute = decl.AttributeLists
            .SelectMany(attrList => attrList.Attributes)
            .FirstOrDefault(attr =>
            {
                var name = attr.Name.ToString();
                return name == "GenerateHLSL" || name == "GenerateHLSLAttribute" || name.EndsWith(".GenerateHLSL") || name.EndsWith(".GenerateHLSLAttribute");
            });

        if (attribute == null)
        {
            return;
        }

        var packingRules = attribute.ArgumentList?.Arguments[0].Expression switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.NumericLiteralExpression) => (PackingRules)(int)literal.Token.Value!,
            MemberAccessExpressionSyntax memberAccess => Enum.Parse<PackingRules>(memberAccess.Name.ToString()),
            _ => PackingRules.Exact
        };

        var virtualPath = attribute.ArgumentList?.Arguments[1].Expression switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal.Token.ValueText,
            _ => null
        };

        if (virtualPath == null)
        {
            Logger.Warning($"Type {decl.Identifier.Text} has GenerateHLSL attribute but no virtual path specified. Skipping HLSL generation for this type.");
            return;
        }

        var index = cadidateDeclarations.Count;
        cadidateDeclarations.Add((decl, packingRules));

        if (!virtualPathToCadidate.TryGetValue(virtualPath, out var list))
        {
            list = new List<int>();
            virtualPathToCadidate[virtualPath] = list;
        }

        list.Add(index);
    }

    [GeneratedRegex("(?<=[a-z])([A-Z])")]
    private static partial Regex CamelCaseToUnderscoreRegex();
}
