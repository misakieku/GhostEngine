using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Ghost.Generator
{
    [Generator]
    public class ComponentSerializationGenerator : IIncrementalGenerator
    {
        private string GetJsonWriteCall(ITypeSymbol type, string fieldName)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_UInt64:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                    return $@"writer.WriteNumber(""{fieldName}"", value.{fieldName});";
                case SpecialType.System_Boolean:
                    return $@"writer.WriteBoolean(""{fieldName}"", value.{fieldName});";
                case SpecialType.System_Char:
                    return $@"writer.WriteString(""{fieldName}"", [value.{fieldName}]);";
                case SpecialType.System_String:
                    return $@"writer.WriteString(""{fieldName}"", value.{fieldName});";
            }

            return $@"writer.WritePropertyName(""{fieldName}""); global::System.Text.Json.JsonSerializer.Serialize(writer, value.{fieldName}, options);";
        }

        private string GetJsonReadCall(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Byte: return "reader.GetByte()";
                case SpecialType.System_SByte: return "reader.GetSByte()";
                case SpecialType.System_Int16: return "reader.GetInt16()";
                case SpecialType.System_Int32: return "reader.GetInt32()";
                case SpecialType.System_Int64: return "reader.GetInt64()";
                case SpecialType.System_Single: return "reader.GetSingle()";
                case SpecialType.System_Double: return "reader.GetDouble()";
                case SpecialType.System_Decimal: return "reader.GetDecimal()";
                case SpecialType.System_UInt16: return "reader.GetUInt16()";
                case SpecialType.System_UInt32: return "reader.GetUInt32()";
                case SpecialType.System_UInt64: return "reader.GetUInt64()";
                // Note: the size of IntPtr and UIntPtr varies by platform, we use Int64/UInt64 to ensure compatibility
                case SpecialType.System_IntPtr: return "reader.GetInt64()";
                case SpecialType.System_UIntPtr: return "reader.GetUInt64()";
                case SpecialType.System_Boolean: return "reader.GetBoolean()";
                case SpecialType.System_Char: return "reader.GetString()[0]";
                case SpecialType.System_String: return "reader.GetString()";
            }

            return $"global::System.Text.Json.JsonSerializer.Deserialize<{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(ref reader, options)";
        }

        private void GenerateJsonSerializer(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> symbols)
        {
            var sbWrites = new StringBuilder();
            var sbReads = new StringBuilder();

            foreach (var symbol in symbols)
            {
                var namespaceName = symbol.ContainingNamespace.ToDisplayString();
                var structName = symbol.Name;
                var fullTypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // 1. Build Field Logic (Same as before)
                sbWrites.Clear();
                sbReads.Clear();

                var fields = symbol.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(f => !f.IsStatic && f.DeclaredAccessibility == Accessibility.Public);

                foreach (var field in fields)
                {
                    // Note: GetJsonWriteCall returns a string ending with ";"
                    var writeCall = GetJsonWriteCall(field.Type, field.Name);
                    if (writeCall != null)
                    {
                        sbWrites.Append("            "); // Indentation
                        sbWrites.AppendLine(writeCall);
                    }

                    var readCall = GetJsonReadCall(field.Type);
                    if (readCall != null)
                    {
                        // Note the double quotes ""{field.Name}"" for the case string
                        sbReads.Append("                    "); // Indentation
                        sbReads.AppendLine($@"case ""{field.Name}"": result.{field.Name} = {readCall}; break;");
                    }
                }

                // 2. The Main Template using $@
                // Watch the double braces {{ }} and double quotes "" ""
                var sourceCode = $@"// <auto-generated/>
#nullable enable

namespace {namespaceName}
{{
    public unsafe static class {structName}_Serializer
    {{
        [global::System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Init() 
        {{
             var id = Ghost.Entities.ComponentTypeID<{fullTypeName}>.Value;
             Ghost.Engine.IO.ComponentSerializerRegistry.Register(id, SerializeBinaryUnsafe,  SerializeJsonUnsafe);
        }}

        // ---------------------------------------------------------
        // BINARY (Fast Path)
        // ---------------------------------------------------------
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static unsafe void SerializeBinaryUnsafe(global::System.IO.BinaryWriter writer, void* value)
        {{
            writer.Write(new global::System.ReadOnlySpan<byte>(value, sizeof({fullTypeName})));
        }}

        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void SerializeBinary(this global::System.IO.BinaryWriter writer, ref {fullTypeName} value)
        {{
            unsafe {{ writer.Write(new global::System.ReadOnlySpan<byte>(global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref value), sizeof({fullTypeName}))); }}
        }}

        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void DeserializeBinary(this global::System.IO.BinaryReader reader, ref {fullTypeName} value)
        {{
            unsafe {{ reader.Read(new global::System.Span<byte>(global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref value), sizeof({fullTypeName}))); }}
        }}

        // ---------------------------------------------------------
        // JSON WRITE
        // ---------------------------------------------------------
        public static unsafe void SerializeJsonUnsafe(System.Text.Json.Utf8JsonWriter writer, void* ptr, System.Text.Json.JsonSerializerOptions? options)
        {{
            SerializeJson(writer, ref *( {fullTypeName}*)ptr, options);
        }}

        public static void SerializeJson(this global::System.Text.Json.Utf8JsonWriter writer, ref {fullTypeName} value, global::System.Text.Json.JsonSerializerOptions? options)
        {{
            writer.WriteStartObject();
{sbWrites}
            writer.WriteEndObject();
        }}

        // ---------------------------------------------------------
        // JSON READ
        // ---------------------------------------------------------
        public static {fullTypeName} DeserializeJson(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Text.Json.JsonSerializerOptions? options)
        {{
            var result = default({fullTypeName});

            if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartObject) throw new global::System.Text.Json.JsonException();

            while (reader.Read())
            {{
                if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject) return result;
                if (reader.TokenType != global::System.Text.Json.JsonTokenType.PropertyName) throw new global::System.Text.Json.JsonException();

                var propName = reader.GetString();
                reader.Read();

                switch (propName)
                {{
{sbReads}
                    default: reader.Skip(); break;
                }}
            }}
            return result;
        }}
    }}
}}";

                context.AddSource($"{structName}.Serializer.gen.cs", sourceCode);
            }
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            return;

            var componentCandidates = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (syntaxNode, _) => syntaxNode is StructDeclarationSyntax,
                    transform: (ctx, _) =>
                    {
                        var structSyntax = (StructDeclarationSyntax)ctx.Node;

                        if (!(ctx.SemanticModel.GetDeclaredSymbol(structSyntax) is INamedTypeSymbol symbol))
                        {
                            return null;
                        }

                        var compilation = ctx.SemanticModel.Compilation;
                        var iComponentSymbol = compilation.GetTypeByMetadataName("Ghost.Entities.IComponent");

                        if (iComponentSymbol == null)
                        {
                            return null;
                        }

                        foreach (var iface in symbol.AllInterfaces)
                        {
                            if (SymbolEqualityComparer.Default.Equals(iface, iComponentSymbol))
                            {
                                return symbol;
                            }
                        }

                        return null;
                    })
                .Where(symbol => symbol != null)
                .Collect();

            context.RegisterSourceOutput(componentCandidates, GenerateJsonSerializer);
        }
    }
}
