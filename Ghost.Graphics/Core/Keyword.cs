using System.Runtime.Intrinsics;

using ElementType = uint;

namespace Ghost.Graphics.Core;

public unsafe struct LocalKeywordSet
{
    public struct ReadOnly
    {
        private LocalKeywordSet _set;

        internal ReadOnly(LocalKeywordSet set)
        {
            _set = set;
        }

        public bool IsKeywordEnabled(int id)
        {
            return _set.IsKeywordEnabled(id);
        }

        public static ReadOnly operator |(in ReadOnly a, in ReadOnly b)
        {
            var resultSet = a._set | b._set;
            return new ReadOnly(resultSet);
        }

        public static ReadOnly operator &(in ReadOnly a, in ReadOnly b)
        {
            var resultSet = a._set & b._set;
            return new ReadOnly(resultSet);
        }
    }

    private const int _DATA_ARRAY_LENGTH = 4; // 4 * 32 = 128 bits
    private const int _SIZE_OF_ELEMENT = sizeof(ElementType);

    private fixed ElementType _data[_DATA_ARRAY_LENGTH];

    public void SetKeyword(int localIndex, bool enabled)
    {
        var index = localIndex / _SIZE_OF_ELEMENT;
        var bit = localIndex % _SIZE_OF_ELEMENT;
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
        var index = localIndex / _SIZE_OF_ELEMENT;
        var bit = localIndex % _SIZE_OF_ELEMENT;
        return (_data[index] & (uint)(1 << bit)) != 0;
    }

    public void Clear()
    {
        for (var i = 0; i < _DATA_ARRAY_LENGTH; i++)
        {
            _data[i] = 0;
        }
    }

    public readonly ReadOnly AsReadOnly()
    {
        return new ReadOnly(this);
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
                    var vecA = Vector128.LoadUnsafe(ref *pDataA, (uint)(i * _SIZE_OF_ELEMENT));
                    var vecB = Vector128.LoadUnsafe(ref *pDataB, (uint)(i * _SIZE_OF_ELEMENT));
                    var vecResult = Vector128.BitwiseOr(vecA, vecB);
                    vecResult.StoreUnsafe(ref result._data[0], (uint)(i * _SIZE_OF_ELEMENT));
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
                    var vecA = Vector128.LoadUnsafe(ref *pDataA, (uint)(i * _SIZE_OF_ELEMENT));
                    var vecB = Vector128.LoadUnsafe(ref *pDataB, (uint)(i * _SIZE_OF_ELEMENT));
                    var vecResult = Vector128.BitwiseAnd(vecA, vecB);
                    vecResult.StoreUnsafe(ref result._data[0], (uint)(i * _SIZE_OF_ELEMENT));
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