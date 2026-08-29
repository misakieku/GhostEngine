namespace Ghost.Ufbx
{
    /// <include file='ufbx_procedural_geometry_list.xml' path='doc/member[@name="ufbx_procedural_geometry_list"]/*' />
    public unsafe partial struct ufbx_procedural_geometry_list
    {
        /// <include file='ufbx_procedural_geometry_list.xml' path='doc/member[@name="ufbx_procedural_geometry_list.data"]/*' />
        public ufbx_procedural_geometry** data;

        /// <include file='ufbx_procedural_geometry_list.xml' path='doc/member[@name="ufbx_procedural_geometry_list.count"]/*' />
        [NativeTypeName("size_t")]
        public nuint count;
    }
}
