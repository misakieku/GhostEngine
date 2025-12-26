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
}