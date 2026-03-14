namespace Ghost.Ufbx
{
    public enum ufbx_constraint_type
    {
        UFBX_CONSTRAINT_UNKNOWN,
        UFBX_CONSTRAINT_AIM,
        UFBX_CONSTRAINT_PARENT,
        UFBX_CONSTRAINT_POSITION,
        UFBX_CONSTRAINT_ROTATION,
        UFBX_CONSTRAINT_SCALE,
        UFBX_CONSTRAINT_SINGLE_CHAIN_IK,
        UFBX_CONSTRAINT_TYPE_FORCE_32BIT = 0x7fffffff,
    }
}
