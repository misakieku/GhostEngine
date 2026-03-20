using Ghost.NativeWrapperGen.Config;
using Ghost.NativeWrapperGen.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ghost.NativeWrapperGen.Parsing;

public sealed class BindingParser
{
    public NativeLibrary Parse(string inputDirectory, WrapperConfig config)
    {
        var members = new List<NativeMember>();
        var structs = new List<NativeStruct>();
        var enums = new List<NativeEnum>();
        var functions = new List<NativeFunction>();

        foreach (var filePath in Directory.GetFiles(inputDirectory, "*.cs", SearchOption.TopDirectoryOnly).OrderBy(static p => p, StringComparer.Ordinal))
        {
            var text = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetRoot();
            var namespaceName = GetNamespace(root);

            foreach (var @struct in root.DescendantNodes().OfType<StructDeclarationSyntax>())
            {
                if (@struct.Parent is not NamespaceDeclarationSyntax && @struct.Parent is not FileScopedNamespaceDeclarationSyntax)
                {
                    continue;
                }

                if (config.SkipTypes.Contains(@struct.Identifier.ValueText, StringComparer.Ordinal))
                {
                    continue;
                }

                var structMembers = ParseMembers(@struct);
                var listInfo = TryMatchList(structMembers);

                structs.Add(new NativeStruct
                {
                    Name = @struct.Identifier.ValueText,
                    Namespace = namespaceName,
                    Members = structMembers,
                    IsList = listInfo.IsList,
                    IsPointerList = listInfo.IsPointerList,
                    ListElementType = listInfo.ListElementType,
                });
            }

            foreach (var @enum in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
            {
                if (@enum.Parent is not NamespaceDeclarationSyntax && @enum.Parent is not FileScopedNamespaceDeclarationSyntax)
                {
                    continue;
                }

                if (config.SkipTypes.Contains(@enum.Identifier.ValueText, StringComparer.Ordinal))
                {
                    continue;
                }

                enums.Add(new NativeEnum
                {
                    Name = @enum.Identifier.ValueText,
                    Members = @enum.Members.Select(static m => m.Identifier.ValueText).ToArray(),
                });
            }

            foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (classDeclaration.Identifier.ValueText != "Api")
                {
                    continue;
                }

                foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
                {
                    functions.Add(new NativeFunction
                    {
                        Name = method.Identifier.ValueText,
                        ReturnType = NormalizeType(method.ReturnType.ToString()),
                        Parameters = method.ParameterList.Parameters.Select(static p => new NativeParameter
                        {
                            Name = p.Identifier.Text, // .Text preserves @ prefix for reserved keywords (e.g. @params, @base)
                            TypeName = NormalizeType(p.Type?.ToString() ?? "void"),
                        }).ToArray(),
                        IsDllImport = method.AttributeLists.SelectMany(static a => a.Attributes).Any(static a => a.Name.ToString().Contains("DllImport", StringComparison.Ordinal)),
                    });
                }
            }
        }

        var structsByName = structs.ToDictionary(static s => s.Name, StringComparer.Ordinal);
        var functionsByName = functions.GroupBy(static f => f.Name, StringComparer.Ordinal).ToDictionary(static g => g.Key, static g => g.First(), StringComparer.Ordinal);

        return new NativeLibrary
        {
            NativeNamespace = config.NativeNamespace,
            Structs = structs,
            Enums = enums,
            Functions = functions,
            StructsByName = structsByName,
            FunctionsByName = functionsByName,
        };
    }

    private static IReadOnlyList<NativeMember> ParseMembers(StructDeclarationSyntax @struct)
    {
        var members = new List<NativeMember>();

        foreach (var member in @struct.Members)
        {
            switch (member)
            {
                case FieldDeclarationSyntax field:
                    if (field.Declaration.Type is FunctionPointerTypeSyntax)
                    {
                        continue;
                    }

                    foreach (var variable in field.Declaration.Variables)
                    {
                        members.Add(new NativeMember
                        {
                            Name = variable.Identifier.ValueText,
                            TypeName = NormalizeType(field.Declaration.Type.ToString()),
                            Kind = NativeMemberKind.Field,
                        });
                    }
                    break;
                case PropertyDeclarationSyntax property:
                    members.Add(new NativeMember
                    {
                        Name = property.Identifier.ValueText,
                        TypeName = NormalizeType(property.Type.ToString()),
                        Kind = NativeMemberKind.Property,
                    });
                    break;
            }
        }

        return members;
    }

    private static (bool IsList, bool IsPointerList, string? ListElementType) TryMatchList(IReadOnlyList<NativeMember> members)
    {
        var data = members.FirstOrDefault(static m => m.Kind == NativeMemberKind.Field && m.Name == "data");
        var count = members.FirstOrDefault(static m => m.Kind == NativeMemberKind.Field && m.Name == "count");
        if (data is null || count is null)
        {
            return default;
        }

        if (count.TypeName != "nuint")
        {
            return default;
        }

        var pointerDepth = GetPointerDepth(data.TypeName);
        if (pointerDepth == 0)
        {
            return default;
        }

        return (true, pointerDepth > 1, TrimPointers(data.TypeName));
    }

    private static string GetNamespace(SyntaxNode root)
    {
        var fileScoped = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScoped is not null)
        {
            return fileScoped.Name.ToString();
        }

        var block = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        if (block is not null)
        {
            return block.Name.ToString();
        }

        return string.Empty;
    }

    public static string NormalizeType(string typeName)
    {
        return typeName.Replace("ref ", string.Empty, StringComparison.Ordinal)
            .Replace("readonly ", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    public static int GetPointerDepth(string typeName)
    {
        var depth = 0;
        foreach (var ch in typeName)
        {
            if (ch == '*')
            {
                depth++;
            }
        }

        return depth;
    }

    public static string TrimPointers(string typeName)
    {
        return typeName.TrimEnd('*').Trim();
    }
}
