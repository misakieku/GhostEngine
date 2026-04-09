namespace Ghost.Core.Graphics;

[AttributeUsage(AttributeTargets.Struct)]
public class GenerateShaderPropertyAttribute : Attribute
{
    public GenerateShaderPropertyAttribute(string shaderName, string? name = null)
    {
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class GenerateAsHLSLTypeAttribute : Attribute
{
    public GenerateAsHLSLTypeAttribute(string hlslTypeName)
    {
    }
}

#if DEBUG || GHOST_EDITOR
public struct ShaderPropertyInfo
{
    public string shaderName;
    public string code;
    public uint size;
}

public static class ShaderPropertiesRegistry
{
    private static readonly Dictionary<string, ShaderPropertyInfo> s_nameToCode = new Dictionary<string, ShaderPropertyInfo>(StringComparer.Ordinal);

    public static void Register(string name, string code, uint size)
    {
        s_nameToCode[name] = new ShaderPropertyInfo { shaderName = name, code = code, size = size };
    }

    public static bool TryGetInfo(string name, out ShaderPropertyInfo info)
    {
        return s_nameToCode.TryGetValue(name, out info);
    }
}
#endif