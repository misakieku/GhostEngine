using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Ghost.Shader.Concept;

/// <summary>
/// Compact representation of enabled keywords using bitsets.
/// Supports up to 256 global and 256 local keywords with O(1) operations.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct KeywordSet : IEquatable<KeywordSet>
{
    private const int GlobalBits = 256;
    private const int LocalBits = 256;
    private const int GlobalLongs = GlobalBits / 64;
    private const int LocalLongs = LocalBits / 64;

    private fixed ulong _globalBits[GlobalLongs];
    private fixed ulong _localBits[LocalLongs];

    public void Enable(ShaderKeyword keyword)
    {
        fixed (ulong* global = _globalBits, local = _localBits)
        {
            if (keyword.Scope == KeywordScope.Global)
                SetBit(global, GlobalLongs, keyword.Id);
            else
                SetBit(local, LocalLongs, keyword.Id);
        }
    }

    public void Disable(ShaderKeyword keyword)
    {
        fixed (ulong* global = _globalBits, local = _localBits)
        {
            if (keyword.Scope == KeywordScope.Global)
                ClearBit(global, GlobalLongs, keyword.Id);
            else
                ClearBit(local, LocalLongs, keyword.Id);
        }
    }

    public readonly bool IsEnabled(ShaderKeyword keyword)
    {
        fixed (ulong* global = _globalBits, local = _localBits)
        {
            if (keyword.Scope == KeywordScope.Global)
                return GetBit(global, GlobalLongs, keyword.Id);
            else
                return GetBit(local, LocalLongs, keyword.Id);
        }
    }

    public void Clear()
    {
        fixed (ulong* global = _globalBits, local = _localBits)
        {
            for (int i = 0; i < GlobalLongs; i++)
                global[i] = 0;
            for (int i = 0; i < LocalLongs; i++)
                local[i] = 0;
        }
    }

    public void SetGlobal(KeywordSet* other)
    {
        fixed (ulong* dest = _globalBits)
        {
            for (int i = 0; i < GlobalLongs; i++)
                dest[i] = other->_globalBits[i];
        }
    }

    public void SetLocal(KeywordSet* other)
    {
        fixed (ulong* dest = _localBits)
        {
            for (int i = 0; i < LocalLongs; i++)
                dest[i] = other->_localBits[i];
        }
    }

    public static unsafe KeywordSet Merge(KeywordSet* a, KeywordSet* b)
    {
        KeywordSet result;
        KeywordSet* pResult = &result;

        for (int i = 0; i < GlobalLongs; i++)
            pResult->_globalBits[i] = a->_globalBits[i] | b->_globalBits[i];
        for (int i = 0; i < LocalLongs; i++)
            pResult->_localBits[i] = a->_localBits[i] | b->_localBits[i];

        return result;
    }

    public readonly ulong ComputeHash()
    {
        ulong hash = 0xcbf29ce484222325; // FNV-1a offset
        const ulong prime = 0x100000001b3;

        fixed (ulong* global = _globalBits, local = _localBits)
        {
            for (int i = 0; i < GlobalLongs; i++)
            {
                hash ^= global[i];
                hash *= prime;
            }
            for (int i = 0; i < LocalLongs; i++)
            {
                hash ^= local[i];
                hash *= prime;
            }
        }
        return hash;
    }

    public readonly bool Equals(KeywordSet other)
    {
        fixed (ulong* thisGlobal = _globalBits, thisLocal = _localBits)
        {
            for (int i = 0; i < GlobalLongs; i++)
                if (thisGlobal[i] != other._globalBits[i])
                    return false;

            for (int i = 0; i < LocalLongs; i++)
                if (thisLocal[i] != other._localBits[i])
                    return false;
        }
        return true;
    }

    public override readonly bool Equals(object? obj) => obj is KeywordSet other && Equals(other);
    public override readonly int GetHashCode() => (int)ComputeHash();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetBit(ulong* bits, int count, int index)
    {
        if (index < 0 || index >= count * 64) return;
        int longIndex = index / 64;
        int bitIndex = index % 64;
        bits[longIndex] |= (1UL << bitIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearBit(ulong* bits, int count, int index)
    {
        if (index < 0 || index >= count * 64) return;
        int longIndex = index / 64;
        int bitIndex = index % 64;
        bits[longIndex] &= ~(1UL << bitIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool GetBit(ulong* bits, int count, int index)
    {
        if (index < 0 || index >= count * 64) return false;
        int longIndex = index / 64;
        int bitIndex = index % 64;
        return (bits[longIndex] & (1UL << bitIndex)) != 0;
    }
}
