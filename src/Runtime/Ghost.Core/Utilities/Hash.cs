using System.Runtime.CompilerServices;

namespace Ghost.Core.Utilities;

public static class Hash
{
    private const ulong _PRIME1 = 0xfa517d6985796b7bul;
    private const ulong _PRIME2 = 0x589578278297b985ul;
    private const ulong _PRIME3 = 0x221147a447814b73ul;
    private const ulong _PRIME4 = 0x9e3779b97f4a7c15ul; // Golden Ratio

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Hash64(ulong a, ulong b)
    {
        return a ^ (b * _PRIME4 + (a << 6) + (a >> 2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Hash64(ulong a, ulong b, ulong c)
    {
        var h1 = a * _PRIME1;
        var h2 = b * _PRIME2;
        var h3 = c * _PRIME3;

        var h = h1 ^ h2 ^ h3;

        h = (h ^ (h >> 33)) * _PRIME4;
        return h ^ (h >> 29);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Hash64(ulong a, ulong b, ulong c, ulong d)
    {
        var h1 = a * _PRIME1;
        var h2 = b * _PRIME2;
        var h3 = c * _PRIME3;
        var h4 = d * _PRIME4;

        var h = h1 ^ h2 ^ h3 ^ h4;

        h = (h ^ (h >> 33)) * _PRIME1;
        return h ^ (h >> 29);
    }
}