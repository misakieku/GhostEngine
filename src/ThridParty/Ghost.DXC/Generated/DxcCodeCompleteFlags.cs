namespace Ghost.DXC;

/// <include file='DxcCodeCompleteFlags.xml' path='doc/member[@name="DxcCodeCompleteFlags"]/*' />
public enum DxcCodeCompleteFlags
{
    /// <include file='DxcCodeCompleteFlags.xml' path='doc/member[@name="DxcCodeCompleteFlags.DxcCodeCompleteFlags_None"]/*' />
    DxcCodeCompleteFlags_None = 0,

    /// <include file='DxcCodeCompleteFlags.xml' path='doc/member[@name="DxcCodeCompleteFlags.DxcCodeCompleteFlags_IncludeMacros"]/*' />
    DxcCodeCompleteFlags_IncludeMacros = 0x1,

    /// <include file='DxcCodeCompleteFlags.xml' path='doc/member[@name="DxcCodeCompleteFlags.DxcCodeCompleteFlags_IncludeCodePatterns"]/*' />
    DxcCodeCompleteFlags_IncludeCodePatterns = 0x2,

    /// <include file='DxcCodeCompleteFlags.xml' path='doc/member[@name="DxcCodeCompleteFlags.DxcCodeCompleteFlags_IncludeBriefComments"]/*' />
    DxcCodeCompleteFlags_IncludeBriefComments = 0x4,
}
