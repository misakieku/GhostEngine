namespace Ghost.Ufbx
{
    public enum ufbx_bake_step_handling
    {
        UFBX_BAKE_STEP_HANDLING_DEFAULT,
        UFBX_BAKE_STEP_HANDLING_CUSTOM_DURATION,
        UFBX_BAKE_STEP_HANDLING_IDENTICAL_TIME,
        UFBX_BAKE_STEP_HANDLING_ADJACENT_DOUBLE,
        UFBX_BAKE_STEP_HANDLING_IGNORE,
        ufbx_bake_step_handling_FORCE_32BIT = 0x7fffffff,
    }
}
