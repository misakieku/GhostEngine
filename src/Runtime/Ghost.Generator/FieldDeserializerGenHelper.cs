using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ghost.Generator
{
    internal static class FieldDeserializerGenHelper
    {
        public static uint ComputeFnv1a(string value)
        {
            const uint OFFSET_BASIS = 2166136261u;
            const uint PRIME = 16777619u;

            var hash = OFFSET_BASIS;
            foreach (var b in Encoding.UTF8.GetBytes(value))
            {
                hash ^= b;
                hash *= PRIME;
            }

            return hash;
        }

        public static bool HasAttribute(ISymbol symbol, string attributeFullName)
        {
            foreach (var attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == attributeFullName)
                {
                    return true;
                }
            }

            return false;
        }

        public static List<IFieldSymbol> GetSerializableFields(INamedTypeSymbol symbol)
        {
            var result = new List<IFieldSymbol>();
            foreach (var member in symbol.GetMembers())
            {
                if (member is IFieldSymbol field
                    && field.DeclaredAccessibility == Accessibility.Public
                    && !field.IsStatic
                    && !field.IsConst
                    && !field.IsReadOnly
                    && !field.IsFixedSizeBuffer)
                {
                    result.Add(field);
                }
            }

            return result;
        }

        public static bool TryEmitFieldSwitchBody(StringBuilder sb, INamedTypeSymbol structSymbol, string indent, out int emittedCount)
        {
            var fields = GetSerializableFields(structSymbol);
            var fullTypeName = structSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var armsByHash = new Dictionary<uint, List<(IFieldSymbol Field, string Code)>>();

            foreach (var field in fields)
            {
                var assignCode = BuildFieldAssignmentCode(field, "reader", "target");
                if (assignCode == null)
                {
                    continue;
                }

                var hash = ComputeFnv1a(field.Name);
                if (!armsByHash.TryGetValue(hash, out var list))
                {
                    list = new List<(IFieldSymbol, string)>();
                    armsByHash[hash] = list;
                }

                list.Add((field, assignCode));
            }

            emittedCount = 0;
            if (armsByHash.Count == 0)
            {
                return false;
            }

            sb.AppendLine($"{indent}ref var target = ref *({fullTypeName}*)componentPtr;");
            sb.AppendLine($"{indent}switch (fieldNameHash)");
            sb.AppendLine($"{indent}{{");

            foreach (var kvp in armsByHash)
            {
                sb.AppendLine($"{indent}    case 0x{kvp.Key:X8}u:");
                sb.AppendLine($"{indent}    {{");
                foreach (var entry in kvp.Value)
                {
                    sb.AppendLine($"{indent}        {entry.Code}");
                    emittedCount++;
                }

                sb.AppendLine($"{indent}        break;");
                sb.AppendLine($"{indent}    }}");
            }

            sb.AppendLine($"{indent}}}");
            return true;
        }

        private static string? BuildFieldAssignmentCode(IFieldSymbol field, string readerVar, string targetVar)
        {
            var targetType = field.Type;
            var isNullable = false;
            if (targetType is INamedTypeSymbol nullableNamed
                && nullableNamed.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
                && nullableNamed.TypeArguments.Length == 1)
            {
                targetType = nullableNamed.TypeArguments[0];
                isNullable = true;
            }

            var targetMember = $"{targetVar}.{field.Name}";
            var assignCode = BuildNonNullAssignmentCode(targetType, readerVar, targetMember);
            if (assignCode == null)
            {
                return null;
            }

            return isNullable
                ? $"if ({readerVar}.Kind == global::Ghost.Core.SceneFieldValueKind.None) {{ {targetMember} = null; }} else {{ {assignCode} }}"
                : assignCode;
        }

        private static string? BuildNonNullAssignmentCode(ITypeSymbol targetType, string readerVar, string targetMember)
        {

            switch (targetType.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return $"if ({readerVar}.TryReadBool(out var val)) {{ {targetMember} = val; }}";
                case SpecialType.System_SByte:
                    return $"if ({readerVar}.TryReadInt64(out var val)) {{ {targetMember} = (sbyte)val; }}";
                case SpecialType.System_Byte:
                    return $"if ({readerVar}.TryReadUInt64(out var val)) {{ {targetMember} = (byte)val; }}";
                case SpecialType.System_Int16:
                    return $"if ({readerVar}.TryReadInt64(out var val)) {{ {targetMember} = (short)val; }}";
                case SpecialType.System_UInt16:
                    return $"if ({readerVar}.TryReadUInt64(out var val)) {{ {targetMember} = (ushort)val; }}";
                case SpecialType.System_Int32:
                    return $"if ({readerVar}.TryReadInt64(out var val)) {{ {targetMember} = (int)val; }}";
                case SpecialType.System_UInt32:
                    return $"if ({readerVar}.TryReadUInt64(out var val)) {{ {targetMember} = (uint)val; }}";
                case SpecialType.System_Int64:
                    return $"if ({readerVar}.TryReadInt64(out var val)) {{ {targetMember} = val; }}";
                case SpecialType.System_UInt64:
                    return $"if ({readerVar}.TryReadUInt64(out var val)) {{ {targetMember} = val; }}";
                case SpecialType.System_Single:
                    return $"if ({readerVar}.TryReadSingle(out var val)) {{ {targetMember} = val; }}";
                case SpecialType.System_Double:
                    return $"if ({readerVar}.TryReadDouble(out var val)) {{ {targetMember} = val; }}";
            }

            if (targetType.TypeKind == TypeKind.Enum)
            {
                return BuildEnumAssignmentCode((INamedTypeSymbol)targetType, readerVar, targetMember);
            }

            var qualifiedName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            switch (qualifiedName)
            {
                case "global::Misaki.HighPerformance.Mathematics.half":
                    return $"if ({readerVar}.TryReadSingle(out var val)) {{ {targetMember} = (global::Misaki.HighPerformance.Mathematics.half)val; }}";
                case "global::Misaki.HighPerformance.Mathematics.float2":
                    return BuildVectorAssign(readerVar, targetMember, "float", "TryReadSingles", 2, $"{qualifiedName}(v[0], v[1])");
                case "global::Misaki.HighPerformance.Mathematics.float3":
                    return BuildVectorAssign(readerVar, targetMember, "float", "TryReadSingles", 3, $"{qualifiedName}(v[0], v[1], v[2])");
                case "global::Misaki.HighPerformance.Mathematics.float4":
                case "global::Misaki.HighPerformance.Mathematics.quaternion":
                case "global::System.Numerics.Vector4":
                case "global::System.Numerics.Quaternion":
                    return BuildVectorAssign(readerVar, targetMember, "float", "TryReadSingles", 4, $"{qualifiedName}(v[0], v[1], v[2], v[3])");
                case "global::Misaki.HighPerformance.Mathematics.float4x4":
                    return BuildFloat4x4Assign(readerVar, targetMember, qualifiedName);
                case "global::Misaki.HighPerformance.Mathematics.int2":
                    return BuildVectorAssign(readerVar, targetMember, "int", "TryReadInt32s", 2, $"{qualifiedName}(v[0], v[1])");
                case "global::Misaki.HighPerformance.Mathematics.int3":
                    return BuildVectorAssign(readerVar, targetMember, "int", "TryReadInt32s", 3, $"{qualifiedName}(v[0], v[1], v[2])");
                case "global::Misaki.HighPerformance.Mathematics.int4":
                    return BuildVectorAssign(readerVar, targetMember, "int", "TryReadInt32s", 4, $"{qualifiedName}(v[0], v[1], v[2], v[3])");
                case "global::Misaki.HighPerformance.Mathematics.uint2":
                    return BuildVectorAssign(readerVar, targetMember, "uint", "TryReadUInt32s", 2, $"{qualifiedName}(v[0], v[1])");
                case "global::Misaki.HighPerformance.Mathematics.uint3":
                    return BuildVectorAssign(readerVar, targetMember, "uint", "TryReadUInt32s", 3, $"{qualifiedName}(v[0], v[1], v[2])");
                case "global::Misaki.HighPerformance.Mathematics.uint4":
                    return BuildVectorAssign(readerVar, targetMember, "uint", "TryReadUInt32s", 4, $"{qualifiedName}(v[0], v[1], v[2], v[3])");
                case "global::System.Numerics.Vector2":
                    return BuildVectorAssign(readerVar, targetMember, "float", "TryReadSingles", 2, $"{qualifiedName}(v[0], v[1])");
                case "global::System.Numerics.Vector3":
                    return BuildVectorAssign(readerVar, targetMember, "float", "TryReadSingles", 3, $"{qualifiedName}(v[0], v[1], v[2])");
                case "global::Ghost.Graphics.Core.RenderingLayerMask":
                    return $"if ({readerVar}.TryReadUInt64(out var val)) {{ {targetMember} = new global::Ghost.Graphics.Core.RenderingLayerMask((uint)val); }}";
                case "global::Ghost.Entities.Entity":
                    return $"{targetMember} = global::Ghost.Entities.ComponentRegistry.ResolveEntity(in {readerVar});";
            }

            if (targetType is INamedTypeSymbol genericNamed && genericNamed.TypeArguments.Length == 1)
            {
                var origDef = genericNamed.ConstructedFrom.ToDisplayString();
                var argType = genericNamed.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (origDef == "Ghost.Core.Handle<T>")
                {
                    return $"{targetMember} = {readerVar}.ResolveAssetHandle<{argType}>();";
                }

                if (origDef == "Ghost.Core.Identifier<T>")
                {
                    if (argType == "global::Ghost.Graphics.Services.MaterialPalette")
                    {
                        return $"{targetMember} = new global::Ghost.Core.Identifier<{argType}>({readerVar}.ResolveMaterialPalette());";
                    }

                    return $"if ({readerVar}.TryReadInt64(out var val)) {{ {targetMember} = new global::Ghost.Core.Identifier<{argType}>((int)val); }}";
                }
            }

            return null;
        }

        private static string BuildVectorAssign(string readerVar, string targetMember, string scalarType, string readMethod, int count, string ctorExpr)
        {
            return $"global::System.Span<{scalarType}> v = stackalloc {scalarType}[{count}]; if ({readerVar}.{readMethod}(v)) {{ {targetMember} = new {ctorExpr}; }}";
        }

        private static string BuildFloat4x4Assign(string readerVar, string targetMember, string matrixType)
        {
            const string F4 = "global::Misaki.HighPerformance.Mathematics.float4";
            var cols = $"new {F4}(v[0], v[1], v[2], v[3]), new {F4}(v[4], v[5], v[6], v[7]), new {F4}(v[8], v[9], v[10], v[11]), new {F4}(v[12], v[13], v[14], v[15])";
            return BuildVectorAssign(readerVar, targetMember, "float", "TryReadSingles", 16, $"{matrixType}({cols})");
        }

        private static string BuildEnumAssignmentCode(INamedTypeSymbol enumType, string readerVar, string targetMember)
        {
            var enumFullName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var enumMembers = enumType.GetMembers().OfType<IFieldSymbol>().Where(f => f.HasConstantValue).ToList();
            if (enumMembers.Count == 0)
            {
                return $"if ({readerVar}.TryReadInt64(out var enumNum)) {{ {targetMember} = ({enumFullName})enumNum; }}";
            }

            var isFlags = HasAttribute(enumType, "System.FlagsAttribute");
            var sb = new StringBuilder();
            sb.Append($"if ({readerVar}.TryReadString(out var enumStr)) {{ ");

            if (isFlags)
            {
                sb.Append("var maskVal = 0L; var rem = global::System.MemoryExtensions.AsSpan(enumStr); ");
                sb.Append("while (!rem.IsEmpty) { var comma = global::System.MemoryExtensions.IndexOf(rem, ','); ");
                sb.Append("var part = global::System.MemoryExtensions.Trim(comma >= 0 ? rem.Slice(0, comma) : rem); ");
                sb.Append("maskVal |= part switch { ");
                foreach (var m in enumMembers)
                {
                    sb.Append($"\"{m.Name}\" => (long){enumFullName}.{m.Name}, ");
                }

                sb.Append($"_ => 0L }}; if (comma < 0) {{ break; }} rem = rem.Slice(comma + 1); }} {targetMember} = ({enumFullName})maskVal; ");
            }
            else
            {
                sb.Append($"{targetMember} = enumStr switch {{ ");
                foreach (var m in enumMembers)
                {
                    sb.Append($"\"{m.Name}\" => {enumFullName}.{m.Name}, ");
                }

                sb.Append($"_ => {targetMember} }}; ");
            }

            sb.Append($"}} else if ({readerVar}.TryReadInt64(out var enumNum)) {{ {targetMember} = ({enumFullName})enumNum; }}");
            return sb.ToString();
        }
    }
}
