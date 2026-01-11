using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Helper extensions for XxHash3 to hash common types without string allocation.
/// Uses SIMD-optimized hashing via System.IO.Hashing.XxHash3.
/// </summary>
internal static class RenderGraphHashExtensions
{
    /// <summary>
    /// Appends an int to the hash.
    /// </summary>
    public static void AppendInt(this XxHash64 hash, int value)
    {
        ReadOnlySpan<int> span = stackalloc int[1] { value };
        hash.Append(MemoryMarshal.AsBytes(span));
    }

    /// <summary>
    /// Appends a bool to the hash.
    /// </summary>
    public static void AppendBool(this XxHash64 hash, bool value)
    {
        ReadOnlySpan<bool> span = stackalloc bool[1] { value };
        hash.Append(MemoryMarshal.AsBytes(span));
    }

    /// <summary>
    /// Appends an enum to the hash.
    /// </summary>
    public static void AppendEnum<TEnum>(this XxHash64 hash, TEnum value) where TEnum : unmanaged, Enum
    {
        ReadOnlySpan<TEnum> span = stackalloc TEnum[1] { value };
        hash.Append(MemoryMarshal.AsBytes(span));
    }

    /// <summary>
    /// Appends a struct to the hash (must be unmanaged).
    /// </summary>
    public static void AppendStruct<T>(this XxHash64 hash, in T value) where T : unmanaged
    {
        ReadOnlySpan<T> span = stackalloc T[1] { value };
        hash.Append(MemoryMarshal.AsBytes(span));
    }

    /// <summary>
    /// Appends a list of resource handle indices to the hash.
    /// </summary>
    public static void AppendHandleList(this XxHash64 hash, List<RenderGraphTextureHandle> handles)
    {
        // Only hash the indices, not the versions (versions change but structure doesn't)
        int count = handles.Count;
        hash.AppendInt(count);
        
        //for (int i = 0; i < count; i++)
        //{
        //    hash.AppendInt(handles[i].Index);
        //}
        Span<int> indices = stackalloc int[count];
        for (int i = 0; i < count; i++)
        {
            indices[i] = handles[i].Index;
        }
        hash.Append(MemoryMarshal.AsBytes(indices));
    }
}
