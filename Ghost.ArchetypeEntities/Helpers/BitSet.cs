// Code from https://github.com/genaray/Arch/blob/master/src/Arch/Core/Utils/BitSet.cs

using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Helpers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ghost.Entities.Helpers;

// NOTE: Can this be replaced with `System.Collections.BitArray`?
// NOTE: If not, can it at least mirror that type's API?
/// <summary>
/// The <see cref="BitSet"/> class
/// represents a resizable collection of bits.
/// </summary>
public struct BitSet : IDisposable
{
    private const int _BIT_SIZE = (sizeof(uint) * 8) - 1; // 31
    private const int _INDEX_SIZE = 5;  // log_2(BitSize + 1)

    private static readonly int _padding = Vector<uint>.Count; // The padding used for vectorisation, the amount of uints required for being vectorized basically

    /// <summary>
    /// Determines the required length of an <see cref="BitSet"/> to hold the passed id or bit.
    /// </summary>
    /// <param name="id">The id or bit.</param>
    /// <returns>A size of required <see cref="uint"/>s for the bitset.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RequiredLength(int id)
    {
        return (id >> 5) + int.Sign(id & _BIT_SIZE);
    }

    /// <summary>
    /// The bits from the bitset.
    /// </summary>
    private UnsafeArray<uint> _bits;

    /// TODO: Update on ClearBit, however clearbit is only used in tests so its fine for now.
    /// <summary>
    /// The highest bit set.
    /// </summary>
    private int _highestBit;

    /// TODO: Update on ClearBit, probably remove <see cref="_highestBit"/> in favor?
    /// <summary>
    /// The maximum <see cref="_bits"/>-index current in use.
    /// </summary>
    private int _max;

