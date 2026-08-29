namespace Ghost.Ufbx
{
    /// <include file='ufbx_color_set.xml' path='doc/member[@name="ufbx_color_set"]/*' />
    public partial struct ufbx_color_set
    {
        /// <include file='ufbx_color_set.xml' path='doc/member[@name="ufbx_color_set.name"]/*' />
        public ufbx_string name;

        /// <include file='ufbx_color_set.xml' path='doc/member[@name="ufbx_color_set.index"]/*' />
        [NativeTypeName("uint32_t")]
        public uint index;

        /// <include file='ufbx_color_set.xml' path='doc/member[@name="ufbx_color_set.vertex_color"]/*' />
        public ufbx_vertex_vec4 vertex_color;
    }
}
