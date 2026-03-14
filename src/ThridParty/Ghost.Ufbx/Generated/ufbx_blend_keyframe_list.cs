namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_blend_keyframe_list
    {
        public ufbx_blend_keyframe* data;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
