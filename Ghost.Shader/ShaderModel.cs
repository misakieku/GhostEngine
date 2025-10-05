namespace Ghost.Shader;

public enum ShaderPropertyType
{
    None,
    Float, Float2, Float3, Float4,
    Int, Int2, Int3, Int4,
    UInt, UInt2, UInt3, UInt4,
    Bool, Bool2, Bool3, Bool4,
    Texture2D, Texture3D, TextureCube,
}

internal class PropertyModel
{
    public ShaderPropertyType type;
    public string name = string.Empty;
    public object? defaultValue;
}

internal class PipelineStateModel
{
    public ZTestOptions zTest;
    public ZWriteOptions zWrite;
    public CullOptions cull;
    public BlendOptions blend;
    public uint colorMask;
}

internal class ShaderModel
{
    public string name = string.Empty;
    public List<PropertyModel> properties = new();
    public PipelineStateModel pipeline = new();
}
