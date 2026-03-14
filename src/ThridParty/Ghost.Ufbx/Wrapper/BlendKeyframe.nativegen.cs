namespace Ghost.Ufbx;

public unsafe struct BlendKeyframe
{
    private ufbx_blend_keyframe* _ptr;

    internal BlendKeyframe(ufbx_blend_keyframe* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool HasShape => _ptr->shape != null;
    public BlendShape Shape => _ptr->shape != null ? new(_ptr->shape) : throw new InvalidOperationException("Shape is null.");

    public float TargetWeight => _ptr->target_weight;

    public float EffectiveWeight => _ptr->effective_weight;

    internal ufbx_blend_keyframe* GetUnsafePtr() => _ptr;
}
