namespace Ghost.Ufbx
{
    public partial struct ufbx_baked_anim
    {
        public ufbx_baked_node_list nodes;

        public ufbx_baked_element_list elements;

        public double playback_time_begin;

        public double playback_time_end;

        public double playback_duration;

        public double key_time_min;

        public double key_time_max;

        public ufbx_baked_anim_metadata metadata;
    }
}
