using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Ghost.Core.Graphics;

public static class PipelineKeywordRegistry
{
    public const int MAX_KEYWORD_COUNT = 128;

    private static readonly ConcurrentDictionary<string, int> s_nameToId = new(StringComparer.Ordinal);
    private static readonly string?[] s_idToName = new string?[MAX_KEYWORD_COUNT];
    private static int s_nextId;

    public static int RegisteredCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => s_nextId;
    }

    public static PipelineKeyword GetOrRegister(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (s_nameToId.TryGetValue(name, out var existingId))
        {
            return new PipelineKeyword(existingId, name);
        }

        var id = s_nameToId.GetOrAdd(name, static key =>
        {
            var newId = Interlocked.Increment(ref s_nextId) - 1;
            if (newId >= MAX_KEYWORD_COUNT)
            {
                throw new InvalidOperationException($"Exceeded maximum of {MAX_KEYWORD_COUNT} pipeline keywords. Cannot register '{key}'.");
            }

            s_idToName[newId] = key;
            return newId;
        });

        return new PipelineKeyword(id, name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetKeyword(string name, out PipelineKeyword keyword)
    {
        if (!string.IsNullOrEmpty(name) && s_nameToId.TryGetValue(name, out var id))
        {
            keyword = new PipelineKeyword(id, name);
            return true;
        }

        keyword = PipelineKeyword.Invalid;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetKeywordName(int id)
    {
        if ((uint)id < (uint)MAX_KEYWORD_COUNT)
        {
            return s_idToName[id];
        }

        return null;
    }

    public static KeywordMask128 ParseMask(string? keywords)
    {
        if (string.IsNullOrWhiteSpace(keywords))
        {
            return KeywordMask128.Empty;
        }

        return ParseMask(keywords.AsSpan());
    }

    public static KeywordMask128 ParseMask(ReadOnlySpan<char> keywords)
    {
        if (keywords.IsEmpty)
        {
            return KeywordMask128.Empty;
        }

        var mask = KeywordMask128.Empty;
        var start = 0;
        for (var i = 0; i <= keywords.Length; i++)
        {
            if (i == keywords.Length || keywords[i] == ';')
            {
                var length = i - start;
                if (length > 0)
                {
                    var kwSpan = keywords.Slice(start, length).Trim();
                    if (!kwSpan.IsEmpty)
                    {
                        var kw = GetOrRegister(kwSpan.ToString());
                        mask = mask.Set(kw.Id);
                    }
                }

                start = i + 1;
            }
        }

        return mask;
    }
}
