using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Win32;

public static unsafe partial class Windows
{
    public const int TRUE = 1;
    public const int FALSE = 0;
    public const int S_OK = 0;
    public const int S_FALSE = 1;
    public const int E_INVALIDARG = unchecked((int)0x80070057);
    public const int E_NOTFOUND = unchecked((int)0x80070490);
    public const uint UINT32_MAX = uint.MaxValue;
    public const uint WAIT_OBJECT_0 = 0x00000000;
    public const uint WAIT_TIMEOUT = 0x00000102;
    public const uint WAIT_FAILED = 0xFFFFFFFF;
    public const uint INFINITE = 0xFFFFFFFF;

    public static ref readonly Guid IID_NULL
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data =
            [
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            ];

            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFailed(HRESULT value, [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        if (value.FAILED)
        {
            Marshal.ThrowExceptionForHR(value.value);
        }
    }

    [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
    public static extern uint WaitForSingleObjectEx(HANDLE hHandle, uint dwMilliseconds, [NativeTypeName("BOOL")] int bAlertable);

    [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
    [return: NativeTypeName("BOOL")]
    public static extern int CloseHandle(HANDLE hObject);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetLastError() => (uint)Marshal.GetLastWin32Error();
}
