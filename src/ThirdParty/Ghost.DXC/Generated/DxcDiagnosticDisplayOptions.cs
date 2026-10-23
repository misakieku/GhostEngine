namespace Ghost.DXC;

/// <include file='DxcDiagnosticDisplayOptions.xml' path='doc/member[@name="DxcDiagnosticDisplayOptions"]/*' />
public enum DxcDiagnosticDisplayOptions
{
    /// <include file='DxcDiagnosticDisplayOptions.xml' path='doc/member[@name="DxcDiagnosticDisplayOptions.DxcDiagnostic_DisplaySourceLocation"]/*' />
    DxcDiagnostic_DisplaySourceLocation = 0x01,

    /// <include file='DxcDiagnosticDisplayOptions.xml' path='doc/member[@name="DxcDiagnosticDisplayOptions.DxcDiagnostic_DisplayColumn"]/*' />
    DxcDiagnostic_DisplayColumn = 0x02,

    /// <include file='DxcDiagnosticDisplayOptions.xml' path='doc/member[@name="DxcDiagnosticDisplayOptions.DxcDiagnostic_DisplaySourceRanges"]/*' />
    DxcDiagnostic_DisplaySourceRanges = 0x04,

    /// <include file='DxcDiagnosticDisplayOptions.xml' path='doc/member[@name="DxcDiagnosticDisplayOptions.DxcDiagnostic_DisplayOption"]/*' />
    DxcDiagnostic_DisplayOption = 0x08,

    /// <include file='DxcDiagnosticDisplayOptions.xml' path='doc/member[@name="DxcDiagnosticDisplayOptions.DxcDiagnostic_DisplayCategoryId"]/*' />
    DxcDiagnostic_DisplayCategoryId = 0x10,

    /// <include file='DxcDiagnosticDisplayOptions.xml' path='doc/member[@name="DxcDiagnosticDisplayOptions.DxcDiagnostic_DisplayCategoryName"]/*' />
    DxcDiagnostic_DisplayCategoryName = 0x20,

    /// <include file='DxcDiagnosticDisplayOptions.xml' path='doc/member[@name="DxcDiagnosticDisplayOptions.DxcDiagnostic_DisplaySeverity"]/*' />
    DxcDiagnostic_DisplaySeverity = 0x200,
}
