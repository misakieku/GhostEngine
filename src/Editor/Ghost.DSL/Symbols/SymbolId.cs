using System.Text;

namespace Ghost.DSL.Symbols;

public static class SymbolId
{
    private const ulong OFFSET_BASIS = 14695981039346656037ul;
    private const ulong PRIME = 1099511628211ul;

    /// <summary>
    /// Computes a stable 64-bit FNV-1a hash over the UTF-8 bytes of a qualified symbol name.
    /// Deterministic across processes and platforms.
    /// </summary>
    public static ulong Compute(string qualifiedName)
    {
        var hash = OFFSET_BASIS;
        var bytes = Encoding.UTF8.GetBytes(qualifiedName);
        for (var i = 0; i < bytes.Length; i++)
        {
            hash ^= bytes[i];
            hash *= PRIME;
        }
        return hash;
    }
}
