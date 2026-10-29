using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Core.Graphics;

[StructLayout(LayoutKind.Sequential)]
public readonly struct KeywordMask128 : IEquatable<KeywordMask128>
{
    public static readonly KeywordMask128 Empty = default;

    public ulong Low { get; init; }
    public ulong High { get; init; }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Low == 0 && High == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KeywordMask128(ulong low, ulong high)
    {
        Low = low;
        High = high;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KeywordMask128 Set(int bitIndex)
    {
        if (bitIndex < 64)
        {
            return new KeywordMask128(Low | (1UL << bitIndex), High);
        }

        return new KeywordMask128(Low, High | (1UL << (bitIndex - 64)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public KeywordMask128 Unset(int bitIndex)
    {
        if (bitIndex < 64)
        {
            return new KeywordMask128(Low & ~(1UL << bitIndex), High);
        }

        return new KeywordMask128(Low, High & ~(1UL << (bitIndex - 64)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(int bitIndex)
    {
        return bitIndex < 64 ? (Low & (1UL << bitIndex)) != 0 : (High & (1UL << (bitIndex - 64))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeywordMask128 operator &(KeywordMask128 left, KeywordMask128 right)
    {
        return new KeywordMask128(left.Low & right.Low, left.High & right.High);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeywordMask128 operator |(KeywordMask128 left, KeywordMask128 right)
    {
        return new KeywordMask128(left.Low | right.Low, left.High | right.High);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeywordMask128 operator ^(KeywordMask128 left, KeywordMask128 right)
    {
        return new KeywordMask128(left.Low ^ right.Low, left.High ^ right.High);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeywordMask128 operator ~(KeywordMask128 mask)
    {
        return new KeywordMask128(~mask.Low, ~mask.High);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(KeywordMask128 left, KeywordMask128 right)
    {
        return left.Low == right.Low && left.High == right.High;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(KeywordMask128 left, KeywordMask128 right)
    {
        return left.Low != right.Low || left.High != right.High;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(KeywordMask128 other)
    {
        return Low == other.Low && High == other.High;
    }

    public override bool Equals(object? obj)
    {
        return obj is KeywordMask128 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Low, High);
    }

    public override string ToString()
    {
        return $"0x{High:X16}_{Low:X16}";
    }
}
