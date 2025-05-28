using System.Numerics;

namespace Ghost.Entities.Utilities;

internal readonly struct ComponentMask
{
    private readonly ulong[] _words;

    public ComponentMask(int entityCapacity)
    {
        _words = new ulong[(entityCapacity + 63) / 64];
    }

    public void Set(int entityIndex)
        => _words[entityIndex >> 6] |= 1UL << (entityIndex & 63);

    public void Clear(int entityIndex)
        => _words[entityIndex >> 6] &= ~(1UL << (entityIndex & 63));

    public bool Get(int entityIndex)
        => ((_words[entityIndex >> 6] >> (entityIndex & 63)) & 1) != 0;

    // Bitwise AND
    public ComponentMask And(in ComponentMask other)
    {
        var result = new ComponentMask(_words.Length * 64);
        for (var i = 0; i < _words.Length; i++)
            result._words[i] = _words[i] & other._words[i];
        return result;
    }

    // Bitwise OR
    public ComponentMask Or(in ComponentMask other)
    {
        var result = new ComponentMask(_words.Length * 64);
        for (var i = 0; i < _words.Length; i++)
            result._words[i] = _words[i] | other._words[i];
        return result;
    }

    // Bitwise NOT
    public ComponentMask Not()
    {
        var result = new ComponentMask(_words.Length * 64);
        for (var i = 0; i < _words.Length; i++)
            result._words[i] = ~_words[i];
        return result;
    }

    // Iterate set bits (fast scan)
    public IEnumerable<int> GetEntityIndices()
    {
        for (var word = 0; word < _words.Length; word++)
        {
            var bits = _words[word];
            while (bits != 0)
            {
                var lowBit = BitOperations.TrailingZeroCount(bits);
                yield return (word << 6) + lowBit;
                bits &= bits - 1; // clear lowest set bit
            }
        }
    }
}
