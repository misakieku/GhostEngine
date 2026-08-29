namespace Ghost.Ufbx
{
    /// <include file='ufbx_constraint_target.xml' path='doc/member[@name="ufbx_constraint_target"]/*' />
    public unsafe partial struct ufbx_constraint_target
    {
        /// <include file='ufbx_constraint_target.xml' path='doc/member[@name="ufbx_constraint_target.node"]/*' />
        public ufbx_node* node;

        /// <include file='ufbx_constraint_target.xml' path='doc/member[@name="ufbx_constraint_target.weight"]/*' />
        [NativeTypeName("ufbx_real")]
        public float weight;

        /// <include file='ufbx_constraint_target.xml' path='doc/member[@name="ufbx_constraint_target.transform"]/*' />
        public ufbx_transform transform;
    }
}
