using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Ghost.Generator;

[Generator]
public class VolumeComponentGenerator : IIncrementalGenerator
{
    private const string VOLUME_COMPONENT_ATTRIBUTE = "Ghost.Core.VolumeComponentAttribute";
    private const string HARDCUT_ATTRIBUTE = "Ghost.Core.HardcutAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                VOLUME_COMPONENT_ATTRIBUTE,
                (node, _) => node is StructDeclarationSyntax,
                (ctx, _) => ctx.TargetSymbol as INamedTypeSymbol)
            .Where(symbol => symbol != null)
            .Collect();

        context.RegisterSourceOutput(candidates, GenerateVolumeComponentCode);
    }

    private static void GenerateVolumeComponentCode(SourceProductionContext context, ImmutableArray<INamedTypeSymbol?> symbols)
    {
        if (symbols.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var item in symbols.Distinct(SymbolEqualityComparer.Default))
        {
            if (item is not INamedTypeSymbol symbol)
            {
                continue;
            }

            var code = GenerateForSymbol(symbol);
            if (!string.IsNullOrEmpty(code))
            {
                context.AddSource($"{symbol.Name}.VolumeComponent.g.cs", code);
            }
        }
    }

    private static string GenerateForSymbol(INamedTypeSymbol symbol)
    {
        var ns = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
        var fullTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeName = symbol.Name;

        var directSb = new StringBuilder();
        var blendSb = new StringBuilder();

        var fields = symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsStatic && !f.IsConst);

        foreach (var field in fields)
        {
            var fieldName = field.Name;
            var fieldType = field.Type;
            var isNullable = false;
            var targetType = fieldType;

            if (fieldType is INamedTypeSymbol namedType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                isNullable = true;
                targetType = namedType.TypeArguments[0];
            }

            var hasHardcut = field.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == HARDCUT_ATTRIBUTE);
            var targetTypeName = targetType.ToDisplayString();

            var isSlerp = !hasHardcut && IsQuaternionType(targetTypeName);
            var isLerp = !hasHardcut && !isSlerp && IsInterpolatableType(targetTypeName, targetType);

            if (isNullable)
            {
                directSb.AppendLine($"        if (source.{fieldName}.HasValue)");
                directSb.AppendLine($"        {{");
                directSb.AppendLine($"            target.{fieldName} = source.{fieldName}.Value;");
                directSb.AppendLine($"        }}");

                if (isSlerp)
                {
                    blendSb.AppendLine($"        if (source.{fieldName} is {{ }} v_{fieldName})");
                    blendSb.AppendLine($"        {{");
                    blendSb.AppendLine($"            target.{fieldName} = global::Misaki.HighPerformance.Mathematics.math.slerp(target.{fieldName}.GetValueOrDefault(), v_{fieldName}, weight);");
                    blendSb.AppendLine($"        }}");
                }
                else if (isLerp)
                {
                    blendSb.AppendLine($"        if (source.{fieldName} is {{ }} v_{fieldName})");
                    blendSb.AppendLine($"        {{");
                    blendSb.AppendLine($"            target.{fieldName} = global::Misaki.HighPerformance.Mathematics.math.lerp(target.{fieldName}.GetValueOrDefault(), v_{fieldName}, weight);");
                    blendSb.AppendLine($"        }}");
                }
                else
                {
                    // Hardcut
                    blendSb.AppendLine($"        if (source.{fieldName} is {{ }} v_{fieldName})");
                    blendSb.AppendLine($"        {{");
                    blendSb.AppendLine($"            target.{fieldName} = v_{fieldName};");
                    blendSb.AppendLine($"        }}");
                }
            }
            else
            {
                directSb.AppendLine($"        target.{fieldName} = source.{fieldName};");

                if (isSlerp)
                {
                    blendSb.AppendLine($"        target.{fieldName} = global::Misaki.HighPerformance.Mathematics.math.slerp(target.{fieldName}, source.{fieldName}, weight);");
                }
                else if (isLerp)
                {
                    blendSb.AppendLine($"        target.{fieldName} = global::Misaki.HighPerformance.Mathematics.math.lerp(target.{fieldName}, source.{fieldName}, weight);");
                }
                else
                {
                    blendSb.AppendLine($"        target.{fieldName} = source.{fieldName};");
                }
            }
        }

        var hasDefault = symbol.GetMembers("Default").Any();
        var defaultPropertyCode = hasDefault
            ? string.Empty
            : $@"
    /// <summary>
    /// Gets the default value when not overridden.
    /// </summary>
    public static {fullTypeName} Default => default;
";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(ns))
        {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
        }

        sb.AppendLine($"partial struct {typeName} : global::Ghost.Engine.Volumes.IVolumeComponent<{fullTypeName}>");
        sb.AppendLine("{");
        sb.Append(defaultPropertyCode);
        sb.AppendLine(@"    /// <summary>
    /// Blends the source volume settings into the target using the specified weight.
    /// </summary>
    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static void Blend(ref " + fullTypeName + " target, in " + fullTypeName + @" source, float weight)
    {
        if (weight <= 0.0f)
        {
            return;
        }

        if (weight >= 1.0f)
        {
" + directSb + @"            return;
        }

" + blendSb + @"    }
}");

        return sb.ToString();
    }

    private static bool IsQuaternionType(string typeName)
    {
        return typeName == "Misaki.HighPerformance.Mathematics.quaternion"
            || typeName == "quaternion"
            || typeName == "System.Numerics.Quaternion"
            || typeName == "Quaternion";
    }

    private static bool IsInterpolatableType(string typeName, ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_Single || type.SpecialType == SpecialType.System_Double)
        {
            return true;
        }

        var simpleName = typeName.Split('.').Last();
        return simpleName switch
        {
            "float2" or "float3" or "float4" => true,
            "double2" or "double3" or "double4" => true,
            "half" or "half2" or "half3" or "half4" => true,
            "Vector2" or "Vector3" or "Vector4" => true,
            _ => false
        };
    }
}
