namespace Ghost.Ufbx;

public unsafe struct MaterialFeatureInfo
{
    private ufbx_material_feature_info* _ptr;

    internal MaterialFeatureInfo(ufbx_material_feature_info* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool Enabled => _ptr->enabled;

    public bool IsExplicit => _ptr->is_explicit;

    internal ufbx_material_feature_info* GetUnsafePtr() => _ptr;
}
