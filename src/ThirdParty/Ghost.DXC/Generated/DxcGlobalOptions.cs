namespace Ghost.DXC;

public enum DxcGlobalOptions
{
    DxcGlobalOpt_None = 0x0,
    DxcGlobalOpt_ThreadBackgroundPriorityForIndexing = 0x1,
    DxcGlobalOpt_ThreadBackgroundPriorityForEditing = 0x2,
    DxcGlobalOpt_ThreadBackgroundPriorityForAll = DxcGlobalOpt_ThreadBackgroundPriorityForIndexing | DxcGlobalOpt_ThreadBackgroundPriorityForEditing,
}
