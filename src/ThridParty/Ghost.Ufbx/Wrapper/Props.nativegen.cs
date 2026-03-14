namespace Ghost.Ufbx;

public unsafe ref struct Props
{
    private ufbx_props* _ptr;

    internal Props(ufbx_props* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public Prop FindProp(ReadOnlySpan<byte> name)
    {
        fixed (byte* namePtr = name)
        {
        var value = Api.ufbx_find_prop_len(_ptr, (sbyte*)namePtr, (nuint)name.Length);
        return new(value);
        }
    }

    public Prop FindProp(sbyte* name)
    {
        return new(Api.ufbx_find_prop(_ptr, name));
    }

    public float FindRealLen(sbyte* name, nuint nameLen, float def)
    {
        return Api.ufbx_find_real_len(_ptr, name, nameLen, def);
    }

    public float FindReal(sbyte* name, float def)
    {
        return Api.ufbx_find_real(_ptr, name, def);
    }

    public Misaki.HighPerformance.Mathematics.float3 FindVec3Len(sbyte* name, nuint nameLen, Misaki.HighPerformance.Mathematics.float3 def)
    {
        return Api.ufbx_find_vec3_len(_ptr, name, nameLen, def);
    }

    public Misaki.HighPerformance.Mathematics.float3 FindVec3(sbyte* name, Misaki.HighPerformance.Mathematics.float3 def)
    {
        return Api.ufbx_find_vec3(_ptr, name, def);
    }

    public long FindIntLen(sbyte* name, nuint nameLen, long def)
    {
        return Api.ufbx_find_int_len(_ptr, name, nameLen, def);
    }

    public long FindInt(sbyte* name, long def)
    {
        return Api.ufbx_find_int(_ptr, name, def);
    }

    public bool FindBoolLen(sbyte* name, nuint nameLen, bool def)
    {
        return Api.ufbx_find_bool_len(_ptr, name, nameLen, def);
    }

    public bool FindBool(sbyte* name, bool def)
    {
        return Api.ufbx_find_bool(_ptr, name, def);
    }

    public string FindStringLen(sbyte* name, nuint nameLen, ufbx_string def)
    {
        return NativeWrapperHelpers.GetString(Api.ufbx_find_string_len(_ptr, name, nameLen, def));
    }

    public string FindString(sbyte* name, ufbx_string def)
    {
        return NativeWrapperHelpers.GetString(Api.ufbx_find_string(_ptr, name, def));
    }

    public ReadOnlySpan<byte> FindBlobLen(sbyte* name, nuint nameLen, ufbx_blob def)
    {
        return NativeWrapperHelpers.AsSpan(Api.ufbx_find_blob_len(_ptr, name, nameLen, def));
    }

    public ReadOnlySpan<byte> FindBlob(sbyte* name, ufbx_blob def)
    {
        return NativeWrapperHelpers.AsSpan(Api.ufbx_find_blob(_ptr, name, def));
    }

    public Prop FindPropConcat(UfbxString parts, nuint numParts)
    {
        return new(Api.ufbx_find_prop_concat(_ptr, parts.GetUnsafePtr(), numParts));
    }

    public ReadOnlySpan<ufbx_prop> PropsValue => _ptr->props.data == null ? ReadOnlySpan<ufbx_prop>.Empty : new ReadOnlySpan<ufbx_prop>(_ptr->props.data, checked((int)_ptr->props.count));

    public nuint NumAnimated => _ptr->num_animated;

    public bool HasDefaults => _ptr->defaults != null;
    public Props Defaults => _ptr->defaults != null ? new(_ptr->defaults) : throw new InvalidOperationException("Defaults is null.");

    internal ufbx_props* GetUnsafePtr() => _ptr;
}
