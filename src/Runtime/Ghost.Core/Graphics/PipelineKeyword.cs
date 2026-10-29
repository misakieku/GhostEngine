using System.Runtime.CompilerServices;

namespace Ghost.Core.Graphics;

public readonly struct PipelineKeyword : IEquatable<PipelineKeyword>
{
    public static readonly PipelineKeyword Invalid = new(-1, string.Empty);

    public int Id { get; }
    public string Name { get; }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id >= 0;
    }

    public KeywordMask128 Mask
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Id < 0 || Id >= 128)
            {
                return KeywordMask128.Empty;
            }

            return KeywordMask128.Empty.Set(Id);
        }
    }

    public PipelineKeyword(int id, string name)
    {
        Id = id;
        Name = name ?? string.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PipelineKeyword Register(string name)
    {
        return PipelineKeywordRegistry.GetOrRegister(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(PipelineKeyword other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is PipelineKeyword other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(PipelineKeyword left, PipelineKeyword right)
    {
        return left.Id == right.Id;
    }

    public static bool operator !=(PipelineKeyword left, PipelineKeyword right)
    {
        return left.Id != right.Id;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Name) ? $"Keyword({Id})" : $"{Name}({Id})";
    }
}
