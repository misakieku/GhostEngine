namespace Ghost.Ufbx;

public unsafe struct MaterialMap
{
    private ufbx_material_map* _ptr;

    internal MaterialMap(ufbx_material_map* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public long ValueInt => _ptr->value_int;

    public bool HasTexture => _ptr->texture != null;
    public Texture Texture => _ptr->texture != null ? new(_ptr->texture) : throw new InvalidOperationException("Texture is null.");

    public bool HasValue => _ptr->has_value;

    public bool TextureEnabled => _ptr->texture_enabled;

    public bool FeatureDisabled => _ptr->feature_disabled;

    public byte ValueComponents => _ptr->value_components;

    public float ValueReal => _ptr->value_real;

    public Misaki.HighPerformance.Mathematics.float2 ValueVec2 => _ptr->value_vec2;

    public Misaki.HighPerformance.Mathematics.float3 ValueVec3 => _ptr->value_vec3;

    public Misaki.HighPerformance.Mathematics.float4 ValueVec4 => _ptr->value_vec4;

    internal ufbx_material_map* GetUnsafePtr() => _ptr;
}
