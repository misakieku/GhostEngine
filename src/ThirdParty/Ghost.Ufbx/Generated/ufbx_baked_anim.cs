namespace Ghost.Ufbx
{
    /// <include file='ufbx_baked_anim.xml' path='doc/member[@name="ufbx_baked_anim"]/*' />
    public partial struct ufbx_baked_anim
    {
        /// <include file='ufbx_baked_anim.xml' path='doc/member[@name="ufbx_baked_anim.nodes"]/*' />
        public ufbx_baked_node_list nodes;

        /// <include file='ufbx_baked_anim.xml' path='doc/member[@name="ufbx_baked_anim.elements"]/*' />
        public ufbx_baked_element_list elements;

        /// <include file='ufbx_baked_anim.xml' path='doc/member[@name="ufbx_baked_anim.playback_time_begin"]/*' />
        public double playback_time_begin;

        /// <include file='ufbx_baked_anim.xml' path='doc/member[@name="ufbx_baked_anim.playback_time_end"]/*' />
        public double playback_time_end;

        /// <include file='ufbx_baked_anim.xml' path='doc/member[@name="ufbx_baked_anim.playback_duration"]/*' />
        public double playback_duration;

        /// <include file='ufbx_baked_anim.xml' path='doc/member[@name="ufbx_baked_anim.key_time_min"]/*' />
        public double key_time_min;

        /// <include file='ufbx_baked_anim.xml' path='doc/member[@name="ufbx_baked_anim.key_time_max"]/*' />
        public double key_time_max;

        /// <include file='ufbx_baked_anim.xml' path='doc/member[@name="ufbx_baked_anim.metadata"]/*' />
        public ufbx_baked_anim_metadata metadata;
    }
}
