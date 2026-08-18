using System;

namespace Ghost.DSL.Syntax.Symbols;

public static class SymbolId
{
    private const ulong FNV_OFFSET_BASIS = 14695981039346656037UL;
    private const ulong FNV_PRIME = 1099511628211UL;

    public static ulong Compute(string qualifiedName)
    {
        if (string.IsNullOrEmpty(qualifiedName))
        {
            return 0;
        }

        ulong hash = FNV_OFFSET_BASIS;
        for (int i = 0; i < qualifiedName.Length; i++)
        {
            hash ^= qualifiedName[i];
            hash *= FNV_PRIME;
        }
        return hash;
    }

    public static ulong Combine(ulong hash1, ulong hash2)
    {
        ulong hash = hash1;
        hash ^= hash2;
        hash *= FNV_PRIME;
        return hash;
    }
}
