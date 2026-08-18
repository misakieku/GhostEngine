namespace Ghost.DSL.Properties;

public class PropertyFieldLayout
{
    public string Name { get; set; } = string.Empty;
    public ShaderPropertyType Type { get; set; }
    public uint Offset { get; set; }
    public uint Size { get; set; }
    public uint Alignment { get; set; }
    public int ArrayLength { get; set; }
    public bool IsInherited { get; set; }
    public string? DeclaringTypeName { get; set; }
}
