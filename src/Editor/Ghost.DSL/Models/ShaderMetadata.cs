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

public class ShaderMetadata
{
    public Dictionary<string, ShaderReflectionData> ReflectionDatas { get; set; } = new Dictionary<string, ShaderReflectionData>(StringComparer.Ordinal);
    public Dictionary<string, string> VirtualShader { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public void Merge(ShaderMetadata other)
    {
        foreach (var kvp in other.ReflectionDatas)
        {
            ReflectionDatas[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in other.VirtualShader)
        {
            VirtualShader[kvp.Key] = kvp.Value;
        }
    }
}