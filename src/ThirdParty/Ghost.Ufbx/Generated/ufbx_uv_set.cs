namespace Ghost.Ufbx
{
    /// <include file='ufbx_uv_set.xml' path='doc/member[@name="ufbx_uv_set"]/*' />
    public partial struct ufbx_uv_set
    {
        /// <include file='ufbx_uv_set.xml' path='doc/member[@name="ufbx_uv_set.name"]/*' />
        public ufbx_string name;

        /// <include file='ufbx_uv_set.xml' path='doc/member[@name="ufbx_uv_set.index"]/*' />
        [NativeTypeName("uint32_t")]
        public uint index;

        /// <include file='ufbx_uv_set.xml' path='doc/member[@name="ufbx_uv_set.vertex_uv"]/*' />
        public ufbx_vertex_vec2 vertex_uv;

        /// <include file='ufbx_uv_set.xml' path='doc/member[@name="ufbx_uv_set.vertex_tangent"]/*' />
        public ufbx_vertex_vec3 vertex_tangent;

        /// <include file='ufbx_uv_set.xml' path='doc/member[@name="ufbx_uv_set.vertex_bitangent"]/*' />
        public ufbx_vertex_vec3 vertex_bitangent;
    }
}
