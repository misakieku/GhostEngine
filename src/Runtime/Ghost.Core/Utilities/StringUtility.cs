using System.Runtime.CompilerServices;

namespace Ghost.Core.Utilities;

public static class StringUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DebugFormat<T>(string format, T value)
    {
#if DEBUG
        return string.Format(format, value);
#else
        return string.Empty;
#endif
    }
}
