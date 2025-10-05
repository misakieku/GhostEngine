using Win32;

namespace Ghost.Graphics.Utilities;

internal static class Win32Utility
{
    public static void ThrowIfFailed(this HResult hr)
    {
        if (hr.Failure)
        {
            throw new InvalidOperationException($"Operation failed with HRESULT: 0x{hr.Value:X8}");
        }
    }
}