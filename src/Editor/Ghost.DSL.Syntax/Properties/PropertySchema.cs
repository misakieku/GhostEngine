using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ghost.DSL.Properties;

public class PropertySchema
{
    public ulong SchemaId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public ulong TargetId { get; set; }
    public ulong? BaseTemplateId { get; set; }
    public ulong? BaseTemplateSchemaId { get; set; }
    public uint TotalSize { get; set; }
    public List<PropertyFieldLayout> Fields { get; set; } = new();

    public IEnumerable<PropertyFieldLayout> TemplateFields => Fields.Where(f => f.IsInherited);
    public IEnumerable<PropertyFieldLayout> DerivedFields => Fields.Where(f => !f.IsInherited);

    public string GenerateHlslStruct(string structName = "MaterialProperties")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"struct {structName}");
        sb.AppendLine("{");
        if (Fields.Count == 0)
        {
            sb.AppendLine("    uint _dummy;");
        }
        else
        {
            foreach (var field in Fields)
            {
                var hlslType = ShaderPropertyTypeHelper.ToHlslTypeName(field.Type);
                var arraySuffix = field.ArrayLength > 0 ? $"[{field.ArrayLength}]" : "";
                sb.AppendLine($"    {hlslType} {field.Name}{arraySuffix}; // offset: {field.Offset}, size: {field.Size}");
            }
        }
        sb.AppendLine("};");
        return sb.ToString();
    }
}
