namespace Ghost.Core.Graphics;

public enum ZTest : byte
{
    Disabled,
    Less,
    LessEqual,
    Equal,
    GreaterEqual,
    Greater,
    NotEqual,
    Always
}

public enum ZWrite : byte
{
    Off,
    On
}

public enum Cull : byte
{
    Off,
    Front,
    Back
}

public enum Blend : byte
{
    Opaque,
    Alpha,
    Additive,
    Multiply,
    PremultipliedAlpha
}

[Flags]
public enum ColorWriteMask : byte
{
    None = 0,
    Red = 1 << 0,
    Green = 1 << 1,
    Blue = 1 << 2,
    Alpha = 1 << 3,
    All = Red | Green | Blue | Alpha
}

public struct PipelineState
{
    public ZTest ZTest
    {
        get; set;
    }

    public ZWrite ZWrite
    {
        get; set;
    }

    public Cull Cull
    {
        get; set;
    }

    public Blend Blend
    {
        get; set;
    }

    public ColorWriteMask ColorMask
    {
        get; set;
    }

    public static PipelineState Default => new PipelineState
    {
        ZTest = ZTest.LessEqual,
        ZWrite = ZWrite.On,
        Cull = Cull.Back,
        Blend = Blend.Opaque,
        ColorMask = ColorWriteMask.All
    };

    public readonly ulong GetHashCode64()
    {
        // 32-bit packed key for states controlled by material / overrides.
        // layout:
        // 0..3   Blend (4 bits)
        // 4..6   Cull  (3 bits)
        // 7..10  DeafaultState (4 bits)
        // 11     ZWrite (1 bit)
        // 12..15 ColorMask (4 bits)

        var key = 0u;
        key |= ((uint)Blend & 0xFu) << 0;
        key |= ((uint)Cull & 0x7u) << 4;
        key |= ((uint)ZTest & 0xFu) << 7;
        key |= ((uint)ZWrite & 0x1u) << 11;
        key |= ((uint)ColorMask & 0xFu) << 12;

        return key;
    }

    public override readonly int GetHashCode()
    {
        var code64 = GetHashCode64();
        return ((int)code64) ^ (int)(code64 >> 32);
    }
}
