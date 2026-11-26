namespace Ghost.DXC;

public enum DxcTranslationUnitFlags
{
    DxcTranslationUnitFlags_None = 0x0,
    DxcTranslationUnitFlags_DetailedPreprocessingRecord = 0x01,
    DxcTranslationUnitFlags_Incomplete = 0x02,
    DxcTranslationUnitFlags_PrecompiledPreamble = 0x04,
    DxcTranslationUnitFlags_CacheCompletionResults = 0x08,
    DxcTranslationUnitFlags_ForSerialization = 0x10,
    DxcTranslationUnitFlags_CXXChainedPCH = 0x20,
    DxcTranslationUnitFlags_SkipFunctionBodies = 0x40,
    DxcTranslationUnitFlags_IncludeBriefCommentsInCodeCompletion = 0x80,
    DxcTranslationUnitFlags_UseCallerThread = 0x800,
}
