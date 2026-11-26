namespace Ghost.DXC;

public enum DxcDiagnosticDisplayOptions
{
    DxcDiagnostic_DisplaySourceLocation = 0x01,
    DxcDiagnostic_DisplayColumn = 0x02,
    DxcDiagnostic_DisplaySourceRanges = 0x04,
    DxcDiagnostic_DisplayOption = 0x08,
    DxcDiagnostic_DisplayCategoryId = 0x10,
    DxcDiagnostic_DisplayCategoryName = 0x20,
    DxcDiagnostic_DisplaySeverity = 0x200,
}
