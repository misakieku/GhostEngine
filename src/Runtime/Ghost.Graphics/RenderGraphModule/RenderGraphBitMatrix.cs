using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Row-aligned 64-bit word bitmask matrix for O(1)/word-parallel RenderGraph dependency and reachability queries.
/// </summary>
internal struct RenderGraphBitMatrix : IDisposable
{
    private UnsafeArray<ulong> _words;

    public readonly int RowCount { get; }
    public readonly int ColumnCount { get; }
    public readonly int WordCount { get; }

    public readonly bool IsCreated => _words.IsCreated;

    public RenderGraphBitMatrix(int rowCount, int columnCount, AllocationHandle allocationHandle)
    {
        RowCount = rowCount;
        ColumnCount = columnCount;
        WordCount = (columnCount + 63) >> 6;
        _words = rowCount > 0 && WordCount > 0
            ? new UnsafeArray<ulong>(rowCount * WordCount, allocationHandle, AllocationOption.Clear)
            : default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int row, int col)
    {
        _words[(row * WordCount) + (col >> 6)] |= 1UL << (col & 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Get(int row, int col)
    {
        return WordCount > 0 && (_words[(row * WordCount) + (col >> 6)] & (1UL << (col & 63))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearRow(int row)
    {
        if (WordCount > 0)
        {
            _words.AsSpan().Slice(row * WordCount, WordCount).Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (_words.IsCreated)
        {
            _words.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<ulong> GetRow(int row)
    {
        return WordCount > 0 ? _words.AsSpan().Slice(row * WordCount, WordCount) : Span<ulong>.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<ulong> GetReadOnlyRow(int row)
    {
        return WordCount > 0 ? _words.AsSpan().Slice(row * WordCount, WordCount) : ReadOnlySpan<ulong>.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OrRow(int dstRow, int srcRow, int startWord = 0)
    {
        var dstOffset = dstRow * WordCount;
        var srcOffset = srcRow * WordCount;
        for (var w = startWord; w < WordCount; w++)
        {
            _words[dstOffset + w] |= _words[srcOffset + w];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void AndRowInto(Span<ulong> dstRow, int srcRow)
    {
        var srcOffset = srcRow * WordCount;
        for (var w = 0; w < WordCount; w++)
        {
            dstRow[w] &= _words[srcOffset + w];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int FindFirstSetBitAfter(int row, int exclusiveStartCol)
    {
        var startCol = exclusiveStartCol + 1;
        if (startCol >= ColumnCount || WordCount == 0)
        {
            return -1;
        }

        var rowOffset = row * WordCount;
        var wordIdx = startCol >> 6;
        var firstWordMask = ~0UL << (startCol & 63);
        var word = _words[rowOffset + wordIdx] & firstWordMask;

        while (true)
        {
            if (word != 0)
            {
                var col = (wordIdx << 6) + BitOperations.TrailingZeroCount(word);
                return col < ColumnCount ? col : -1;
            }

            wordIdx++;
            if (wordIdx >= WordCount)
            {
                return -1;
            }

            word = _words[rowOffset + wordIdx];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IntersectsRange(int row, int startColInclusive, int endColInclusive)
    {
        if (startColInclusive > endColInclusive || WordCount == 0)
        {
            return false;
        }

        var rowOffset = row * WordCount;
        var startWord = startColInclusive >> 6;
        var endWord = endColInclusive >> 6;
        var startMask = ~0UL << (startColInclusive & 63);
        var endBit = endColInclusive & 63;
        var endMask = endBit == 63 ? ulong.MaxValue : (1UL << (endBit + 1)) - 1UL;

        if (startWord == endWord)
        {
            return (_words[rowOffset + startWord] & startMask & endMask) != 0;
        }

        if ((_words[rowOffset + startWord] & startMask) != 0)
        {
            return true;
        }

        for (var w = startWord + 1; w < endWord; w++)
        {
            if (_words[rowOffset + w] != 0)
            {
                return true;
            }
        }

        return (_words[rowOffset + endWord] & endMask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IntersectsRow(int rowA, int rowB)
    {
        var offsetA = rowA * WordCount;
        var offsetB = rowB * WordCount;
        for (var w = 0; w < WordCount; w++)
        {
            if ((_words[offsetA + w] & _words[offsetB + w]) != 0)
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IntersectsWords(int row, ReadOnlySpan<ulong> mask)
    {
        var offset = row * WordCount;
        for (var w = 0; w < WordCount; w++)
        {
            if ((_words[offset + w] & mask[w]) != 0)
            {
                return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
        if (_words.IsCreated)
        {
            _words.Dispose();
        }
    }
}
