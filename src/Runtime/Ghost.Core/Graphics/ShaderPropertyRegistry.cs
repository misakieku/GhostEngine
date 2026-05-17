namespace Ghost.Core.Graphics;

public enum ShaderPropertyType
{
    Unknown,
    Float,
    Float2,
    Float3,
    Float4,
    Int,
    UInt,
    Float4x4,
    Texture2D,
    Texture3D,
    Buffer
}

public readonly struct ShaderPropertyFieldInfo
{
    public string Name
    {
        get; init;
    }

    public ShaderPropertyType Type
    {
        get; init;
    }

    public int Offset
    {
        get; init;
    }
}

#if GHOST_SAFETY_CHECKS
public struct ShaderPropertyInfo
{
    public string ShaderName
    {
        get; init;
    }

    public string Code
    {
        get; init;
    }

    public uint Size
    {
        get; init;
    }

    public ShaderPropertyFieldInfo[] Fields
    {
        get; init;
    }
}

public static class ShaderPropertiesRegistry
{
    private static readonly Dictionary<string, ShaderPropertyInfo> s_nameToCode = new Dictionary<string, ShaderPropertyInfo>(StringComparer.Ordinal);

    public static void Register(string name, string code, uint size, ShaderPropertyFieldInfo[] fields)
    {
        s_nameToCode[name] = new ShaderPropertyInfo { ShaderName = name, Code = code, Size = size, Fields = fields };
    }

    public static bool TryGetInfo(string name, out ShaderPropertyInfo info)
    {
        return s_nameToCode.TryGetValue(name, out info);
    }
}
#endif
