namespace Ghost.Ufbx;

public unsafe ref struct Prop
{
    private ufbx_prop* _ptr;

    internal Prop(ufbx_prop* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public ufbx_prop_type Type => _ptr->type;

    public ufbx_prop_flags Flags => _ptr->flags;

    public ReadOnlySpan<byte> ValueStrBytes => NativeWrapperHelpers.AsByteSpan(_ptr->value_str);
    public string ValueStr => NativeWrapperHelpers.GetString(_ptr->value_str);

    public ReadOnlySpan<byte> ValueBlob => NativeWrapperHelpers.AsSpan(_ptr->value_blob);

    public long ValueInt => _ptr->value_int;

    public float ValueReal => _ptr->value_real;

    public Misaki.HighPerformance.Mathematics.float2 ValueVec2 => _ptr->value_vec2;

    public Misaki.HighPerformance.Mathematics.float3 ValueVec3 => _ptr->value_vec3;

    public Misaki.HighPerformance.Mathematics.float4 ValueVec4 => _ptr->value_vec4;

    internal ufbx_prop* GetUnsafePtr() => _ptr;
}
