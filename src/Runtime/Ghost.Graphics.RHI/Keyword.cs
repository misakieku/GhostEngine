using System.Runtime.Intrinsics;
using ElementType = uint;

namespace Ghost.Graphics.RHI;

public unsafe struct LocalKeywordSet
{
    private const int _DATA_ARRAY_LENGTH = 4; // 4 * 32 = 128 bits
    private const int _BITS_PER_ELEMENT = sizeof(ElementType) * 8;

    private fixed ElementType _data[_DATA_ARRAY_LENGTH];

    public void SetKeyword(int localIndex, bool enabled)
    {
        var index = localIndex / _BITS_PER_ELEMENT;
        var bit = localIndex % _BITS_PER_ELEMENT;
        if (enabled)
        {
            _data[index] |= (uint)(1 << bit);
        }
        else
        {
            _data[index] &= ~(uint)(1 << bit);
        }
    }

    public bool IsKeywordEnabled(int localIndex)
    {
        var index = localIndex / _BITS_PER_ELEMENT;
        var bit = localIndex % _BITS_PER_ELEMENT;
        return (_data[index] & (uint)(1 << bit)) != 0;
    }

    public void Clear()
    {
        for (var i = 0; i < _DATA_ARRAY_LENGTH; i++)
        {
            _data[i] = 0;
        }
    }

    public ulong GetHashCode64()
    {
        var hash = 14695981039346656037ul; // FNV Offset basis

        for (var i = 0; i < _DATA_ARRAY_LENGTH; i++)
        {
            hash ^= _data[i];
            hash *= 1099511628211ul; // FNV prime
        }

        return hash;
    }

    public override int GetHashCode()
    {
        var hash = 17;
        for (var i = 0; i < _DATA_ARRAY_LENGTH; i++)
        {
            hash = hash * 31 + _data[i].GetHashCode();
        }

        return hash;
    }


    public static LocalKeywordSet operator |(in LocalKeywordSet a, in LocalKeywordSet b)
    {
        var result = default(LocalKeywordSet);

        if (Vector128<ElementType>.IsSupported)
        {
            fixed (ElementType* pDataA = a._data)
            fixed (ElementType* pDataB = b._data)
            {
                for (var i = 0; i < _DATA_ARRAY_LENGTH; i += Vector128<ElementType>.Count)
                {
                    var elementOffset = (nuint)i;
                    var vecA = Vector128.LoadUnsafe(ref *pDataA, elementOffset);
                    var vecB = Vector128.LoadUnsafe(ref *pDataB, elementOffset);
                    var vecResult = Vector128.BitwiseOr(vecA, vecB);
                    vecResult.StoreUnsafe(ref result._data[0], elementOffset);
                }
            }
        }
        else
        {
            for (var i = 0; i < _DATA_ARRAY_LENGTH; i++)
            {
                result._data[i] = a._data[i] | b._data[i];
            }
        }

        return result;
    }

    public static LocalKeywordSet operator &(in LocalKeywordSet a, in LocalKeywordSet b)
    {
        var result = default(LocalKeywordSet);

        if (Vector128<ElementType>.IsSupported)
        {
            fixed (ElementType* pDataA = a._data)
            fixed (ElementType* pDataB = b._data)
            {
                for (var i = 0; i < _DATA_ARRAY_LENGTH; i += Vector128<ElementType>.Count)
                {
                    var elementOffset = (nuint)i;
                    var vecA = Vector128.LoadUnsafe(ref *pDataA, elementOffset);
                    var vecB = Vector128.LoadUnsafe(ref *pDataB, elementOffset);
                    var vecResult = Vector128.BitwiseAnd(vecA, vecB);
                    vecResult.StoreUnsafe(ref result._data[0], elementOffset);
                }
            }
        }
        else
        {

            for (var i = 0; i < _DATA_ARRAY_LENGTH; i++)
            {
                result._data[i] = a._data[i] & b._data[i];
            }
        }

        return result;
    }
}
