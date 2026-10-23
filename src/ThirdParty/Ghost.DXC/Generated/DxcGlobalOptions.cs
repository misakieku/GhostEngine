namespace Ghost.DXC;

/// <include file='DxcGlobalOptions.xml' path='doc/member[@name="DxcGlobalOptions"]/*' />
public enum DxcGlobalOptions
{
    /// <include file='DxcGlobalOptions.xml' path='doc/member[@name="DxcGlobalOptions.DxcGlobalOpt_None"]/*' />
    DxcGlobalOpt_None = 0x0,

    /// <include file='DxcGlobalOptions.xml' path='doc/member[@name="DxcGlobalOptions.DxcGlobalOpt_ThreadBackgroundPriorityForIndexing"]/*' />
    DxcGlobalOpt_ThreadBackgroundPriorityForIndexing = 0x1,

    /// <include file='DxcGlobalOptions.xml' path='doc/member[@name="DxcGlobalOptions.DxcGlobalOpt_ThreadBackgroundPriorityForEditing"]/*' />
    DxcGlobalOpt_ThreadBackgroundPriorityForEditing = 0x2,

    /// <include file='DxcGlobalOptions.xml' path='doc/member[@name="DxcGlobalOptions.DxcGlobalOpt_ThreadBackgroundPriorityForAll"]/*' />
    DxcGlobalOpt_ThreadBackgroundPriorityForAll = DxcGlobalOpt_ThreadBackgroundPriorityForIndexing | DxcGlobalOpt_ThreadBackgroundPriorityForEditing,
}
