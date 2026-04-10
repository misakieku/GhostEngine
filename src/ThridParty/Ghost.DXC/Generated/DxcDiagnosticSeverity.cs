namespace Ghost.DXC;

/// <include file='DxcDiagnosticSeverity.xml' path='doc/member[@name="DxcDiagnosticSeverity"]/*' />
public enum DxcDiagnosticSeverity
{
    /// <include file='DxcDiagnosticSeverity.xml' path='doc/member[@name="DxcDiagnosticSeverity.DxcDiagnostic_Ignored"]/*' />
    DxcDiagnostic_Ignored = 0,

    /// <include file='DxcDiagnosticSeverity.xml' path='doc/member[@name="DxcDiagnosticSeverity.DxcDiagnostic_Note"]/*' />
    DxcDiagnostic_Note = 1,

    /// <include file='DxcDiagnosticSeverity.xml' path='doc/member[@name="DxcDiagnosticSeverity.DxcDiagnostic_Warning"]/*' />
    DxcDiagnostic_Warning = 2,

    /// <include file='DxcDiagnosticSeverity.xml' path='doc/member[@name="DxcDiagnosticSeverity.DxcDiagnostic_Error"]/*' />
    DxcDiagnostic_Error = 3,

    /// <include file='DxcDiagnosticSeverity.xml' path='doc/member[@name="DxcDiagnosticSeverity.DxcDiagnostic_Fatal"]/*' />
    DxcDiagnostic_Fatal = 4,
}