    /// <summary>
    /// Initializes a new instance of the <see cref="BitSet" /> class.
    /// </summary>
    public BitSet()
    {
        _bits = new UnsafeArray<uint>(_padding, Allocator.Persistent);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitSet" /> class.
    /// </summary>
    public BitSet(params Span<uint> bits)
    {
        _bits = new UnsafeArray<uint>(bits.Length, Allocator.Persistent);
        _bits.CopyFrom(bits);
    }

    /// <summary>
    /// The highest uint index in use inside the <see cref="_bits"/>-array.
    /// </summary>
    public readonly int HighestIndex
    {
        get => _max;
    }

    /// <summary>
    /// The highest bit set.
    /// </summary>
    public readonly int HighestBit
    {
        get => _highestBit;
    }

    /// <summary>
    /// Returns the length of the bitset, how many int's it consists of.
    /// </summary>
    public readonly int Count
    {
        get => _bits.Count;
    }

    /// <summary>
    /// Checks whether a bit is set at the index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>True if it is, otherwise false</returns>
    public readonly bool IsSet(int index)
    {
        var b = index >> _INDEX_SIZE;
        if (b >= _bits.Count)
        {
            return false;
        }

        return (_bits[b] & (1 << (index & _BIT_SIZE))) != 0;
    }

    /// <summary>
    /// Sets a bit at the given index.
    /// Resizes its internal array if necessary.
    /// </summary>
    /// <param name="index">The index.</param>
    public void SetBit(int index)
    {
        var b = index >> _INDEX_SIZE;
        if (b >= _bits.Count)
        {
            _bits.Resize((b + _padding) / _padding * _padding);  // Round up to a multiply of Padding
        }

        // Track highest set bit
        _highestBit = Math.Max(_highestBit, index);
        _max = (_highestBit / (_BIT_SIZE + 1)) + 1;
        _bits[b] |= 1u << (index & _BIT_SIZE);
    }

    /// <summary>
    /// Clears the bit at the given index.
    /// </summary>
    /// <param name="index">The index.</param>
    public readonly void ClearBit(int index)
    {
        var b = index >> _INDEX_SIZE;
        if (b >= _bits.Count)
        {
            return;
        }

        _bits[b] &= ~(1u << (index & _BIT_SIZE));
    }

    /// <summary>
    /// Sets all bits.
    /// </summary>
    public void SetAll()
    {
        var count = _bits.Count;
        for (var i = 0; i < count; i++)
        {
            _bits[i] = 0xffffffff;
        }

        _highestBit = (_bits.Count * (_BIT_SIZE + 1)) - 1;
        _max = (_highestBit / (_BIT_SIZE + 1)) + 1;
    }

    /// <summary>
    /// Clears all set bits.
    /// </summary>
    public readonly void ClearAll()
    {
        _bits.Clear();
    }

    /// <summary>
    /// Checks if all bits from this instance match those of the other instance.
    /// </summary>
    /// <param name="other">The other <see cref="BitSet"/>.</param>
    /// <returns>True if they match, false if not.</returns>
    [SkipLocalsInit]
    public readonly bool All(BitSet other)
    {
        var min = Math.Min(Math.Min(Count, other.Count), _max);
        if (!Vector.IsHardwareAccelerated || min < _padding)
        {
            // Bitwise and
            for (var i = 0; i < min; i++)
            {
                var bit = _bits[i];
                if ((bit & other._bits[i]) != bit)
                {
                    return false;
                }
            }

            // Handle extra bits on our side that might just be all zero.
            for (var i = min; i < _max; i++)
            {
                if (_bits[i] != 0)
                {
                    return false;
                }
            }
        }
        else
        {
            // Vectorized bitwise and
            for (var i = 0; i < min; i += _padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                var otherVector = new Vector<uint>(other._bits.AsSpan()[i..]);

                var resultVector = Vector.BitwiseAnd(vector, otherVector);
                if (!Vector.EqualsAll(resultVector, vector))
                {
                    return false;
                }
            }

            // Handle extra bits on our side that might just be all zero.
            for (var i = min; i < _max; i += _padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                if (!Vector.EqualsAll(vector, Vector<uint>.Zero)) // Vectors are not zero bits[0] != 0 basically
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if any bits from this instance match those of the other instance.
    /// </summary>
    /// <param name="other">The other <see cref="BitSet"/>.</param>
    /// <returns>True if they match, false if not.</returns>
    public readonly bool Any(BitSet other)
    {
        var min = Math.Min(Math.Min(Count, other.Count), _max);
        if (!Vector.IsHardwareAccelerated || min < _padding)
        {
            var bits = _bits.AsSpan();
            var otherBits = other._bits.AsSpan();

            // Bitwise and, return true since any is met
            for (var i = 0; i < min; i++)
            {
                var bit = bits[i];
                if ((bit & otherBits[i]) > 0)
                {
                    return true;
                }
            }

            // Handle extra bits on our side that might just be all zero.
            for (var i = min; i < _max; i++)
            {
                if (bits[i] > 0)
                {
                    return false;
                }
            }
        }
        else
        {
            // Vectorized bitwise and, return true since any is met
            for (var i = 0; i < min; i += _padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                var otherVector = new Vector<uint>(other._bits.AsSpan()[i..]);

                var resultVector = Vector.BitwiseAnd(vector, otherVector);
                if (!Vector.EqualsAll(resultVector, Vector<uint>.Zero))
                {
                    return true;
                }
            }

            // Handle extra bits on our side that might just be all zero.
            for (var i = min; i < _max; i += _padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                if (!Vector.EqualsAll(vector, Vector<uint>.Zero)) // Vectors are not zero bits[0] != 0 basically
                {
                    return false;
                }
            }
        }

        return _highestBit <= 0;
    }

    /// <summary>
    /// Checks if none bits from this instance match those of the other instance.
    /// </summary>
    /// <param name="other">The other <see cref="BitSet"/>.</param>
    /// <returns>True if none match, false if not.</returns>
    public readonly bool None(BitSet other)
    {
        var min = Math.Min(Math.Min(Count, other.Count), _max);
        if (!Vector.IsHardwareAccelerated || min < _padding)
        {
            var bits = _bits.AsSpan();
            var otherBits = other._bits.AsSpan();

            // Bitwise and, return true since any is met
            for (var i = 0; i < min; i++)
            {
                var bit = bits[i];
                if ((bit & otherBits[i]) != 0)
                {
                    return false;
                }
            }
        }
        else
        {
            // Vectorized bitwise and, return true since any is met
            for (var i = 0; i < min; i += _padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                var otherVector = new Vector<uint>(other._bits.AsSpan()[i..]);

                var resultVector = Vector.BitwiseAnd(vector, otherVector);
                if (!Vector.EqualsAll(resultVector, Vector<uint>.Zero))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if exactly all bits from this instance match those of the other instance.
    /// </summary>
    /// <param name="other">The other <see cref="BitSet"/>.</param>
    /// <returns>True if they match, false if not.</returns>
    public readonly bool Exclusive(BitSet other)
    {
        var min = Math.Min(Math.Min(Count, other.Count), _max);

        if (!Vector.IsHardwareAccelerated || min < _padding)
        {
            var bits = _bits.AsSpan();
            var otherBits = other._bits.AsSpan();

            // Bitwise xor, if both are not totally equal, return false
            for (var i = 0; i < min; i++)
            {
                var bit = bits[i];
                if ((bit ^ otherBits[i]) != 0)
                {
                    return false;
                }
            }

            // handle extra bits on our side that might just be all zero
            for (var i = min; i < _max; i++)
            {
                if (bits[i] != 0)
                {
                    return false;
                }
            }
        }
        else
        {
            // Vectorized bitwise xor, return true since any is met
            for (var i = 0; i < min; i += _padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                var otherVector = new Vector<uint>(other._bits.AsSpan()[i..]);

                var resultVector = Vector.Xor(vector, otherVector);
                if (!Vector.EqualsAll(resultVector, Vector<uint>.Zero))
                {
                    return false;
                }
            }

            // Handle extra bits on our side that might just be all zero.
            for (var i = min; i < _max; i += _padding)
            {
                var vector = new Vector<uint>(_bits.AsSpan()[i..]);
                if (!Vector.EqualsAll(vector, Vector<uint>.Zero)) // Vectors are not zero bits[0] != 0 basically
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     Creates a <see cref="Span{T}"/> to access the <see cref="_bits"/>.
    /// </summary>
    /// <returns>The hash.</returns>
    public readonly Span<uint> AsSpan()
    {
        var max = (_highestBit / (_BIT_SIZE + 1)) + 1;
        return _bits.AsSpan()[0..max];
    }

    /// <summary>
    ///     Copies the bits into a <see cref="Span{T}"/> and returns a slice containing the copied <see cref="_bits"/>.
    /// </summary>
    /// <param name="span">The <see cref="Span{T}"/> to copy into.</param>
    /// <param name="zero">If true, it will zero the unused space from the <see cref="span"/>.</param>
    /// <returns>The <see cref="Span{T}"/>.</returns>
    public readonly Span<uint> AsSpan(Span<uint> span, bool zero = true)
    {
        // Copy everything that's possible from one to another
        var length = Math.Min(Count, span.Length);
        for (var index = 0; index < length; index++)
        {
            span[index] = _bits[index];
        }

        // Zero the rest space which was not overriden due to the copy.
        for (var index = length; zero && index < span.Length; index++)
        {
            span[index] = 0;
        }

        return span[0..length];
    }

    /// <summary>
    ///     Calculates the hash, this is unique for the set bits. Two <see cref="BitSet"/> with the same set bits, result in the same hash.
    /// </summary>
    /// <returns>The hash.</returns>
    public readonly override int GetHashCode()
    {
        return Component.GetHashCode(AsSpan());
    }

    /// <summary>
    ///     Prints the content of this instance.
    /// </summary>
    /// <returns>The string.</returns>
    public readonly override string ToString()
    {
        // Convert uint to binary form for pretty printing
        var binaryBuilder = new StringBuilder();
        foreach (var bit in _bits)
        {
            binaryBuilder.Append(Convert.ToString((uint)bit, 2).PadLeft(32, '0')).Append(',');
        }
        binaryBuilder.Length--;

        return $"{nameof(_bits)}: {binaryBuilder}, {nameof(Count)}: {Count}";
    }

    public void Dispose()
    {
        _bits.Dispose();
    }
}

/// <summary>
/// The <see cref="SpanBitSet"/> struct
/// represents a non resizable collection of bits.
/// Used to set, check and clear bits on a allocated <see cref="BitSet"/> or on the stack.
/// </summary>
public readonly ref struct SpanBitSet
{
    private const int _BIT_SIZE = (sizeof(uint) * 8) - 1; // 31
    // NOTE: Is a byte not 8 bits?
    private const int _BYTE_SIZE = 5; // log_2(BitSize + 1)

    /// <summary>
    ///     The bits from the bitset.
    /// </summary>
    private readonly Span<uint> _bits;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BitSet" /> class.
    /// </summary>
    public SpanBitSet(Span<uint> bits)
    {
        _bits = bits;
    }

    /// <summary>
    /// Checks whether a bit is set at the index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>True if it is, otherwise false</returns>
    public bool IsSet(int index)
    {
        var b = index >> _BYTE_SIZE;
        if (b >= _bits.Length)
        {
            return false;
        }

        return (_bits[b] & (1 << (index & _BIT_SIZE))) != 0;
    }

    /// <summary>
    /// Sets a bit at the given index.
    /// Resizes its internal array if necessary.
    /// </summary>
    /// <param name="index">The index.</param>
    public void SetBit(int index)
    {
        var b = index >> _BYTE_SIZE;
        if (b >= _bits.Length)
        {
            return;
        }

        _bits[b] |= 1u << (index & _BIT_SIZE);
    }

    /// <summary>
    /// Clears the bit at the given index.
    /// </summary>
    /// <param name="index">The index.</param>
    public void ClearBit(int index)
    {
        var b = index >> _BYTE_SIZE;
        if (b >= _bits.Length)
        {
            return;
        }

        _bits[b] &= ~(1u << (index & _BIT_SIZE));
    }

    /// <summary>
    /// Sets all bits.
    /// </summary>
    public void SetAll()
    {
        var count = _bits.Length;
        for (var i = 0; i < count; i++)
        {
            _bits[i] = 0xffffffff;
        }
    }

    /// <summary>
    /// Clears all set bits.
    /// </summary>
    public void ClearAll()
    {
        _bits.Clear();
    }

    /// <summary>
    /// Creates a <see cref="Span{T}"/> to access the <see cref="_bits"/>.
    /// </summary>
    /// <returns>The hash.</returns>
    public Span<uint> AsSpan()
    {
        return _bits;
    }

    /// <summary>
    /// Copies the bits into a <see cref="Span{T}"/> and returns a slice containing the copied <see cref="_bits"/>.
    /// </summary>
    /// <param name=""></param>
    /// <returns>The hash.</returns>
    public Span<uint> AsSpan(Span<uint> span, bool zero = true)
    {
        // Prevent exception because target array is to small for copy operation
        var length = Math.Min(this._bits.Length, span.Length);
        for (var index = 0; index < length; index++)
        {
            span[index] = _bits[index];
        }

        // Zero the rest space which was not overriden due to the copy.
        for (var index = length; zero && index < span.Length; index++)
        {
            span[index] = 0;
        }

        return span[.._bits.Length];
    }

    /// <summary>
    /// Calculates the hash, this is unique for the set bits. Two <see cref="BitSet"/> with the same set bits, result in the same hash.
    /// </summary>
    /// <returns>The hash.</returns>
    public override int GetHashCode()
    {
        return Component.GetHashCode(AsSpan());
    }

    /// <summary>
    /// Prints the content of this instance.
    /// </summary>
    /// <returns>The string.</returns>
    public override string ToString()
    {
        // Convert uint to binary form for pretty printing
        var binaryBuilder = new StringBuilder();
        foreach (var bit in _bits)
        {
            binaryBuilder.Append(Convert.ToString((uint)bit, 2).PadLeft(32, '0')).Append(',');
        }
        binaryBuilder.Length--;

        return $"{nameof(_bits)}: {string.Join(",", binaryBuilder)}";
    }
}
