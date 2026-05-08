using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Ghost.Generator
{
    [Generator]
    internal class ShaderPropertiesGenerator : IIncrementalGenerator
    {
        private class ShaderStructInfo
        {
            public string ShaderName
            {
                set; get;
            }

            public string Name
            {
                get; set;
            }

            public INamedTypeSymbol TypeSymbol
            {
                get; set;
            }
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var shaderProperties = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "Ghost.Core.Graphics.GenerateShaderPropertyAttribute",
                    (n, ct) => n is Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax,
                    (ctx, ct) =>
                    {
                        var structSymbol = (INamedTypeSymbol)ctx.TargetSymbol;

                        var attributeData = ctx.Attributes.FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == "Ghost.Core.Graphics.GenerateShaderPropertyAttribute");
                        if (attributeData == null)
                        {
                            return null;
                        }

                        return new ShaderStructInfo
                        {
                            ShaderName = attributeData.ConstructorArguments[0].Value as string,
                            Name = attributeData.ConstructorArguments[1].Value as string ?? structSymbol.Name,
                            TypeSymbol = structSymbol
                        };
                    })
                .Where(x => x != null)
                .Collect();

            context.RegisterSourceOutput(shaderProperties, GenerateHlslPropertyStruct);
        }

        private void GenerateHlslPropertyStruct(SourceProductionContext context, ImmutableArray<ShaderStructInfo> array)
        {
            if (array.IsDefaultOrEmpty)
            {
                return;
            }

            var codeBuilder = new StringBuilder();
            var registerBuilder = new StringBuilder();

            foreach (var info in array)
            {
                if (string.IsNullOrEmpty(info.ShaderName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                        "GSHADER001",
                        "Invalid shader name",
                        $"Shader name for struct '{info.TypeSymbol.Name}' cannot be null or empty.",
                        "ShaderGeneration",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true), info.TypeSymbol.Locations.FirstOrDefault()));
                    continue;
                }

                if (!info.TypeSymbol.IsUnmanagedType)
                {
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                        "GSHADER002",
                        "Unsupported struct type",
                        $"Struct '{info.TypeSymbol.Name}' is not an unmanaged type and cannot be used for HLSL generation.",
                        "ShaderGeneration",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true), info.TypeSymbol.Locations.FirstOrDefault()));
                    continue;
                }

                var definedSymbol = $"__{info.Name.ToUpper()}_G_HLSL";

                var fieldsBuilder = new StringBuilder();
                fieldsBuilder.AppendLine($"        public static readonly global::Ghost.Core.Graphics.ShaderPropertyFieldInfo[] ReflectionData = new global::Ghost.Core.Graphics.ShaderPropertyFieldInfo[]");
                fieldsBuilder.AppendLine("        {");

                var fields = info.TypeSymbol.GetMembers().OfType<IFieldSymbol>();
                foreach (var field in fields)
                {
                    if (field.IsStatic || field.IsConst)
                    {
                        continue;
                    }

                    var hlslTypeAttribute = field.GetAttributes().FirstOrDefault(ad => ad.AttributeClass?.ToDisplayString() == "Ghost.Engine.Utilities.GenerateAsHLSLTypeAttribute");
                    var hlslType = string.Empty;
                    var shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.Unknown";

                    if (hlslTypeAttribute == null)
                    {
                        switch (field.Type.SpecialType)
                        {
                            case SpecialType.System_Single:
                                hlslType = "float";
                                shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.Float";
                                break;
                            case SpecialType.System_Int32:
                                hlslType = "int";
                                shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.Int";
                                break;
                            case SpecialType.System_UInt32:
                                hlslType = "uint";
                                shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.UInt";
                                break;
                            default:
                                var typeName = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                switch (typeName)
                                {
                                    case "global::System.Numerics.Vector2" or "global::Misaki.HighPerformance.Mathematics.float2":
                                        hlslType = "float2";
                                        shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.Float2";
                                        break;
                                    case "global::System.Numerics.Vector3" or "global::Misaki.HighPerformance.Mathematics.float3":
                                        hlslType = "float3";
                                        shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.Float3";
                                        break;
                                    case "global::System.Numerics.Vector4" or "global::Misaki.HighPerformance.Mathematics.float4":
                                        hlslType = "float4";
                                        shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.Float4";
                                        break;
                                    case "global::System.Numerics.Matrix4x4" or "global::Misaki.HighPerformance.Mathematics.float4x4":
                                        hlslType = "float4x4";
                                        shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.Float4x4";
                                        break;
                                    case "global::System.Numerics.Quaternion" or "global::Misaki.HighPerformance.Mathematics.quaternion":
                                        hlslType = "float4";
                                        shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.Float4";
                                        break;
                                    case "global::Ghost.Core.Graphics.Texture2DHandle":
                                        hlslType = "uint";
                                        shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.Texture2D";
                                        break;
                                    case "global::Ghost.Core.Graphics.Texture3DHandle":
                                        hlslType = "uint";
                                        shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.Texture3D";
                                        break;
                                    case "global::Ghost.Core.Graphics.BufferHandle":
                                        hlslType = "uint";
                                        shaderPropType = "global::Ghost.Core.Graphics.ShaderPropertyType.Buffer";
                                        break;
                                    case var _ when typeName.StartsWith("global::Misaki.HighPerformance.Mathematics."):
                                        hlslType = typeName.Substring("global::Misaki.HighPerformance.Mathematics.".Length);
                                        break;
                                    default:
                                        break;
                                }

                                break;
                        }
                    }
                    else
                    {
                        hlslType = hlslTypeAttribute.ConstructorArguments[0].Value as string;
                    }

                    if (string.IsNullOrEmpty(hlslType))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                            "GSHADER003",
                            "Unsupported field type",
                            $"Field '{field.Name}' in struct '{info.TypeSymbol.Name}' has unsupported type '{field.Type.ToDisplayString()}' for HLSL generation.",
                            "ShaderGeneration",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true), field.Locations.FirstOrDefault()));
                        continue;
                    }

                    codeBuilder.AppendLine($"    {hlslType} {field.Name};");

                    fieldsBuilder.AppendLine($"            new global::Ghost.Core.Graphics.ShaderPropertyFieldInfo {{ Name = \"{field.Name}\", Type = {shaderPropType}, Offset = (int)global::System.Runtime.InteropServices.Marshal.OffsetOf<{info.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(\"{field.Name}\") }},");
                }

                fieldsBuilder.AppendLine("        };");

                var code = $@"// <auto-generated/>

