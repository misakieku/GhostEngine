using System.Runtime.InteropServices;

namespace Ghost.Shader.Concept;

/// <summary>
/// Render state configuration for pipeline creation.
/// Immutable and hashable for PSO caching.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RenderState : IEquatable<RenderState>
{
    // Rasterizer State
    public CullMode CullMode;
    public FillMode FillMode;
    public bool FrontCounterClockwise;
    public float DepthBias;
    public float SlopeScaledDepthBias;

    // Depth-Stencil State
    public bool DepthTestEnable;
    public bool DepthWriteEnable;
    public CompareFunction DepthCompareFunc;
    public bool StencilEnable;
    public byte StencilReadMask;
    public byte StencilWriteMask;

    // Blend State (per RT, simplified to single RT here)
    public bool BlendEnable;
    public BlendFactor SrcBlend;
    public BlendFactor DestBlend;
    public BlendOperation BlendOp;
    public BlendFactor SrcBlendAlpha;
    public BlendFactor DestBlendAlpha;
    public BlendOperation BlendOpAlpha;
    public ColorWriteMask ColorWriteMask;

    // Topology
    public PrimitiveTopology Topology;

    public static RenderState Default => new()
    {
        CullMode = CullMode.Back,
        FillMode = FillMode.Solid,
        FrontCounterClockwise = false,
        DepthTestEnable = true,
        DepthWriteEnable = true,
        DepthCompareFunc = CompareFunction.LessEqual,
        StencilEnable = false,
        StencilReadMask = 0xFF,
        StencilWriteMask = 0xFF,
        BlendEnable = false,
        SrcBlend = BlendFactor.One,
        DestBlend = BlendFactor.Zero,
        BlendOp = BlendOperation.Add,
        SrcBlendAlpha = BlendFactor.One,
        DestBlendAlpha = BlendFactor.Zero,
        BlendOpAlpha = BlendOperation.Add,
        ColorWriteMask = ColorWriteMask.All,
        Topology = PrimitiveTopology.TriangleList
    };

    public unsafe ulong ComputeHash()
    {
        fixed (RenderState* ptr = &this)
        {
            return ComputeHash64((byte*)ptr, sizeof(RenderState));
        }
    }

    private static unsafe ulong ComputeHash64(byte* data, int length)
    {
        ulong hash = 0xcbf29ce484222325;
        const ulong prime = 0x100000001b3;
        for (int i = 0; i < length; i++)
        {
            hash ^= data[i];
            hash *= prime;
        }
        return hash;
    }

    public bool Equals(RenderState other)
    {
        return CullMode == other.CullMode &&
               FillMode == other.FillMode &&
               FrontCounterClockwise == other.FrontCounterClockwise &&
               DepthBias == other.DepthBias &&
               SlopeScaledDepthBias == other.SlopeScaledDepthBias &&
               DepthTestEnable == other.DepthTestEnable &&
               DepthWriteEnable == other.DepthWriteEnable &&
               DepthCompareFunc == other.DepthCompareFunc &&
               StencilEnable == other.StencilEnable &&
               StencilReadMask == other.StencilReadMask &&
               StencilWriteMask == other.StencilWriteMask &&
               BlendEnable == other.BlendEnable &&
               SrcBlend == other.SrcBlend &&
               DestBlend == other.DestBlend &&
               BlendOp == other.BlendOp &&
               SrcBlendAlpha == other.SrcBlendAlpha &&
               DestBlendAlpha == other.DestBlendAlpha &&
               BlendOpAlpha == other.BlendOpAlpha &&
               ColorWriteMask == other.ColorWriteMask &&
               Topology == other.Topology;
    }

    public override bool Equals(object? obj) => obj is RenderState other && Equals(other);
    public override int GetHashCode() => (int)ComputeHash();
}

public enum CullMode : byte { None, Front, Back }
public enum FillMode : byte { Wireframe, Solid }
public enum CompareFunction : byte { Never, Less, Equal, LessEqual, Greater, NotEqual, GreaterEqual, Always }
public enum BlendFactor : byte { Zero, One, SrcColor, InvSrcColor, SrcAlpha, InvSrcAlpha, DestAlpha, InvDestAlpha, DestColor, InvDestColor }
public enum BlendOperation : byte { Add, Subtract, ReverseSubtract, Min, Max }
public enum PrimitiveTopology : byte { PointList, LineList, LineStrip, TriangleList, TriangleStrip }

[Flags]
public enum ColorWriteMask : byte
{
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 4,
    Alpha = 8,
    All = Red | Green | Blue | Alpha
}
