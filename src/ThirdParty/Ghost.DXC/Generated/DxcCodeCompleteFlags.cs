namespace Ghost.DXC;

public enum DxcCodeCompleteFlags
{
    DxcCodeCompleteFlags_None = 0,
    DxcCodeCompleteFlags_IncludeMacros = 0x1,
    DxcCodeCompleteFlags_IncludeCodePatterns = 0x2,
    DxcCodeCompleteFlags_IncludeBriefComments = 0x4,
}
