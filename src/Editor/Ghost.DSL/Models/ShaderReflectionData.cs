namespace Ghost.DSL.Models;

public class ShaderPropertyFieldInfo
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int Offset { get; set; }
}

public class ShaderReflectionData
{
    public string ShaderName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public uint Size { get; set; }

    public ShaderPropertyFieldInfo[] Fields { get; set; } = Array.Empty<ShaderPropertyFieldInfo>();
}