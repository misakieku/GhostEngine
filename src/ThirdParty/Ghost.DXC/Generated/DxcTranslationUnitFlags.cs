namespace Ghost.DXC;

/// <include file='DxcTranslationUnitFlags.xml' path='doc/member[@name="DxcTranslationUnitFlags"]/*' />
public enum DxcTranslationUnitFlags
{
    /// <include file='DxcTranslationUnitFlags.xml' path='doc/member[@name="DxcTranslationUnitFlags.DxcTranslationUnitFlags_None"]/*' />
    DxcTranslationUnitFlags_None = 0x0,

    /// <include file='DxcTranslationUnitFlags.xml' path='doc/member[@name="DxcTranslationUnitFlags.DxcTranslationUnitFlags_DetailedPreprocessingRecord"]/*' />
    DxcTranslationUnitFlags_DetailedPreprocessingRecord = 0x01,

    /// <include file='DxcTranslationUnitFlags.xml' path='doc/member[@name="DxcTranslationUnitFlags.DxcTranslationUnitFlags_Incomplete"]/*' />
    DxcTranslationUnitFlags_Incomplete = 0x02,

    /// <include file='DxcTranslationUnitFlags.xml' path='doc/member[@name="DxcTranslationUnitFlags.DxcTranslationUnitFlags_PrecompiledPreamble"]/*' />
    DxcTranslationUnitFlags_PrecompiledPreamble = 0x04,

    /// <include file='DxcTranslationUnitFlags.xml' path='doc/member[@name="DxcTranslationUnitFlags.DxcTranslationUnitFlags_CacheCompletionResults"]/*' />
    DxcTranslationUnitFlags_CacheCompletionResults = 0x08,

    /// <include file='DxcTranslationUnitFlags.xml' path='doc/member[@name="DxcTranslationUnitFlags.DxcTranslationUnitFlags_ForSerialization"]/*' />
    DxcTranslationUnitFlags_ForSerialization = 0x10,

    /// <include file='DxcTranslationUnitFlags.xml' path='doc/member[@name="DxcTranslationUnitFlags.DxcTranslationUnitFlags_CXXChainedPCH"]/*' />
    DxcTranslationUnitFlags_CXXChainedPCH = 0x20,

    /// <include file='DxcTranslationUnitFlags.xml' path='doc/member[@name="DxcTranslationUnitFlags.DxcTranslationUnitFlags_SkipFunctionBodies"]/*' />
    DxcTranslationUnitFlags_SkipFunctionBodies = 0x40,

    /// <include file='DxcTranslationUnitFlags.xml' path='doc/member[@name="DxcTranslationUnitFlags.DxcTranslationUnitFlags_IncludeBriefCommentsInCodeCompletion"]/*' />
    DxcTranslationUnitFlags_IncludeBriefCommentsInCodeCompletion = 0x80,

    /// <include file='DxcTranslationUnitFlags.xml' path='doc/member[@name="DxcTranslationUnitFlags.DxcTranslationUnitFlags_UseCallerThread"]/*' />
    DxcTranslationUnitFlags_UseCallerThread = 0x800,
}
