using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RHI;

public static class RHIUtility
{
    public const int MAX_RENDER_TARGETS = 8;
    public const ulong SHADER_ID_MASK = 0xFFFFFFFFFFFFFFF0ul;
    public const ulong PIPELINE_KEY_MASK = 0xFFFFFFFFFFFFFFF0ul;
    public const ulong GRAPHICS_PIPELINE_KEY_FLAG = 0x1ul;
    public const ulong COMPUTE_PIPELINE_KEY_FLAG = 0x2ul;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetBytesPerPixel(this TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R8_UNorm => 1,
            TextureFormat.R8_SNorm => 1,
            TextureFormat.R16_UNorm => 2,
            TextureFormat.R16_SNorm => 2,
            TextureFormat.R16_Float => 2,
            TextureFormat.R32_UInt => 4,
            TextureFormat.R32_SInt => 4,

            TextureFormat.R8G8_UNorm => 2,
            TextureFormat.R8G8_SNorm => 2,
            TextureFormat.R16G16_UNorm => 4,
            TextureFormat.R16G16_SNorm => 4,
            TextureFormat.R16G16_Float => 4,
            TextureFormat.R32G32_Float => 8,

            TextureFormat.R8G8B8A8_UNorm => 4,
            TextureFormat.R8G8B8A8_SNorm => 4,
            TextureFormat.B8G8R8A8_UNorm => 4,

            TextureFormat.R10G10B10A2_UNorm => 4,
            TextureFormat.R16G16B16A16_Float => 8,
            TextureFormat.R32G32B32A32_Float => 16,

            TextureFormat.D24_UNorm_S8_UInt => 4,
            TextureFormat.D32_Float => 4,

            TextureFormat.R32_Typeless => 4,
            TextureFormat.R24G8_Typeless => 4,
            _ => throw new NotSupportedException($"Texture format {format} is not supported."),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDepthStencilFormat(this TextureFormat format)
    {
        return format == TextureFormat.D24_UNorm_S8_UInt || format == TextureFormat.D32_Float;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStencilFormat(this TextureFormat format)
    {
        return format == TextureFormat.D24_UNorm_S8_UInt;
    }

    public static void GetSurfaceInfo(this TextureFormat format, uint width, uint height, out uint rowPitch, out uint slicePitch, out uint rowCount)
    {
        var bc = false;
        var packed = false;
        var planar = false;
        var bpe = 0u;

        //switch (Format)
        //{
        //    case Format.BC1Typeless:
        //    case Format.BC1Unorm:
        //    case Format.BC1UnormSrgb:
        //    case Format.BC4Typeless:
        //    case Format.BC4Unorm:
        //    case Format.BC4Snorm:
        //        bc = true;
        //        bpe = 8;
        //        break;

        //    case Format.BC2Typeless:
        //    case Format.BC2Unorm:
        //    case Format.BC2UnormSrgb:
        //    case Format.BC3Typeless:
        //    case Format.BC3Unorm:
        //    case Format.BC3UnormSrgb:
        //    case Format.BC5Typeless:
        //    case Format.BC5Unorm:
        //    case Format.BC5Snorm:
        //    case Format.BC6HTypeless:
        //    case Format.BC6HUF16:
        //    case Format.BC6HSF16:
        //    case Format.BC7Typeless:
        //    case Format.BC7Unorm:
        //    case Format.BC7UnormSrgb:
        //        bc = true;
        //        bpe = 16;
        //        break;

        //    case Format.R8G8_B8G8Unorm:
        //    case Format.G8R8_G8B8Unorm:
        //    case Format.YUY2:
        //        packed = true;
        //        bpe = 4;
        //        break;

        //    case Format.Y210:
        //    case Format.Y216:
        //        packed = true;
        //        bpe = 8;
        //        break;

        //    case Format.NV12:
        //    case Format.Opaque420:
        //    case Format.P208:
        //        planar = true;
        //        bpe = 2;
        //        break;

        //    case Format.P010:
        //    case Format.P016:
        //        planar = true;
        //        bpe = 4;
        //        break;

        //    default:
        //        break;
        //}

        if (bc)
        {
            var numBlocksWide = 0u;
            if (width > 0)
            {
                numBlocksWide = Math.Max(1u, (width + 3) / 4u);
            }

            var numBlocksHigh = 0u;
            if (height > 0)
            {
                numBlocksHigh = Math.Max(1u, (height + 3) / 4u);
            }

            rowPitch = numBlocksWide * bpe;
            rowCount = numBlocksHigh;
            slicePitch = rowPitch * numBlocksHigh;
        }
        else if (packed)
        {
            rowPitch = ((width + 1u) >> 1) * bpe;
            rowCount = height;
            slicePitch = rowPitch * height;
        }
        else if (planar)
        {
            rowPitch = ((width + 1u) >> 1) * bpe;
            slicePitch = (rowPitch * height) + ((rowPitch * height + 1) >> 1);
            rowCount = height + ((height + 1u) >> 1);
        }
        else
        {
            var bpp = GetBytesPerPixel(format) * 8;
            rowPitch = (width * bpp + 7) / 8; // round up to nearest byte
            rowCount = height;
            slicePitch = rowPitch * height;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetShaderID(string shaderName)
    {
        var hash = XxHash64.HashToUInt64(MemoryMarshal.AsBytes(shaderName.AsSpan()));
        return hash & SHADER_ID_MASK;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetPassID(ulong shaderID, int passIndex)
    {
        Logger.DebugAssert(passIndex >= 0 && passIndex < 16, "Pass index must be between 0 and 15 to fit within the shader ID mask.");
        return shaderID | ((ulong)passIndex & 0xFul);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key64<ShaderPass> CreateShaderPassKey(ulong passID, ulong compiledHash)
    {
        return Hash.Combine64(passID, compiledHash);
    }

    public static unsafe Key128<PipelineState> CreateGraphicsPipelineKey(ulong compiledHash, PipelineState pipelineState, PassAttachmentHash passAttachmentHash)
    {
        // Order-sensitive 128-bit mix. Cheap and stable, avoids span hashing.
        static ulong Mix64(ulong x)
        {
            x ^= x >> 30;
            x *= 0xBF58476D1CE4E5B9ul;
            x ^= x >> 27;
            x *= 0x94D049BB133111EBul;
            x ^= x >> 31;
            return x;
        }

        var mLo = compiledHash;
        var mHi = pipelineState.GetHashCode64();

        var pPasskey = (ulong*)&passAttachmentHash.value;
        var pLo = pPasskey[0];
        var pHi = pPasskey[1];

        // Distinct constants + cross-feeding to reduce structural collisions.
        var hi = Mix64(mHi ^ (pHi + 0xC2B2AE3D27D4EB4Ful) ^ (pLo * 0x165667B19E3779F9ul));
        var lo = Mix64(mLo ^ (pLo + 0x9E3779B97F4A7C15ul) ^ (mHi * 0xD6E8FEB86659FD93ul));

        lo = lo & PIPELINE_KEY_MASK | GRAPHICS_PIPELINE_KEY_FLAG; // Ensure graphics pipeline keys are distinguishable from compute pipeline keys.

        return new Key128<PipelineState>(new UInt128(hi, lo));
    }

    public static Key128<PipelineState> CreateComputePipelineKey(ulong compiledHash)
    {
        // Since compute shader don't have blend state or attachment configurations, we can afford a simpler key generation.
        // Just use the compiled hash with a distinct flag to avoid collisions with graphics pipeline keys.
#if true
        return new Key128<PipelineState>(new UInt128(compiledHash, compiledHash ^ COMPUTE_PIPELINE_KEY_FLAG));
#else
        var shaderHash = compiledHash;
        var stateHash = ~compiledHash;
        // Simple XOR mix. Not as robust as the graphics pipeline key, but sufficient for compute shaders which have fewer variants.
        var hi = shaderHash ^ (stateHash + 0x9E3779B97F4A7C15ul) ^ (shaderHash * 0xD6E8FEB86659FD93ul);
        var lo = stateHash ^ (shaderHash + 0xC2B2AE3D27D4EB4Ful) ^ (stateHash * 0x165667B19E3779F9ul);
        lo = lo & PIPELINE_KEY_MASK | COMPUTE_PIPELINE_KEY_FLAG; // Ensure compute pipeline keys are distinguishable from graphics pipeline keys.

        return new Key128<ComputePipeline>(new UInt128(hi, lo));
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetStringFromHash(UInt128 key, Span<char> destination)
    {
        return key.TryFormat(destination, out var _, "X16");
    }
}