namespace {info.TypeSymbol.ContainingNamespace.ToDisplayString()}
{{
    [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 4)]
    {info.TypeSymbol.DeclaredAccessibility.ToString().ToLower()} partial struct {info.TypeSymbol.Name}
    {{
#if GHOST_EDITOR
        public const string HLSL_SOURCE = @""
# ifndef {definedSymbol}
# define {definedSymbol}
struct {info.Name}
{{
{codeBuilder}
}};
# endif // {definedSymbol}"";

{fieldsBuilder}
#endif
    }}
}}";

                context.AddSource($"{info.TypeSymbol.Name}_HLSL.gen.cs", code);
                codeBuilder.Clear();

                var typeFullName = info.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                registerBuilder.AppendLine($@"        global::Ghost.Core.Graphics.ShaderPropertiesRegistry.Register(""{info.ShaderName}"", {typeFullName}.HLSL_SOURCE, (uint)sizeof({typeFullName}), {typeFullName}.ReflectionData);");
            }

            var registerTypeName = "g_shaderproperty_registeration";
            var registerCode = $@"// <auto-generated/>

#if GHOST_EDITOR
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
internal static partial class {registerTypeName}
{{
    [global::System.Runtime.CompilerServices.ModuleInitializer]
    internal unsafe static void RegisterShaderProperties()
    {{
{registerBuilder}
    }}
}}
#endif";

            context.AddSource($"{registerTypeName}.gen.cs", registerCode);
        }
    }
}
