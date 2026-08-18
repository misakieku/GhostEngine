using System;
using System.Collections.Generic;
using System.Linq;
using Ghost.DSL.Parser;
using Ghost.DSL.ShaderParser.Syntax;
using Ghost.DSL.Syntax.Symbols;

namespace Ghost.DSL.Properties;

public static class PropertyLayoutEngine
{
    public static PropertySchema? ComputeTemplateLayout(
        TemplateDeclarationSyntax templateSyntax,
        string templateQualifiedName,
        List<DSLShaderError> errors)
    {
        var schema = new PropertySchema
        {
            TargetName = templateQualifiedName,
            TargetId = SymbolId.Compute(templateQualifiedName)
        };

        if (templateSyntax.Properties == null || templateSyntax.Properties.Declarations.Count == 0)
        {
            schema.TotalSize = 0;
            schema.SchemaId = ComputeSchemaId(schema.TargetId, null, schema.Fields);
            return schema;
        }

        uint currentOffset = 0;
        foreach (var decl in templateSyntax.Properties.Declarations)
        {
            if (decl.TypeName.Equals("bool", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new DSLShaderError
                {
                    Message = $"'bool' is forbidden in shader properties. Use 'uint' or 'int' instead (field: '{decl.Name}').",
                    Line = decl.Line,
                    Column = decl.Column
                });
                continue;
            }

            if (!ShaderPropertyTypeHelper.TryParse(decl.TypeName, out var propType))
            {
                errors.Add(new DSLShaderError
                {
                    Message = $"Unknown shader property type '{decl.TypeName}' for field '{decl.Name}'.",
                    Line = decl.Line,
                    Column = decl.Column
                });
                continue;
            }

            var elemSize = ShaderPropertyTypeHelper.GetSize(propType);
            var elemAlign = ShaderPropertyTypeHelper.GetAlignment(propType);

            uint size;
            uint alignment;
            if (decl.ArrayLength > 0)
            {
                size = (uint)(elemSize * decl.ArrayLength);
                alignment = Math.Max(16u, elemAlign); // Arrays in HLSL aligned to 16 bytes
            }
            else
            {
                size = elemSize;
                alignment = elemAlign;
            }

            var offset = (currentOffset + alignment - 1) & ~(alignment - 1);

            schema.Fields.Add(new PropertyFieldLayout
            {
                Name = decl.Name,
                Type = propType,
                Offset = offset,
                Size = size,
                Alignment = alignment,
                ArrayLength = decl.ArrayLength,
                IsInherited = false,
                DeclaringTypeName = templateQualifiedName
            });

            currentOffset = offset + size;
        }

        schema.TotalSize = currentOffset == 0 ? 0 : (currentOffset + 15) & ~15u;
        schema.SchemaId = ComputeSchemaId(schema.TargetId, null, schema.Fields);
        return schema;
    }

    public static PropertySchema? ComputeShaderLayout(
        ShaderDeclarationSyntax shaderSyntax,
        string shaderQualifiedName,
        PropertySchema? templateSchema,
        List<DSLShaderError> errors)
    {
        var schema = new PropertySchema
        {
            TargetName = shaderQualifiedName,
            TargetId = SymbolId.Compute(shaderQualifiedName),
            BaseTemplateId = templateSchema?.TargetId,
            BaseTemplateSchemaId = templateSchema?.SchemaId
        };

        // 1. Copy template fields as prefix
        uint currentOffset = 0;
        if (templateSchema != null && templateSchema.Fields.Count > 0)
        {
            foreach (var tf in templateSchema.Fields)
            {
                schema.Fields.Add(new PropertyFieldLayout
                {
                    Name = tf.Name,
                    Type = tf.Type,
                    Offset = tf.Offset,
                    Size = tf.Size,
                    Alignment = tf.Alignment,
                    ArrayLength = tf.ArrayLength,
                    IsInherited = true,
                    DeclaringTypeName = tf.DeclaringTypeName ?? templateSchema.TargetName
                });
            }

            currentOffset = templateSchema.Fields.Max(f => f.Offset + f.Size);
        }

        // 2. Append derived shader properties
        if (shaderSyntax.Properties != null)
        {
            foreach (var decl in shaderSyntax.Properties.Declarations)
            {
                if (decl.TypeName.Equals("bool", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(new DSLShaderError
                    {
                        Message = $"'bool' is forbidden in shader properties. Use 'uint' or 'int' instead (field: '{decl.Name}').",
                        Line = decl.Line,
                        Column = decl.Column
                    });
                    continue;
                }

                if (!ShaderPropertyTypeHelper.TryParse(decl.TypeName, out var propType))
                {
                    errors.Add(new DSLShaderError
                    {
                        Message = $"Unknown shader property type '{decl.TypeName}' for field '{decl.Name}'.",
                        Line = decl.Line,
                        Column = decl.Column
                    });
                    continue;
                }

                // Check for name shadowing with template fields
                if (schema.Fields.Any(f => f.Name == decl.Name))
                {
                    errors.Add(new DSLShaderError
                    {
                        Message = $"Property '{decl.Name}' in shader '{shaderQualifiedName}' shadows an inherited property.",
                        Line = decl.Line,
                        Column = decl.Column
                    });
                }

                var elemSize = ShaderPropertyTypeHelper.GetSize(propType);
                var elemAlign = ShaderPropertyTypeHelper.GetAlignment(propType);

                uint size;
                uint alignment;
                if (decl.ArrayLength > 0)
                {
                    size = (uint)(elemSize * decl.ArrayLength);
                    alignment = Math.Max(16u, elemAlign);
                }
                else
                {
                    size = elemSize;
                    alignment = elemAlign;
                }

                var offset = (currentOffset + alignment - 1) & ~(alignment - 1);

                schema.Fields.Add(new PropertyFieldLayout
                {
                    Name = decl.Name,
                    Type = propType,
                    Offset = offset,
                    Size = size,
                    Alignment = alignment,
                    ArrayLength = decl.ArrayLength,
                    IsInherited = false,
                    DeclaringTypeName = shaderQualifiedName
                });

                currentOffset = offset + size;
            }
        }

        schema.TotalSize = currentOffset == 0 ? 0 : (currentOffset + 15) & ~15u;
        schema.SchemaId = ComputeSchemaId(schema.TargetId, schema.BaseTemplateSchemaId, schema.Fields);
        return schema;
    }

    public static ulong ComputeSchemaId(ulong targetId, ulong? baseTemplateSchemaId, IReadOnlyList<PropertyFieldLayout> fields)
    {
        ulong hash = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        // Hash target ID
        hash ^= targetId;
        hash *= prime;

        // Hash base template schema ID
        if (baseTemplateSchemaId.HasValue)
        {
            hash ^= baseTemplateSchemaId.Value;
            hash *= prime;
        }

        // Hash fields
        foreach (var f in fields)
        {
            foreach (char c in f.Name)
            {
                hash ^= c;
                hash *= prime;
            }

            hash ^= (ulong)f.Type;
            hash *= prime;

            hash ^= f.Offset;
            hash *= prime;

            hash ^= f.Size;
            hash *= prime;

            hash ^= (ulong)f.ArrayLength;
            hash *= prime;
        }

        return hash;
    }
}
