namespace Ghost.Ufbx
{
    public unsafe partial struct ufbx_constraint_target
    {
        public ufbx_node* node;

        [NativeTypeName("ufbx_real")]
        public float weight;

        public ufbx_transform transform;
    }
}
