namespace Ghost.Ufbx;

public unsafe struct BakedNode
{
    private ufbx_baked_node* _ptr;

    internal BakedNode(ufbx_baked_node* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint TypedId => _ptr->typed_id;

    public uint ElementId => _ptr->element_id;

    public bool ConstantTranslation => _ptr->constant_translation;

    public bool ConstantRotation => _ptr->constant_rotation;

    public bool ConstantScale => _ptr->constant_scale;

    public ReadOnlySpan<ufbx_baked_vec3> TranslationKeys => _ptr->translation_keys.data == null ? ReadOnlySpan<ufbx_baked_vec3>.Empty : new ReadOnlySpan<ufbx_baked_vec3>(_ptr->translation_keys.data, checked((int)_ptr->translation_keys.count));

    public ReadOnlySpan<ufbx_baked_quat> RotationKeys => _ptr->rotation_keys.data == null ? ReadOnlySpan<ufbx_baked_quat>.Empty : new ReadOnlySpan<ufbx_baked_quat>(_ptr->rotation_keys.data, checked((int)_ptr->rotation_keys.count));

    public ReadOnlySpan<ufbx_baked_vec3> ScaleKeys => _ptr->scale_keys.data == null ? ReadOnlySpan<ufbx_baked_vec3>.Empty : new ReadOnlySpan<ufbx_baked_vec3>(_ptr->scale_keys.data, checked((int)_ptr->scale_keys.count));

    internal ufbx_baked_node* GetUnsafePtr() => _ptr;
}
