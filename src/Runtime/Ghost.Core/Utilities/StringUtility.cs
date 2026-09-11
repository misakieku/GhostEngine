using System.Runtime.CompilerServices;

namespace Ghost.Core.Utilities;

public static class StringUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DebugFormat<T>(string format, T arg)
    {
#if DEBUG
        return string.Format(format, arg);
#else
        return string.Empty;
#endif
    }

    /// <summary>
    /// Formats a string using the specified format and arguments, but only when the code is compiled in DEBUG mode. In release builds, it returns an empty string.
    /// </summary>
    /// <param name="format">The composite format string.</param>
    /// <param name="args">The arguments to format into the string.</param>
    /// <returns>The formatted string, or an empty string in release mode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string DebugFormat(string format, params object[] args)
    {
#if DEBUG
        return string.Format(format, args);
#else
        return string.Empty;
#endif
    }
}
