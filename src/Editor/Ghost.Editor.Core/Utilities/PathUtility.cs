using System.Runtime.CompilerServices;

namespace Ghost.Editor.Core.Utilities;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetUniqueName(string path)
    {
        var directory = Path.GetDirectoryName(path);
        directory ??= ".";

        var fileName = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        var uniqueName = fileName;
        var counter = 1;

        while (File.Exists(Path.Combine(directory, uniqueName + extension)))
        {
            uniqueName = $"{fileName} ({counter++})";
        }

        return Path.Combine(directory, uniqueName + extension);
    }
}
