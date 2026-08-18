#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Ghost.DSL.Parser;
using Ghost.DSL.Properties;
using Ghost.DSL.ShaderParser.Syntax;
using Ghost.DSL.Syntax.Symbols;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Ghost.Generator;

[Generator]
public class GhostShaderCatalogGenerator : IIncrementalGenerator
{
    private sealed class ShaderFileInfo : IEquatable<ShaderFileInfo>
    {
        public string FilePath { get; }
        public string Text { get; }

        public ShaderFileInfo(string filePath, string text)
        {
            FilePath = filePath;
            Text = text;
        }

        public bool Equals(ShaderFileInfo other) =>
            other != null && FilePath == other.FilePath && Text == other.Text;

        public override bool Equals(object obj) =>
            obj is ShaderFileInfo other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((FilePath != null ? FilePath.GetHashCode() : 0) * 397) ^
                       (Text != null ? Text.GetHashCode() : 0);
            }
        }
    }

    private struct InterfaceEntry
    {
        public ulong Id;
        public bool IsClosed;
        public bool IsExported;
        public string? Module;
    }

    private struct ImplementationEntry
    {
        public ulong Id;
        public string InterfaceName;
        public bool IsExported;
        public string? Module;
        public string? Provider;
    }

    private struct TemplateEntry
    {
        public ulong Id;
        public TemplateDeclarationSyntax Syntax;
        public string? Module;
    }

    private struct ShaderEntry
    {
        public ulong Id;
        public ShaderDeclarationSyntax Syntax;
        public string? Module;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var shaderFiles = context.AdditionalTextsProvider
            .Where(static file =>
            {
                var path = file.Path;
                return path.EndsWith(".gshdr", StringComparison.OrdinalIgnoreCase) ||
                       path.EndsWith(".gmod", StringComparison.OrdinalIgnoreCase) ||
                       path.EndsWith(".gcomp", StringComparison.OrdinalIgnoreCase);
            })
            .Select(static (file, ct) =>
            {
                var text = file.GetText(ct);
                return text == null ? null : new ShaderFileInfo(file.Path, text.ToString());
            })
            .Where(static item => item != null)
            .Select(static (item, ct) => item!)
            .Collect();

        context.RegisterSourceOutput(shaderFiles, static (spc, files) => GenerateSourceOutput(spc, files));
    }

    private static void GenerateSourceOutput(
        SourceProductionContext context,
        ImmutableArray<ShaderFileInfo> files)
    {
        if (files.IsDefaultOrEmpty)
        {
            return;
        }

        var errors = new List<DSLShaderError>();
        var parsedDocs = new List<DSLDocumentSyntax>();
        var parsedCompute = new List<ComputeShaderSyntax>();

        foreach (var file in files)
        {
            if (file.FilePath.EndsWith(".gcomp", StringComparison.OrdinalIgnoreCase))
            {
                var compute = DSLParser.ParseComputeShader(file.Text, file.FilePath, errors);
                if (compute != null)
                {
                    parsedCompute.Add(compute);
                }
            }
            else
            {
                var doc = DSLParser.ParseDocument(file.Text, file.FilePath, errors);
                if (doc != null)
                {
                    parsedDocs.Add(doc);
                }
            }
        }

        foreach (var err in errors)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "GHOST_DSL001",
                    "Shader DSL Syntax Error",
                    "{0}",
                    "Ghost.DSL",
                    DiagnosticSeverity.Error,
                    true),
                Location.None,
                err.ToString()));
        }

        // Collect all symbols
        var interfaces = new Dictionary<string, InterfaceEntry>(StringComparer.Ordinal);
        var implementations = new Dictionary<string, ImplementationEntry>(StringComparer.Ordinal);
        var templates = new Dictionary<string, TemplateEntry>(StringComparer.Ordinal);
        var shaders = new Dictionary<string, ShaderEntry>(StringComparer.Ordinal);

        foreach (var doc in parsedDocs)
        {
            // Top-level declarations
            foreach (var iface in doc.Interfaces)
            {
                var qName = iface.Name;
                interfaces[qName] = new InterfaceEntry
                {
                    Id = SymbolId.Compute(qName),
                    IsClosed = iface.IsClosed,
                    IsExported = iface.IsExported,
                    Module = null
                };
            }

            foreach (var impl in doc.Implementations)
            {
                var qName = impl.Name;
                implementations[qName] = new ImplementationEntry
                {
                    Id = SymbolId.Compute(qName),
                    InterfaceName = impl.InterfaceName,
                    IsExported = impl.IsExported,
                    Module = null,
                    Provider = impl.Provider
                };
            }

            foreach (var tmpl in doc.Templates)
            {
                var qName = tmpl.Name;
                templates[qName] = new TemplateEntry
                {
                    Id = SymbolId.Compute(qName),
                    Syntax = tmpl,
                    Module = null
                };
            }

            foreach (var shdr in doc.Shaders)
            {
                var qName = shdr.Name;
                shaders[qName] = new ShaderEntry
                {
                    Id = SymbolId.Compute(qName),
                    Syntax = shdr,
                    Module = null
                };
            }

            // Modules
            foreach (var mod in doc.Modules)
            {
                foreach (var iface in mod.Interfaces)
                {
                    var qName = $"{mod.Name}.{iface.Name}";
                    interfaces[qName] = new InterfaceEntry
                    {
                        Id = SymbolId.Compute(qName),
                        IsClosed = iface.IsClosed,
                        IsExported = iface.IsExported,
                        Module = mod.Name
                    };
                }

                foreach (var impl in mod.Implementations)
                {
                    var qName = $"{mod.Name}.{impl.Name}";
                    var ifaceQName = impl.InterfaceName;
                    if (!interfaces.ContainsKey(ifaceQName))
                    {
                        var match = interfaces.Keys.FirstOrDefault(k => k.EndsWith("." + impl.InterfaceName, StringComparison.Ordinal));
                        if (match != null)
                        {
                            ifaceQName = match;
                        }
                    }
                    implementations[qName] = new ImplementationEntry
                    {
                        Id = SymbolId.Compute(qName),
                        InterfaceName = ifaceQName,
                        IsExported = impl.IsExported,
                        Module = mod.Name,
                        Provider = impl.Provider
                    };
                }

                foreach (var tmpl in mod.Templates)
                {
                    var qName = $"{mod.Name}.{tmpl.Name}";
                    templates[qName] = new TemplateEntry
                    {
                        Id = SymbolId.Compute(qName),
                        Syntax = tmpl,
                        Module = mod.Name
                    };
                }

                foreach (var shdr in mod.Shaders)
                {
                    var qName = $"{mod.Name}.{shdr.Name}";
                    shaders[qName] = new ShaderEntry
                    {
                        Id = SymbolId.Compute(qName),
                        Syntax = shdr,
                        Module = mod.Name
                    };
                }
            }
        }

        // Layout template properties
        var templateSchemas = new Dictionary<string, PropertySchema>(StringComparer.Ordinal);
        foreach (var kvp in templates)
        {
            var qName = kvp.Key;
            var entry = kvp.Value;
            var propErrors = new List<DSLShaderError>();
            var schema = PropertyLayoutEngine.ComputeTemplateLayout(entry.Syntax, qName, propErrors);
            if (schema != null)
            {
                templateSchemas[qName] = schema;
            }
        }

        // Layout shader properties
        var shaderSchemas = new Dictionary<string, PropertySchema>(StringComparer.Ordinal);
        foreach (var kvp in shaders)
        {
            var qName = kvp.Key;
            var entry = kvp.Value;
            PropertySchema? baseSchema = null;
            if (!string.IsNullOrEmpty(entry.Syntax.TemplateName))
            {
                var templateName = entry.Syntax.TemplateName!;
                if (!templateSchemas.TryGetValue(templateName, out baseSchema))
                {
                    // Try to match template unqualified name
                    baseSchema = templateSchemas.Values.FirstOrDefault(t => t.TargetName.EndsWith(templateName, StringComparison.Ordinal));
                }
            }

            var propErrors = new List<DSLShaderError>();
            var schema = PropertyLayoutEngine.ComputeShaderLayout(entry.Syntax, qName, baseSchema, propErrors);
            if (schema != null)
            {
                shaderSchemas[qName] = schema;
            }
        }

        // 1. Generate GhostShaderCatalog.g.cs
        var catalogCode = GenerateCatalogSource(interfaces, implementations, shaders);
        context.AddSource("GhostShaderCatalog.g.cs", SourceText.From(catalogCode, Encoding.UTF8));

        // 2. Generate GhostShaderProperties.g.cs
        var propertiesCode = GeneratePropertiesSource(templateSchemas, shaderSchemas);
        context.AddSource("GhostShaderProperties.g.cs", SourceText.From(propertiesCode, Encoding.UTF8));
    }

    private static string GenerateCatalogSource(
        Dictionary<string, InterfaceEntry> interfaces,
        Dictionary<string, ImplementationEntry> implementations,
        Dictionary<string, ShaderEntry> shaders)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using Ghost.Core.Graphics;");
        sb.AppendLine();
        sb.AppendLine("namespace Ghost.Generated.Shaders");
        sb.AppendLine("{");
        sb.AppendLine("    public static class GhostShaderCatalog");
        sb.AppendLine("    {");

        // Interfaces
        sb.AppendLine("        public static class Interfaces");
        sb.AppendLine("        {");
        foreach (var kvp in interfaces)
        {
            var qName = kvp.Key;
            var entry = kvp.Value;
            var ident = SanitizeIdentifier(qName);
            sb.AppendLine($"            /// <summary>Interface {qName} (0x{entry.Id:X16})</summary>");
            sb.AppendLine($"            public readonly struct {ident} : IShaderInterfaceTag");
            sb.AppendLine("            {");
            sb.AppendLine($"                public static ShaderInterfaceId Id => new(0x{entry.Id:X16}UL);");
            sb.AppendLine("            }");
        }
        sb.AppendLine("        }");
        sb.AppendLine();

        // Implementations
        sb.AppendLine("        public static class Implementations");
        sb.AppendLine("        {");
        foreach (var kvp in implementations)
        {
            var qName = kvp.Key;
            var entry = kvp.Value;
            var ident = SanitizeIdentifier(qName);
            var ifaceIdent = SanitizeIdentifier(entry.InterfaceName);
            var providerComment = entry.Provider != null ? $" [Provider: {entry.Provider}]" : "";
            sb.AppendLine($"            /// <summary>Implementation {qName} : {entry.InterfaceName}{providerComment} (0x{entry.Id:X16})</summary>");
            sb.AppendLine($"            public readonly struct {ident} : IShaderImplementationTag<Interfaces.{ifaceIdent}>");
            sb.AppendLine("            {");
            sb.AppendLine($"                public static ShaderImplementationId Id => new(0x{entry.Id:X16}UL);");
            sb.AppendLine("            }");
        }
        sb.AppendLine("        }");
        sb.AppendLine();

        // Shaders
        sb.AppendLine("        public static class Shaders");
        sb.AppendLine("        {");
        foreach (var kvp in shaders)
        {
            var qName = kvp.Key;
            var entry = kvp.Value;
            var ident = SanitizeIdentifier(qName);
            var templateInfo = entry.Syntax.TemplateName != null ? $" : {entry.Syntax.TemplateName}" : "";
            sb.AppendLine($"            /// <summary>Shader {qName}{templateInfo} (0x{entry.Id:X16})</summary>");
            sb.AppendLine($"            public readonly struct {ident} : IShaderTag");
            sb.AppendLine("            {");
            sb.AppendLine($"                public static ShaderId Id => new(0x{entry.Id:X16}UL);");
            sb.AppendLine("            }");
        }
        sb.AppendLine("        }");

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GeneratePropertiesSource(
        Dictionary<string, PropertySchema> templateSchemas,
        Dictionary<string, PropertySchema> shaderSchemas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine("using Ghost.Core.Graphics;");
        sb.AppendLine("using Misaki.HighPerformance.Mathematics;");
        sb.AppendLine();
        sb.AppendLine("namespace Ghost.Generated.Shaders");
        sb.AppendLine("{");

        var allSchemas = templateSchemas.Concat(shaderSchemas).GroupBy(kv => kv.Key).Select(g => g.First());

        foreach (var kvp in allSchemas)
        {
            var qName = kvp.Key;
            var schema = kvp.Value;
            if (schema.Fields.Count == 0)
            {
                continue;
            }

            var structName = $"{SanitizeIdentifier(qName)}Properties";
            var size = schema.TotalSize;

            sb.AppendLine($"    /// <summary>Property struct for {qName} (SchemaId: 0x{schema.SchemaId:X16}, Size: {size} bytes)</summary>");
            sb.AppendLine($"    [StructLayout(LayoutKind.Explicit, Size = {size})]");
            sb.AppendLine($"    public struct {structName} : IShaderProperties");
            sb.AppendLine("    {");
            sb.AppendLine($"        public static ShaderId ShaderId => new(0x{schema.TargetId:X16}UL);");
            sb.AppendLine($"        public static ShaderPropertySchemaId SchemaId => new(0x{schema.SchemaId:X16}UL);");
            sb.AppendLine($"        public static uint PropertySize => {size};");
            sb.AppendLine();

            foreach (var field in schema.Fields)
            {
                var csType = ShaderPropertyTypeHelper.ToCSharpTypeName(field.Type);
                if (field.ArrayLength > 0)
                {
                    var elemSize = ShaderPropertyTypeHelper.GetSize(field.Type);
                    for (int i = 0; i < field.ArrayLength; i++)
                    {
                        var elemOffset = field.Offset + (uint)(i * elemSize);
                        sb.AppendLine($"        [FieldOffset({elemOffset})] public {csType} {field.Name}_{i};");
                    }
                    sb.AppendLine($"        public ReadOnlySpan<{csType}> {field.Name} => MemoryMarshal.CreateReadOnlySpan(ref {field.Name}_0, {field.ArrayLength});");
                }
                else
                {
                    sb.AppendLine($"        [FieldOffset({field.Offset})] public {csType} {field.Name};");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";
        var sb = new StringBuilder(name.Length);
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }
        return sb.ToString();
    }
}
