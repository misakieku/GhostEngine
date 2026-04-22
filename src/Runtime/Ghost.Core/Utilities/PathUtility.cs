using System.Runtime.CompilerServices;

namespace Ghost.Core.Utilities;

public static class PathUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
